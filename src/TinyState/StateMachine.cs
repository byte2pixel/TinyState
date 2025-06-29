using System.Diagnostics.CodeAnalysis;

namespace TinyState;

/// <summary>
/// Represents a simple state machine with strongly-typed states and triggers.
/// </summary>
/// <typeparam name="TState">The type representing states. Must be non-nullable.</typeparam>
/// <typeparam name="TTrigger">The type representing triggers. Must be non-nullable.</typeparam>
public class StateMachine<TState, TTrigger> where TState : notnull where TTrigger : notnull
{
    private TState _currentState;
    private readonly Dictionary<TState, StateConfiguration> _configurations = new();


    /// <summary>
    /// Initializes the state machine with the specified initial state.
    /// </summary>
    /// <param name="initialState">The initial state of the state machine.</param>
    public StateMachine(TState initialState)
    {
        _currentState = initialState;
    }

    /// <summary>
    /// Configures the state with the specified state identifier.
    /// </summary>
    /// <param name="state">The state identifier.</param>
    /// <returns>The configuration object for the specified state.</returns>
    public StateConfiguration Configure(TState state)
    {
        if (_configurations.TryGetValue(state, out StateConfiguration? config)) return config;
        config = new StateConfiguration();
        _configurations[state] = config;
        return config;
    }

    /// <summary>
    /// Gets the current state of the state machine.
    /// </summary>
    public TState State => _currentState;

    /// <summary>
    /// Fires the specified trigger, causing the state machine to transition if a valid transition is configured.
    /// </summary>
    /// <param name="trigger">The trigger to fire.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if there is no configuration for the current state, or if there is no transition for the given trigger in the current state.
    /// </exception>
    public void Fire(TTrigger trigger)
    {
        if (!_configurations.TryGetValue(_currentState, out StateConfiguration? config))
            throw new InvalidOperationException($"No configuration found for state '{_currentState}'");

        if (!config.TryGetTransition(trigger, out TState? nextState))
            throw new InvalidOperationException($"No transition from '{_currentState}' via '{trigger}'");

        _currentState = nextState;
    }

    /// <summary>
    /// Configures the state-specific behavior, including transitions, entry, and exit actions.
    /// </summary>
    public class StateConfiguration
    {
        private readonly Dictionary<TTrigger, TState> _transitions = new();
        private TTrigger? _currentTrigger; // Will never be null due to the type constraint but isn't initialized until When() is called.
        private bool _hasCurrentTrigger;

        /// <summary>
        /// Specifies the trigger that will cause the transition.
        /// </summary>
        /// <param name="trigger">The trigger to specify.</param>
        /// <returns>The current state configuration instance.</returns>
        public StateConfiguration When(TTrigger trigger)
        {
            _currentTrigger = trigger;
            _hasCurrentTrigger = true;
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
        public StateConfiguration GoTo(TState targetState)
        {
            if (!_hasCurrentTrigger || _currentTrigger is null)
            {
                throw new InvalidOperationException("You must call When() before GoTo().");
            }
            _transitions[_currentTrigger] = targetState;
            _hasCurrentTrigger = false;
            return this;
        }

        internal bool TryGetTransition(TTrigger trigger, [NotNullWhen(true)] out TState? nextState)
        {
            return _transitions.TryGetValue(trigger, out nextState);
        }

        // Add OnEnter, OnExit, OnTransition hooks as needed
    }
}
