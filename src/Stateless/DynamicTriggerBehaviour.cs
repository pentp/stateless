using System;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal sealed class DynamicTriggerBehaviour : TriggerBehaviour
        {
            internal readonly Func<object[], TState> Destination;
            internal readonly Reflection.DynamicTransitionInfo TransitionInfo;

            public DynamicTriggerBehaviour(TTrigger trigger, Func<object[], TState> destination, 
                TransitionGuard transitionGuard, Reflection.DynamicTransitionInfo info)
                : base(trigger, transitionGuard)
            {
                Destination = destination ?? throw new ArgumentNullException(nameof(destination));
                TransitionInfo = info ?? throw new ArgumentNullException(nameof(info));
            }
        }
    }
}
