namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal sealed class ReentryTriggerBehaviour : TriggerBehaviour
        {
            internal readonly TState Destination;

            public ReentryTriggerBehaviour(TState destination) => Destination = destination;
            public ReentryTriggerBehaviour(TState destination, TransitionGuard transitionGuard) : base(transitionGuard) => Destination = destination;
        }
    }
}
