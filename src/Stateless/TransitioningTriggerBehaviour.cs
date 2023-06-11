namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal sealed class TransitioningTriggerBehaviour : TriggerBehaviour
        {
            internal readonly TState Destination;

            public TransitioningTriggerBehaviour(TState destination) => Destination = destination;
            public TransitioningTriggerBehaviour(TState destination, TransitionGuard transitionGuard) : base(transitionGuard) => Destination = destination;
        }
    }
}
