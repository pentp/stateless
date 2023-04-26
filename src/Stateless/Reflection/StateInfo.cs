using System;
using System.Collections.Generic;
using System.Linq;

namespace Stateless.Reflection
{
    /// <summary>
    /// Describes an internal StateRepresentation through the reflection API.
    /// </summary>
    public class StateInfo
    {
        internal static StateInfo CreateStateInfo<TState, TTrigger>(StateMachine<TState, TTrigger>.StateRepresentation stateRepresentation)
        {
            if (stateRepresentation == null)
                throw new ArgumentException(nameof(stateRepresentation));

            var ignoredTriggers = new List<IgnoredTransitionInfo>();

            // stateRepresentation.TriggerBehaviours maps from TTrigger to ICollection<TriggerBehaviour>
            foreach (var triggerBehaviours in stateRepresentation.TriggerBehaviours)
            {
                foreach (var item in triggerBehaviours.Value)
                {
                    if (item is StateMachine<TState, TTrigger>.IgnoredTriggerBehaviour behaviour)
                    {
                        ignoredTriggers.Add(IgnoredTransitionInfo.Create(behaviour));
                    }
                }
            }

            return new StateInfo(stateRepresentation.UnderlyingState, ignoredTriggers,
                stateRepresentation.EntryActions?.ConvertAll(e => ActionInfo.Create(e)) ?? Enumerable.Empty<ActionInfo>(),
                stateRepresentation.ActivateActions?.ConvertAll(e => e.Description) ?? Enumerable.Empty<InvocationInfo>(),
                stateRepresentation.DeactivateActions?.ConvertAll(e => e.Description) ?? Enumerable.Empty<InvocationInfo>(),
                stateRepresentation.ExitActions?.ConvertAll(e => e.Description) ?? Enumerable.Empty<InvocationInfo>());
        }
 
        internal void AddRelationships<TState, TTrigger>(StateMachine<TState, TTrigger>.StateRepresentation stateRepresentation, Dictionary<TState, StateInfo> stateInfo)
        {
            var substates = new List<StateInfo>();
            if (stateRepresentation.Substates != null)
                foreach (var s in stateRepresentation.Substates)
                    substates.Add(stateInfo[s.UnderlyingState]);

            StateInfo superstate = null;
            if (stateRepresentation.Superstate != null)
                superstate = stateInfo[stateRepresentation.Superstate.UnderlyingState];

            var fixedTransitions = new List<FixedTransitionInfo>();
            var dynamicTransitions = new List<DynamicTransitionInfo>();

            foreach (var triggerBehaviours in stateRepresentation.TriggerBehaviours)
            {
                // First add all the deterministic transitions
                foreach (var item in triggerBehaviours.Value)
                    if (item is StateMachine<TState, TTrigger>.TransitioningTriggerBehaviour behaviour)
                    {
                        var destinationInfo = stateInfo[behaviour.Destination];
                        fixedTransitions.Add(FixedTransitionInfo.Create(item, destinationInfo));
                    }
                foreach (var item in triggerBehaviours.Value)
                    if (item is StateMachine<TState, TTrigger>.ReentryTriggerBehaviour behaviour)
                    {
                        var destinationInfo = stateInfo[behaviour.Destination];
                        fixedTransitions.Add(FixedTransitionInfo.Create(item, destinationInfo));
                    }
                //Then add all the internal transitions
                foreach (var item in triggerBehaviours.Value)
                    if (item is StateMachine<TState, TTrigger>.InternalTriggerBehaviour)
                    {
                        var destinationInfo = stateInfo[stateRepresentation.UnderlyingState];
                        fixedTransitions.Add(FixedTransitionInfo.Create(item, destinationInfo));
                    }
                // Then add all the dynamic transitions
                foreach (var item in triggerBehaviours.Value)
                    if (item is StateMachine<TState, TTrigger>.DynamicTriggerBehaviour behaviour)
                    {
                        dynamicTransitions.Add(behaviour.TransitionInfo);
                    }
            }

            Superstate = superstate;
            Substates = substates;
            FixedTransitions = fixedTransitions;
            DynamicTransitions = dynamicTransitions;
        }

        private StateInfo(
            object underlyingState,
            IEnumerable<IgnoredTransitionInfo> ignoredTriggers,
            IEnumerable<ActionInfo> entryActions,
            IEnumerable<InvocationInfo> activateActions,
            IEnumerable<InvocationInfo> deactivateActions,
            IEnumerable<InvocationInfo> exitActions)
        {
            UnderlyingState = underlyingState;
            IgnoredTriggers = ignoredTriggers;
            EntryActions = entryActions;
            ActivateActions = activateActions;
            DeactivateActions = deactivateActions;
            ExitActions = exitActions;
        }

        /// <summary>
        /// The instance or value this state represents.
        /// </summary>
        public object UnderlyingState { get; }

        /// <summary>
        /// Substates defined for this StateResource.
        /// </summary>
        public IEnumerable<StateInfo> Substates { get; private set; }

        /// <summary>
        /// Superstate defined, if any, for this StateResource.
        /// </summary>
        public StateInfo Superstate { get; private set; }

        /// <summary>
        /// Actions that are defined to be executed on state-entry.
        /// </summary>
        public IEnumerable<ActionInfo> EntryActions { get; private set; }

        /// <summary>
        /// Actions that are defined to be executed on activation.
        /// </summary>
        public IEnumerable<InvocationInfo> ActivateActions { get; private set; }

        /// <summary>
        /// Actions that are defined to be executed on deactivation.
        /// </summary>
        public IEnumerable<InvocationInfo> DeactivateActions { get; private set; }

        /// <summary>
        /// Actions that are defined to be executed on state-exit.
        /// </summary>
        public IEnumerable<InvocationInfo> ExitActions { get; private set; }

        /// <summary> 
        /// Transitions defined for this state.
        /// </summary>
        public IEnumerable<TransitionInfo> Transitions 
        {
            get { 
                return FixedTransitions == null // A quick way to check if AddRelationships has been called.
                            ? null 
                            : FixedTransitions.Concat<TransitionInfo>(DynamicTransitions); 
            } 
        }

        /// <summary>
        /// Transitions defined for this state.
        /// </summary>
        public IEnumerable<FixedTransitionInfo> FixedTransitions { get; private set; }

        /// <summary>
        /// Dynamic Transitions defined for this state internally.
        /// </summary>
        public IEnumerable<DynamicTransitionInfo> DynamicTransitions { get; private set; }

        /// <summary>
        /// Triggers ignored for this state.
        /// </summary>
        public IEnumerable<IgnoredTransitionInfo> IgnoredTriggers { get; private set; }

        /// <summary>
        /// Passes through to the value's ToString.
        /// </summary>
        public override string ToString()
        {
            return UnderlyingState?.ToString() ?? "<null>";
        }
    }
}
