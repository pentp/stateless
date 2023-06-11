namespace Stateless
{
    internal abstract class TriggerBehaviour
    {
        protected TriggerBehaviour() { }
        protected TriggerBehaviour(TransitionGuard guard) => Guard = guard;

        /// <summary>
        /// Guard is the transition guard for this trigger.  Equal to
        /// TransitionGuard.Empty if there is no transition guard
        /// </summary>
        internal readonly TransitionGuard Guard;

        /// <summary>
        /// GuardConditionsMet is true if all of the guard functions return true
        /// or if there are no guard functions
        /// </summary>
        public bool GuardConditionsMet() => Guard.GuardConditionsMet();
    }
}
