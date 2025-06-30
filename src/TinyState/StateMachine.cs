using System.Diagnostics.CodeAnalysis;

namespace TinyState;

/// <summary>
/// Represents a simple state machine with strongly-typed states and triggers.
/// </summary>
/// <typeparam name="TState">The type representing states. Must be non-nullable.</typeparam>
/// <typeparam name="TTrigger">The type representing triggers. Must be non-nullable.</typeparam>
public class StateMachine<TState, TTrigger>
    where TState : notnull
    where TTrigger : notnull
{
    private readonly Dictionary<TState, StateConfiguration> _configurations = new();
    private bool _isTransitioning;

    /// <summary>
    /// Initializes the state machine with the specified initial state.
    /// </summary>
    /// <param name="initialState">The initial state of the state machine.</param>
    public StateMachine(TState initialState)
    {
        State = initialState;
    }

    /// <summary>
    /// Configures the state with the specified state identifier.
    /// </summary>
    /// <param name="state">The state identifier.</param>
    /// <returns>The configuration object for the specified state.</returns>
    public StateConfiguration Configure(TState state)
    {
        if (_configurations.TryGetValue(state, out StateConfiguration? config))
            return config;
        config = new StateConfiguration();
        _configurations[state] = config;
        return config;
    }

    /// <summary>
    /// Gets the current state of the state machine.
    /// </summary>
    public TState State { get; private set; }

    /// <summary>
    /// Fires the specified trigger, causing the state machine to transition if a valid transition is configured.
    /// </summary>
    /// <param name="trigger">The trigger to fire.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if there is no configuration for the current state, or if there is no transition for the given trigger in the current state.
    /// </exception>
    public void Fire(TTrigger trigger)
    {
        FireAsync(trigger).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously fires the specified trigger, causing the state machine to transition if a valid transition is configured.
    /// </summary>
    /// <remarks>
    /// Async hooks (OnExitAsync, OnTransitionAsync, OnEnterAsync) are always executed before their synchronous counterparts (OnExit, OnTransition, OnEnter).
    /// This ensures that any asynchronous side effects complete before synchronous logic runs. This order is not configurable.
    /// </remarks>
    /// <param name="trigger">The trigger to fire.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if there is no configuration for the current state, or if there is no transition for the given trigger in the current state, or if the target state is not configured.
    /// </exception>
    public async Task FireAsync(TTrigger trigger)
    {
        if (_isTransitioning)
            throw new InvalidOperationException(
                "Reentrant transitions are not allowed. Cannot fire a trigger during another transition."
            );
        if (!_configurations.TryGetValue(State, out StateConfiguration? config))
            throw new InvalidOperationException($"No configuration found for state '{State}'");

        (TState nextState, StateConfiguration toConfig) = await SelectTransitionAsync(
            config,
            trigger
        );

        _isTransitioning = true;
        try
        {
            // OnExit hooks (async then sync)
            if (config.OnExitActionAsync is not null)
                await config.OnExitActionAsync();
            config.OnExitAction?.Invoke();

            // OnTransition hooks (async then sync)
            if (config.OnTransitionActionAsync is not null)
                await config.OnTransitionActionAsync(trigger, nextState);
            config.OnTransitionAction?.Invoke(trigger, nextState);

            State = nextState;

            // OnEnter hooks (async then sync)
            if (toConfig.OnEnterActionAsync is not null)
                await toConfig.OnEnterActionAsync();
            toConfig.OnEnterAction?.Invoke();
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private async Task<(TState nextState, StateConfiguration toConfig)> SelectTransitionAsync(
        StateConfiguration config,
        TTrigger trigger
    )
    {
        Exception? guardException = null;
        foreach (
            (var guards, var guardsAsync, TState targetState) in config.GetTransitionsForTrigger(
                trigger
            )
        )
        {
            try
            {
                if (!_configurations.TryGetValue(targetState, out StateConfiguration? toConfig))
                    continue;

                bool allAsyncPassed =
                    guardsAsync.Count == 0
                    || (await Task.WhenAll(guardsAsync.Select(g => g()))).All(result => result);
                if (!allAsyncPassed)
                    continue;

                bool allSyncPassed = guards.Count == 0 || guards.All(g => g());
                if (!allSyncPassed)
                    continue;

                return (targetState, toConfig);
            }
            catch (Exception ex)
            {
                guardException = ex;
                break;
            }
        }

        if (guardException is not null)
            throw guardException;
        throw new InvalidOperationException(
            $"No transition from '{State}' via '{trigger}' (all guards failed or none present)."
        );
    }

    /// <summary>
    /// Builder stage: after Trigger, allows When/WhenAsync/TransitionTo.
    /// </summary>
    [SuppressMessage(
        "ReSharper",
        "UnusedMethodReturnValue.Global",
        Justification = "Used for fluent API chaining"
    )]
    public interface IGuardOrTransitionStage
    {
        IGuardOrTransitionStage When(Func<bool> guard);
        IGuardOrTransitionStage WhenAsync(Func<Task<bool>> guardAsync);
        IAfterTransitionStage TransitionTo(TState targetState);
    }

    /// <summary>
    /// Builder stage: after TransitionTo, only allows Trigger or hooks.
    /// </summary>
    [SuppressMessage(
        "ReSharper",
        "UnusedMemberInSuper.Global",
        Justification = "Used for fluent API chaining"
    )]
    public interface IAfterTransitionStage
    {
        IGuardOrTransitionStage Trigger(TTrigger trigger);
        StateConfiguration OnEnter(Action action);
        StateConfiguration OnEnterAsync(Func<Task> action);
        StateConfiguration OnExit(Action action);
        StateConfiguration OnExitAsync(Func<Task> action);
        StateConfiguration OnTransition(Action<TTrigger, TState> action);
        StateConfiguration OnTransitionAsync(Func<TTrigger, TState, Task> action);
    }

    /// <summary>
    /// Configures the state-specific behavior, including transitions, entry, and exit actions.
    /// </summary>
    public class StateConfiguration : IGuardOrTransitionStage, IAfterTransitionStage
    {
        private sealed class TransitionInfo
        {
            public readonly List<Func<bool>> Guards = [];
            public readonly List<Func<Task<bool>>> GuardsAsync = [];
            public TState? TargetState;
        }

        private readonly Dictionary<TTrigger, TransitionInfo> _transitions = new();
        private TTrigger? _currentTrigger;
        private TransitionInfo? _currentTransition;

        internal Action? OnEnterAction;
        internal Func<Task>? OnEnterActionAsync;
        internal Action? OnExitAction;
        internal Func<Task>? OnExitActionAsync;
        internal Action<TTrigger, TState>? OnTransitionAction;
        internal Func<TTrigger, TState, Task>? OnTransitionActionAsync;

        /// <summary>
        /// Begins configuration for a new trigger on this state.
        /// </summary>
        /// <param name="trigger">The trigger to configure.</param>
        /// <returns>The builder for further configuration.</returns>
        public IGuardOrTransitionStage Trigger(TTrigger trigger)
        {
            _currentTrigger = trigger;
            _currentTransition = new TransitionInfo();
            _transitions[trigger] = _currentTransition;
            return this;
        }

        /// <summary>
        /// Adds a synchronous guard condition for the current trigger. Multiple guards can be chained; all must pass.
        /// </summary>
        /// <param name="guard">A synchronous guard function returning true to allow the transition.</param>
        /// <returns>The builder for further configuration.</returns>
        public IGuardOrTransitionStage When(Func<bool> guard)
        {
            if (_currentTransition is null)
                throw new InvalidOperationException("You must call Trigger() before When().");
            _currentTransition.Guards.Add(guard);
            return this;
        }

        /// <summary>
        /// Adds an asynchronous guard condition for the current trigger. Multiple guards can be chained; all must pass.
        /// </summary>
        /// <param name="guardAsync">An asynchronous guard function returning true to allow the transition.</param>
        /// <returns>The builder for further configuration.</returns>
        public IGuardOrTransitionStage WhenAsync(Func<Task<bool>> guardAsync)
        {
            if (_currentTransition is null)
                throw new InvalidOperationException("You must call Trigger() before WhenAsync().");
            _currentTransition.GuardsAsync.Add(guardAsync);
            return this;
        }

        /// <summary>
        /// Sets the target state for the current trigger. Ends the guard chain for this trigger.
        /// </summary>
        /// <param name="targetState">The state to transition to if all guards pass.</param>
        /// <returns>The builder for further configuration (new trigger or hooks).</returns>
        public IAfterTransitionStage TransitionTo(TState targetState)
        {
            if (_currentTransition is null || _currentTrigger is null)
                throw new InvalidOperationException(
                    "You must call Trigger() before TransitionTo()."
                );
            _currentTransition.TargetState = targetState;
            // After TransitionTo, prevent further When/WhenAsync for this trigger
            _currentTransition = null;
            _currentTrigger = default;
            return this;
        }

        internal IEnumerable<(
            List<Func<bool>> guards,
            List<Func<Task<bool>>> guardsAsync,
            TState targetState
        )> GetTransitionsForTrigger(TTrigger trigger)
        {
            if (
                _transitions.TryGetValue(trigger, out TransitionInfo? t)
                && t.TargetState is not null
            )
                yield return (t.Guards, t.GuardsAsync, t.TargetState);
        }

        /// <summary>
        /// Registers an action to be called when entering this state.
        /// </summary>
        /// <param name="action">The action to call on entry.</param>
        /// <returns>The state configuration for chaining.</returns>
        public StateConfiguration OnEnter(Action action)
        {
            OnEnterAction = action;
            return this;
        }

        /// <summary>
        /// Registers an asynchronous action to be called when entering this state.
        /// </summary>
        /// <param name="action">The async action to call on entry.</param>
        /// <returns>The state configuration for chaining.</returns>
        public StateConfiguration OnEnterAsync(Func<Task> action)
        {
            OnEnterActionAsync = action;
            return this;
        }

        /// <summary>
        /// Registers an action to be called when exiting this state.
        /// </summary>
        /// <param name="action">The action to call on exit.</param>
        /// <returns>The state configuration for chaining.</returns>
        public StateConfiguration OnExit(Action action)
        {
            OnExitAction = action;
            return this;
        }

        /// <summary>
        /// Registers an asynchronous action to be called when exiting this state.
        /// </summary>
        /// <param name="action">The async action to call on exit.</param>
        /// <returns>The state configuration for chaining.</returns>
        public StateConfiguration OnExitAsync(Func<Task> action)
        {
            OnExitActionAsync = action;
            return this;
        }

        /// <summary>
        /// Registers an action to be called when a transition occurs from this state.
        /// </summary>
        /// <param name="action">The action to call on transition.</param>
        /// <returns>The state configuration for chaining.</returns>
        public StateConfiguration OnTransition(Action<TTrigger, TState> action)
        {
            OnTransitionAction = action;
            return this;
        }

        /// <summary>
        /// Registers an asynchronous action to be called when a transition occurs from this state.
        /// </summary>
        /// <param name="action">The async action to call on transition.</param>
        /// <returns>The state configuration for chaining.</returns>
        public StateConfiguration OnTransitionAsync(Func<TTrigger, TState, Task> action)
        {
            OnTransitionActionAsync = action;
            return this;
        }
    }
}
