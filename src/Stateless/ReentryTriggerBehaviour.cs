namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal sealed class ReentryTriggerBehaviour : TriggerBehaviour
        {
            internal readonly TState Destination;

            // transitionGuard can be null if there is no guard function on the transition
            public ReentryTriggerBehaviour(TTrigger trigger, TState destination, TransitionGuard transitionGuard)
                : base(trigger, transitionGuard)
            {
                Destination = destination;
            }
        }
        
    }
}
