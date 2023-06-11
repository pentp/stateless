using System;
using System.Linq;

namespace Stateless.Reflection
{
    /// <summary>
    /// Describes a transition that can be initiated from a trigger.
    /// </summary>
    public class FixedTransitionInfo : TransitionInfo
    {
        internal FixedTransitionInfo(object trigger, TriggerBehaviour behaviour, StateInfo destinationStateInfo)
        {
            Trigger = new TriggerInfo(trigger);
            DestinationState = destinationStateInfo;
            GuardConditionsMethodDescriptions = behaviour.Guard.Conditions?.Select(c => c.MethodDescription) ?? Array.Empty<InvocationInfo>();
        }

        /// <summary>
        /// The state that will be transitioned into on activation.
        /// </summary>
        public StateInfo DestinationState { get; private set; }
    }
}