namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal sealed class ReentryTriggerBehaviour : TriggerBehaviour
        {
            internal readonly TState Destination;

            public ReentryTriggerBehaviour(TTrigger trigger, TState destination, TransitionGuard transitionGuard)
                : base(trigger, transitionGuard)
            {
                Destination = destination;
            }
        }
    }
}
