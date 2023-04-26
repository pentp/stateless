﻿using System;
using System.Linq;

namespace Stateless.Reflection
{
    /// <summary>
    /// Describes a trigger that is "ignored" (stays in the same state)
    /// </summary>
    public class IgnoredTransitionInfo : TransitionInfo
    {
        internal static IgnoredTransitionInfo Create<TState, TTrigger>(StateMachine<TState, TTrigger>.IgnoredTriggerBehaviour behaviour)
        {
            var transition = new IgnoredTransitionInfo
            {
                Trigger = new TriggerInfo(behaviour.Trigger),
                GuardConditionsMethodDescriptions = behaviour.Guard.Conditions?.Select(c => c.MethodDescription) ?? Array.Empty<InvocationInfo>()
            };

            return transition;
        }

        private IgnoredTransitionInfo() { }
    }
}