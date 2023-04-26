using Stateless.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Stateless
{
    /// <summary>
    /// Enum for the different modes used when Fire-ing a trigger
    /// </summary>
    public enum FiringMode
    {
        /// <summary> Use immediate mode when the queuing of trigger events are not needed. Care must be taken when using this mode, as there is no run-to-completion guaranteed.</summary>
        Immediate,
        /// <summary> Use the queued Fire-ing mode when run-to-completion is required. This is the recommended mode.</summary>
        Queued
    }

    /// <summary>
    /// Models behaviour as transitions between a finite set of states.
    /// </summary>
    /// <typeparam name="TState">The type used to represent the states.</typeparam>
    /// <typeparam name="TTrigger">The type used to represent the triggers that cause state transitions.</typeparam>
    public partial class StateMachine<TState, TTrigger>
    {
        private readonly Dictionary<TState, StateRepresentation> _stateConfiguration = new();
        private readonly Dictionary<TTrigger, TriggerWithParameters> _triggerConfiguration = new();
        private readonly Func<TState> _stateAccessor;
        private readonly Action<TState> _stateMutator;
        private Delegate _unhandledTriggerAction;
        private readonly OnTransitionedEvent _onTransitionedEvent = new();
        private readonly OnTransitionedEvent _onTransitionCompletedEvent = new();
        private readonly TState _initialState;
        private TState _currentState;
        private readonly bool _immediateFiringMode;

        private struct QueuedTrigger
        {
            public TTrigger Trigger;
            public object[] Args;
        }

        private readonly Queue<QueuedTrigger> _eventQueue = new Queue<QueuedTrigger>();
        private bool _firing;

        /// <summary>
        /// Construct a state machine with external state storage.
        /// </summary>
        /// <param name="stateAccessor">A function that will be called to read the current state value.</param>
        /// <param name="stateMutator">An action that will be called to write new state values.</param>
        public StateMachine(Func<TState> stateAccessor, Action<TState> stateMutator) :this(stateAccessor, stateMutator, FiringMode.Queued)
        {
        }

        /// <summary>
        /// Construct a state machine.
        /// </summary>
        /// <param name="initialState">The initial state.</param>
        public StateMachine(TState initialState) : this(initialState, FiringMode.Queued)
        {
        }

        /// <summary>
        /// Construct a state machine with external state storage.
        /// </summary>
        /// <param name="stateAccessor">A function that will be called to read the current state value.</param>
        /// <param name="stateMutator">An action that will be called to write new state values.</param>
        /// <param name="firingMode">Optional specification of firing mode.</param>
        public StateMachine(Func<TState> stateAccessor, Action<TState> stateMutator, FiringMode firingMode)
        {
            _stateAccessor = stateAccessor ?? throw new ArgumentNullException(nameof(stateAccessor));
            _stateMutator = stateMutator ?? throw new ArgumentNullException(nameof(stateMutator));

            _initialState = stateAccessor();
            _immediateFiringMode = firingMode == FiringMode.Immediate;
        }

        /// <summary>
        /// Construct a state machine.
        /// </summary>
        /// <param name="initialState">The initial state.</param>
        /// <param name="firingMode">Optional specification of firing mode.</param>
        public StateMachine(TState initialState, FiringMode firingMode)
        {
            _currentState = _initialState = initialState;
            _immediateFiringMode = firingMode == FiringMode.Immediate;
        }

        /// <summary>
        /// For certain situations, it is essential that the SynchronizationContext is retained for all delegate calls.
        /// </summary>
        public bool RetainSynchronizationContext { get; set; } = false;

        /// <summary>
        /// The current state.
        /// </summary>
        public TState State
        {
            get => _stateAccessor is { } accessor ? accessor() : _currentState;
            private set
            {
                if (_stateMutator is { } mutator) mutator(value);
                else _currentState = value;
            }
        }

        /// <summary>
        /// The currently-permissible trigger values.
        /// </summary>
        public IEnumerable<TTrigger> PermittedTriggers
        {
            get
            {
                return GetPermittedTriggers();
            }
        }

        /// <summary>
        /// The currently-permissible trigger values.
        /// </summary>
        public IEnumerable<TTrigger> GetPermittedTriggers(params object[] args)
        {
            return CurrentRepresentation?.GetPermittedTriggers(args) ?? Enumerable.Empty<TTrigger>();
        }

        /// <summary>
        /// Gets the currently-permissible triggers with any configured parameters.
        /// </summary>
        public IEnumerable<TriggerDetails<TState, TTrigger>> GetDetailedPermittedTriggers(params object[] args)
        {
            return CurrentRepresentation?.GetPermittedTriggers(args)
                .ConvertAll(trigger => new TriggerDetails<TState, TTrigger>(trigger, _triggerConfiguration))
                ?? Enumerable.Empty<TriggerDetails<TState, TTrigger>>();
        }

        StateRepresentation CurrentRepresentation => TryGetRepresentation(State);

        /// <summary>
        /// Provides an info object which exposes the states, transitions, and actions of this machine.
        /// </summary>
        public StateMachineInfo GetInfo()
        {
            var initialState = StateInfo.CreateStateInfo(new StateRepresentation(_initialState, RetainSynchronizationContext));

            var behaviours = new List<TState>();
            foreach (var kvp in _stateConfiguration)
                foreach (var b in kvp.Value.TriggerBehaviours)
                    foreach (var tb in b.Value)
                    {
                        if (tb is TransitioningTriggerBehaviour transitioning)
                            behaviours.Add(transitioning.Destination);
                        else if (tb is ReentryTriggerBehaviour reentry)
                            behaviours.Add(reentry.Destination);
                    }

            var representations = new Dictionary<TState, StateRepresentation>(_stateConfiguration);
            foreach (var representation in behaviours)
                if (!representations.ContainsKey(representation))
                    representations.Add(representation, new StateRepresentation(representation, RetainSynchronizationContext));

            var info = representations.ToDictionary(kvp => kvp.Key, kvp => StateInfo.CreateStateInfo(kvp.Value));

            foreach (var state in info)
                state.Value.AddRelationships(representations[state.Key], info);

            return new StateMachineInfo(info.Values, typeof(TState), typeof(TTrigger), initialState);
        }

        StateRepresentation TryGetRepresentation(TState state)
        {
            _stateConfiguration.TryGetValue(state, out StateRepresentation result);
            return result;
        }

        StateRepresentation GetRepresentation(TState state)
        {
            if (!_stateConfiguration.TryGetValue(state, out StateRepresentation result))
            {
                result = new StateRepresentation(state, RetainSynchronizationContext);
                _stateConfiguration.Add(state, result);
            }

            return result;
        }

        /// <summary>
        /// Begin configuration of the entry/exit actions and allowed transitions
        /// when the state machine is in a particular state.
        /// </summary>
        /// <param name="state">The state to configure.</param>
        /// <returns>A configuration object through which the state can be configured.</returns>
        public StateConfiguration Configure(TState state)
        {
            return new StateConfiguration(this, GetRepresentation(state));
        }

        /// <summary>
        /// Transition from the current state via the specified trigger.
        /// The target state is determined by the configuration of the current state.
        /// Actions associated with leaving the current state and entering the new one
        /// will be invoked.
        /// </summary>
        /// <param name="trigger">The trigger to fire.</param>
        /// <exception cref="System.InvalidOperationException">The current state does
        /// not allow the trigger to be fired.</exception>
        public void Fire(TTrigger trigger)
        {
            InternalFire(trigger);
        }

        /// <summary>
        /// Transition from the current state via the specified trigger.
        /// The target state is determined by the configuration of the current state.
        /// Actions associated with leaving the current state and entering the new one
        /// will be invoked.
        /// </summary>
        /// <param name="trigger">The trigger to fire.</param>
        /// <param name="args">A variable-length parameters list containing arguments. </param>
        /// <exception cref="System.InvalidOperationException">The current state does
        /// not allow the trigger to be fired.</exception>
        public void Fire(TriggerWithParameters trigger, params object[] args)
        {
            if (trigger == null) throw new ArgumentNullException(nameof(trigger));
            InternalFire(trigger.Trigger, args);
        }

        /// <summary>
        /// Specify the arguments that must be supplied when a specific trigger is fired.
        /// </summary>
        /// <param name="trigger">The underlying trigger value.</param>
        /// <param name="argumentTypes">The argument types expected by the trigger.</param>
        /// <returns>An object that can be passed to the Fire() method in order to
        /// fire the parameterised trigger.</returns>
        public TriggerWithParameters SetTriggerParameters(TTrigger trigger, params Type[] argumentTypes)
        {
            var configuration = new TriggerWithParameters(trigger, argumentTypes);
            SaveTriggerConfiguration(configuration);
            return configuration;
        }

        /// <summary>
        /// Transition from the current state via the specified trigger.
        /// The target state is determined by the configuration of the current state.
        /// Actions associated with leaving the current state and entering the new one
        /// will be invoked.
        /// </summary>
        /// <typeparam name="TArg0">Type of the first trigger argument.</typeparam>
        /// <param name="trigger">The trigger to fire.</param>
        /// <param name="arg0">The first argument.</param>
        /// <exception cref="System.InvalidOperationException">The current state does
        /// not allow the trigger to be fired.</exception>
        public void Fire<TArg0>(TriggerWithParameters<TArg0> trigger, TArg0 arg0)
        {
            if (trigger == null) throw new ArgumentNullException(nameof(trigger));
            InternalFire(trigger.Trigger, arg0);
        }

        /// <summary>
        /// Transition from the current state via the specified trigger.
        /// The target state is determined by the configuration of the current state.
        /// Actions associated with leaving the current state and entering the new one
        /// will be invoked.
        /// </summary>
        /// <typeparam name="TArg0">Type of the first trigger argument.</typeparam>
        /// <typeparam name="TArg1">Type of the second trigger argument.</typeparam>
        /// <param name="arg0">The first argument.</param>
        /// <param name="arg1">The second argument.</param>
        /// <param name="trigger">The trigger to fire.</param>
        /// <exception cref="System.InvalidOperationException">The current state does
        /// not allow the trigger to be fired.</exception>
        public void Fire<TArg0, TArg1>(TriggerWithParameters<TArg0, TArg1> trigger, TArg0 arg0, TArg1 arg1)
        {
            if (trigger == null) throw new ArgumentNullException(nameof(trigger));
            InternalFire(trigger.Trigger, arg0, arg1);
        }

        /// <summary>
        /// Transition from the current state via the specified trigger.
        /// The target state is determined by the configuration of the current state.
        /// Actions associated with leaving the current state and entering the new one
        /// will be invoked.
        /// </summary>
        /// <typeparam name="TArg0">Type of the first trigger argument.</typeparam>
        /// <typeparam name="TArg1">Type of the second trigger argument.</typeparam>
        /// <typeparam name="TArg2">Type of the third trigger argument.</typeparam>
        /// <param name="arg0">The first argument.</param>
        /// <param name="arg1">The second argument.</param>
        /// <param name="arg2">The third argument.</param>
        /// <param name="trigger">The trigger to fire.</param>
        /// <exception cref="System.InvalidOperationException">The current state does
        /// not allow the trigger to be fired.</exception>
        public void Fire<TArg0, TArg1, TArg2>(TriggerWithParameters<TArg0, TArg1, TArg2> trigger, TArg0 arg0, TArg1 arg1, TArg2 arg2)
        {
            if (trigger == null) throw new ArgumentNullException(nameof(trigger));
            InternalFire(trigger.Trigger, arg0, arg1, arg2);
        }

        /// <summary>
        /// Activates current state. Actions associated with activating the current state
        /// will be invoked. The activation is idempotent and subsequent activation of the same current state
        /// will not lead to re-execution of activation callbacks.
        /// </summary>
        public void Activate()
        {
            CurrentRepresentation?.Activate();
        }

        /// <summary>
        /// Deactivates current state. Actions associated with deactivating the current state
        /// will be invoked. The deactivation is idempotent and subsequent deactivation of the same current state
        /// will not lead to re-execution of deactivation callbacks.
        /// </summary>
        public void Deactivate()
        {
            CurrentRepresentation?.Deactivate();
        }

        /// <summary>
        /// Determine how to Fire the trigger
        /// </summary>
        /// <param name="trigger">The trigger. </param>
        /// <param name="args">A variable-length parameters list containing arguments. </param>
        void InternalFire(TTrigger trigger, params object[] args)
        {
            if (_immediateFiringMode) InternalFireOne(trigger, args);
            else InternalFireQueued(trigger, args);
        }

        /// <summary>
        /// Queue events and then fire in order.
        /// If only one event is queued, this behaves identically to the non-queued version.
        /// </summary>
        /// <param name="trigger">  The trigger. </param>
        /// <param name="args">     A variable-length parameters list containing arguments. </param>
        private void InternalFireQueued(TTrigger trigger, params object[] args)
        {
            // Add trigger to queue
            _eventQueue.Enqueue(new QueuedTrigger { Trigger = trigger, Args = args });

            // If a trigger is already being handled then the trigger will be queued (FIFO) and processed later.
            if (_firing)
            {
                return;
            }

            try
            {
                _firing = true;

                // Empty queue for triggers
                while (_eventQueue.Count > 0)
                {
                    var queuedEvent = _eventQueue.Dequeue();
                    InternalFireOne(queuedEvent.Trigger, queuedEvent.Args);
                }
            }
            finally
            {
                _firing = false;
            }
        }

        /// <summary>
        /// This method handles the execution of a trigger handler. It finds a
        /// handle, then updates the current state information.
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="args"></param>
        void InternalFireOne(TTrigger trigger, params object[] args)
        {
            // If this is a trigger with parameters, we must validate the parameter(s)
            if (_triggerConfiguration.TryGetValue(trigger, out TriggerWithParameters configuration))
                configuration.ValidateParameters(args);

            var source = State;
            var representativeState = TryGetRepresentation(source);

            // Try to find a trigger handler, either in the current state or a super state.
            TriggerBehaviourResult result = default;
            if (representativeState?.TryFindHandler(trigger, args, out result) != true)
            {
                ExecuteUnhandledTrigger(source, trigger, result.UnmetGuardConditions);
                return;
            }

            switch (result.Handler)
            {
                // Check if this trigger should be ignored
                case IgnoredTriggerBehaviour _:
                    return;
                // Handle special case, re-entry in superstate
                // Check if it is an internal transition, or a transition from one state to another.
                case ReentryTriggerBehaviour handler:
                {
                    // Handle transition, and set new state
                    var transition = new Transition(source, handler.Destination, trigger, args);
                    HandleReentryTrigger(args, representativeState, transition);
                    break;
                }
                case DynamicTriggerBehaviour dynamicTrigger when dynamicTrigger.Destination(args) is var destination:
                case TransitioningTriggerBehaviour transitioning when (destination = transitioning.Destination) is var _:
                    {
                    // Handle transition, and set new state
                    var transition = new Transition(source, destination, trigger, args);
                    HandleTransitioningTrigger(args, representativeState, transition);

                    break;
                }
                case InternalTriggerBehaviour.Sync internalAction:
                {
                    // Internal transitions does not update the current state, but must execute the associated action.
                    var transition = new Transition(source, source, trigger, args);
                    internalAction.Execute(transition, args);
                    break;
                }
                case InternalTriggerBehaviour.Async: throw new InvalidOperationException("Running Async internal actions in synchronous mode is not allowed");
                default:
                    throw new InvalidOperationException("State machine configuration incorrect, no handler for trigger.");
            }
        }

        private void HandleReentryTrigger(object[] args, StateRepresentation representativeState, Transition transition)
        {
            StateRepresentation representation;
            transition = representativeState.Exit(transition);
            var newRepresentation = GetRepresentation(transition.Destination);

            if (!transition.Source.Equals(transition.Destination))
            {
                // Then Exit the final superstate
                transition = new Transition(transition.Destination, transition.Destination, transition.Trigger, args);
                newRepresentation.Exit(transition);

                _onTransitionedEvent.Invoke(transition);
                representation = EnterState(newRepresentation, transition, args);
                _onTransitionCompletedEvent.Invoke(transition);

            }
            else
            {
                _onTransitionedEvent.Invoke(transition);
                representation = EnterState(newRepresentation, transition, args);
                _onTransitionCompletedEvent.Invoke(transition);
            }
            State = representation.UnderlyingState;
        }

        private void HandleTransitioningTrigger( object[] args, StateRepresentation representativeState, Transition transition)
        {
            transition = representativeState.Exit(transition);

            State = transition.Destination;
            var newRepresentation = GetRepresentation(transition.Destination);

            //Alert all listeners of state transition
            _onTransitionedEvent.Invoke(transition);
            var representation = EnterState(newRepresentation, transition, args);

            // Check if state has changed by entering new state (by firing triggers in OnEntry or such)
            if (!representation.UnderlyingState.Equals(State))
            {
                // The state has been changed after entering the state, must update current state to new one
                State = representation.UnderlyingState;
            }

            _onTransitionCompletedEvent.Invoke(new Transition(transition.Source, State, transition.Trigger, transition.Parameters));
        }

        private StateRepresentation EnterState(StateRepresentation representation, Transition transition, object [] args)
        {
            // Enter the new state
            representation.Enter(transition, args);

            if (_immediateFiringMode && !State.Equals(transition.Destination))
            {
                // This can happen if triggers are fired in OnEntry
                // Must update current representation with updated State
                representation = GetRepresentation(State);
                transition = new Transition(transition.Source, State, transition.Trigger, args);
            }

            // Recursively enter substates that have an initial transition
            if (representation.HasInitialTransition)
            {
                // Verify that the target state is a substate
                // Check if state has substate(s), and if an initial transition(s) has been set up.
                if (!representation.Substates.Exists(s => s.UnderlyingState.Equals(representation.InitialTransitionTarget)))
                {
                    throw new InvalidOperationException($"The target ({representation.InitialTransitionTarget}) for the initial transition is not a substate.");
                }

                var initialTransition = new InitialTransition(transition.Source, representation.InitialTransitionTarget, transition.Trigger, args);
                representation = GetRepresentation(representation.InitialTransitionTarget);

                // Alert all listeners of initial state transition
                _onTransitionedEvent.Invoke(new Transition(transition.Destination, initialTransition.Destination, transition.Trigger, transition.Parameters));
                representation = EnterState(representation, initialTransition, args);
            }

            return representation;
        }

        /// <summary>
        /// Override the default behaviour of throwing an exception when an unhandled trigger
        /// is fired.
        /// </summary>
        /// <param name="unhandledTriggerAction">An action to call when an unhandled trigger is fired.</param>
        public void OnUnhandledTrigger(Action<TState, TTrigger> unhandledTriggerAction)
        {
            _unhandledTriggerAction = unhandledTriggerAction;
        }

        /// <summary>
        /// Override the default behaviour of throwing an exception when an unhandled trigger
        /// is fired.
        /// </summary>
        /// <param name="unhandledTriggerAction">An action to call when an unhandled trigger is fired.</param>
        public void OnUnhandledTrigger(Action<TState, TTrigger, ICollection<string>> unhandledTriggerAction)
        {
            _unhandledTriggerAction = unhandledTriggerAction;
        }

        private void ExecuteUnhandledTrigger(TState source, TTrigger trigger, List<string> unmetGuardConditions)
        {
            switch (_unhandledTriggerAction)
            {
                case Action<TState, TTrigger> act: act(source, trigger); break;
                case Action<TState, TTrigger, ICollection<string>> act2: act2(source, trigger, unmetGuardConditions); break;
                case null: DefaultUnhandledTriggerAction(source, trigger, unmetGuardConditions); break;
                default: throw new InvalidOperationException("Cannot execute asynchronous action specified in OnUnhandledTrigger. Use asynchronous version of Fire [FireAsync]");
            }
        }

        /// <summary>
        /// Determine if the state machine is in the supplied state.
        /// </summary>
        /// <param name="state">The state to test for.</param>
        /// <returns>True if the current state is equal to, or a substate of,
        /// the supplied state.</returns>
        public bool IsInState(TState state)
        {
            return CurrentRepresentation?.IsIncludedIn(state) ?? State.Equals(state);
        }

        /// <summary>
        /// Returns true if <paramref name="trigger"/> can be fired
        /// in the current state.
        /// </summary>
        /// <param name="trigger">Trigger to test.</param>
        /// <returns>True if the trigger can be fired, false otherwise.</returns>
        public bool CanFire(TTrigger trigger)
        {
            return CurrentRepresentation?.CanHandle(trigger) ?? false;
        }

        /// <summary>
        /// Returns true if <paramref name="trigger"/> can be fired
        /// in the current state.
        /// </summary>
        /// <param name="trigger">Trigger to test.</param>
        /// <param name="unmetGuards">Guard descriptions of unmet guards. If given trigger is not configured for current state, this will be null.</param>
        /// <returns>True if the trigger can be fired, false otherwise.</returns>
        public bool CanFire(TTrigger trigger, out ICollection<string> unmetGuards)
        {
            unmetGuards = null;
            return CurrentRepresentation?.CanHandle(trigger, out unmetGuards) ?? false;
        }

        /// <summary>
        /// A human-readable representation of the state machine.
        /// </summary>
        /// <returns>A description of the current state and permitted triggers.</returns>
        public override string ToString()
        {
            return $"StateMachine {{ State = {State}, PermittedTriggers = {{ {string.Join(", ", GetPermittedTriggers())} }}}}";
        }

        /// <summary>
        /// Specify the arguments that must be supplied when a specific trigger is fired.
        /// </summary>
        /// <typeparam name="TArg0">Type of the first trigger argument.</typeparam>
        /// <param name="trigger">The underlying trigger value.</param>
        /// <returns>An object that can be passed to the Fire() method in order to
        /// fire the parameterised trigger.</returns>
        public TriggerWithParameters<TArg0> SetTriggerParameters<TArg0>(TTrigger trigger)
        {
            var configuration = new TriggerWithParameters<TArg0>(trigger);
            SaveTriggerConfiguration(configuration);
            return configuration;
        }

        /// <summary>
        /// Specify the arguments that must be supplied when a specific trigger is fired.
        /// </summary>
        /// <typeparam name="TArg0">Type of the first trigger argument.</typeparam>
        /// <typeparam name="TArg1">Type of the second trigger argument.</typeparam>
        /// <param name="trigger">The underlying trigger value.</param>
        /// <returns>An object that can be passed to the Fire() method in order to
        /// fire the parameterised trigger.</returns>
        public TriggerWithParameters<TArg0, TArg1> SetTriggerParameters<TArg0, TArg1>(TTrigger trigger)
        {
            var configuration = new TriggerWithParameters<TArg0, TArg1>(trigger);
            SaveTriggerConfiguration(configuration);
            return configuration;
        }

        /// <summary>
        /// Specify the arguments that must be supplied when a specific trigger is fired.
        /// </summary>
        /// <typeparam name="TArg0">Type of the first trigger argument.</typeparam>
        /// <typeparam name="TArg1">Type of the second trigger argument.</typeparam>
        /// <typeparam name="TArg2">Type of the third trigger argument.</typeparam>
        /// <param name="trigger">The underlying trigger value.</param>
        /// <returns>An object that can be passed to the Fire() method in order to
        /// fire the parameterised trigger.</returns>
        public TriggerWithParameters<TArg0, TArg1, TArg2> SetTriggerParameters<TArg0, TArg1, TArg2>(TTrigger trigger)
        {
            var configuration = new TriggerWithParameters<TArg0, TArg1, TArg2>(trigger);
            SaveTriggerConfiguration(configuration);
            return configuration;
        }

        void SaveTriggerConfiguration(TriggerWithParameters trigger)
        {
            if (_triggerConfiguration.ContainsKey(trigger.Trigger))
                throw new InvalidOperationException(
                    string.Format(StateMachineResources.CannotReconfigureParameters, trigger));

            _triggerConfiguration.Add(trigger.Trigger, trigger);
        }

        static void DefaultUnhandledTriggerAction(TState state, TTrigger trigger, List<string> unmetGuardConditions)
        {
            if (unmetGuardConditions?.Count > 0)
                throw new InvalidOperationException(
                    string.Format(
                        StateMachineResources.NoTransitionsUnmetGuardConditions,
                        trigger, state, string.Join(", ", unmetGuardConditions)));

            throw new InvalidOperationException(
                string.Format(
                    StateMachineResources.NoTransitionsPermitted,
                    trigger, state));
        }

        /// <summary>
        /// Registers a callback that will be invoked every time the state machine
        /// transitions from one state into another.
        /// </summary>
        /// <param name="onTransitionAction">The action to execute, accepting the details
        /// of the transition.</param>
        public void OnTransitioned(Action<Transition> onTransitionAction)
        {
            if (onTransitionAction == null) throw new ArgumentNullException(nameof(onTransitionAction));
            _onTransitionedEvent.Register(onTransitionAction);
        }

        /// <summary>
        /// Registers a callback that will be invoked every time the statemachine
        /// transitions from one state into another and all the OnEntryFrom etc methods
        /// have been invoked
        /// </summary>
        /// <param name="onTransitionAction">The action to execute, accepting the details
        /// of the transition.</param>
        public void OnTransitionCompleted(Action<Transition> onTransitionAction)
        {
            if (onTransitionAction == null) throw new ArgumentNullException(nameof(onTransitionAction));
            _onTransitionCompletedEvent.Register(onTransitionAction);
        }
    }
}
