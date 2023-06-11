using System;
using System.Linq;

namespace Stateless.Reflection
{
    /// <summary>
    /// Describes a trigger that is "ignored" (stays in the same state)
    /// </summary>
    public class IgnoredTransitionInfo : TransitionInfo
    {
        internal IgnoredTransitionInfo(object trigger, IgnoredTriggerBehaviour behaviour)
        {
            Trigger = new TriggerInfo(trigger);
            GuardConditionsMethodDescriptions = behaviour.Guard.Conditions?.Select(c => c.MethodDescription) ?? Array.Empty<InvocationInfo>();
        }
    }
}