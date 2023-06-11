namespace Stateless
{
    internal sealed class IgnoredTriggerBehaviour : TriggerBehaviour
    {
        public IgnoredTriggerBehaviour() { }
        public IgnoredTriggerBehaviour(TransitionGuard transitionGuard) : base(transitionGuard) { }
    }
}
