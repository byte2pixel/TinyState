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

        (TState nextState, StateConfiguration toConfig) = await SelectTransitionAsync(config, trigger);

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

    private async Task<(TState nextState, StateConfiguration toConfig)> SelectTransitionAsync(StateConfiguration config,
        TTrigger trigger)
    {
        Exception? guardException = null;
        foreach ((var guard, var guardAsync, TState targetState) in config.GetTransitionsForTrigger(trigger))
        {
            try
            {
                if (guardAsync != null)
                {
                    if (!await guardAsync()) continue;
                    if (!_configurations.TryGetValue(targetState, out var toConfig))
                        throw new InvalidOperationException($"No configuration found for target state '{targetState}'");
                    return (targetState, toConfig);
                }

                if (guard != null && !guard()) continue;
                if (!_configurations.TryGetValue(targetState, out var toConfig2))
                    throw new InvalidOperationException($"No configuration found for target state '{targetState}'");
                return (targetState, toConfig2);
            }
            catch (Exception ex)
            {
                guardException = ex;
                break;
            }
        }

        if (guardException is not null) throw guardException;
        throw new InvalidOperationException(
            $"No transition from '{State}' via '{trigger}' (all guards failed or none present).");
    }

    /// <summary>
    /// Configures the state-specific behavior, including transitions, entry, and exit actions.
    /// </summary>
    public class StateConfiguration
    {
        private sealed class TransitionInfo
        {
            public Func<bool>? Guard;
            public Func<Task<bool>>? GuardAsync;
            public TState TargetState = default!;
        }

        private readonly Dictionary<TTrigger, TransitionInfo> _transitions = new();
        private TTrigger? _currentTrigger;
        private TransitionInfo? _currentTransition;
        private bool _hasCurrentTrigger;

        internal Action? OnEnterAction;
        internal Func<Task>? OnEnterActionAsync;
        internal Action? OnExitAction;
        internal Func<Task>? OnExitActionAsync;
        internal Action<TTrigger, TState>? OnTransitionAction;
        internal Func<TTrigger, TState, Task>? OnTransitionActionAsync;

        /// <summary>
        /// Specifies the trigger that will cause the transition.
        /// </summary>
        /// <param name="trigger">The trigger to specify.</param>
        /// <returns>The current state configuration instance.</returns>
        public StateConfiguration Trigger(TTrigger trigger)
        {
            _currentTrigger = trigger;
            _hasCurrentTrigger = true;
            _currentTransition = new TransitionInfo();
            _transitions[trigger] = _currentTransition;
            return this;
        }

        /// <summary>
        /// Specifies the guard condition that must be satisfied for the transition to occur.
        /// </summary>
        /// <param name="guard">The synchronous guard condition.</param>
        /// <returns>The current state configuration instance.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if Trigger() was not called before calling When().
        /// </exception>
        public StateConfiguration When(Func<bool> guard)
        {
            if (_currentTransition is null)
                throw new InvalidOperationException("You must call Trigger() before When().");
            _currentTransition.Guard = guard;
            return this;
        }

        /// <summary>
        /// Specifies the asynchronous guard condition that must be satisfied for the transition to occur.
        /// </summary>
        /// <param name="guardAsync">The asynchronous guard condition.</param>
        /// <returns>The current state configuration instance.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if Trigger() was not called before calling WhenAsync().
        /// </exception>
        public StateConfiguration WhenAsync(Func<Task<bool>> guardAsync)
        {
            if (_currentTransition is null)
                throw new InvalidOperationException("You must call Trigger() before WhenAsync().");
            _currentTransition.GuardAsync = guardAsync;
            return this;
        }

        /// <summary>
        /// Specifies the target state for the transition.
        /// </summary>
        /// <param name="targetState">The target state to transition to.</param>
        /// <returns>The current state configuration instance.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if When() was not called before calling GoTo().
        /// </exception>
        public StateConfiguration TransitionTo(TState targetState)
        {
            if (!_hasCurrentTrigger || _currentTrigger is null || _currentTransition is null)
                throw new InvalidOperationException(
                    "You must call Trigger() before TransitionTo()."
                );
            _currentTransition.TargetState = targetState;
            _hasCurrentTrigger = false;
            _currentTransition = null;
            return this;
        }

        internal IEnumerable<(
            Func<bool>? guard,
            Func<Task<bool>>? guardAsync,
            TState targetState
            )> GetTransitionsForTrigger(TTrigger trigger)
        {
            if (_transitions.TryGetValue(trigger, out var t))
                yield return (t.Guard, t.GuardAsync, t.TargetState);
        }

        public StateConfiguration OnEnter(Action action)
        {
            OnEnterAction = action;
            return this;
        }

        public StateConfiguration OnEnterAsync(Func<Task> action)
        {
            OnEnterActionAsync = action;
            return this;
        }

        public StateConfiguration OnExit(Action action)
        {
            OnExitAction = action;
            return this;
        }

        public StateConfiguration OnExitAsync(Func<Task> action)
        {
            OnExitActionAsync = action;
            return this;
        }

        public StateConfiguration OnTransition(Action<TTrigger, TState> action)
        {
            OnTransitionAction = action;
            return this;
        }

        public StateConfiguration OnTransitionAsync(Func<TTrigger, TState, Task> action)
        {
            OnTransitionActionAsync = action;
            return this;
        }
    }
}
