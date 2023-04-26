namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal sealed class IgnoredTriggerBehaviour : TriggerBehaviour
        {
            public IgnoredTriggerBehaviour(TTrigger trigger, TransitionGuard transitionGuard)
                : base(trigger, transitionGuard)
            {
            }
        }
    }
}
