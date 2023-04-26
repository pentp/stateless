namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal abstract class TriggerBehaviour
        {
            /// <summary>
            /// If there is no guard function, _guard is set to TransitionGuard.Empty
            /// </summary>
            readonly TransitionGuard _guard;

            /// <summary>
            /// TriggerBehaviour constructor
            /// </summary>
            /// <param name="trigger"></param>
            /// <param name="guard">TransitionGuard (null if no guard function)</param>
            protected TriggerBehaviour(TTrigger trigger, TransitionGuard guard)
            {
                _guard = guard;
                Trigger = trigger;
            }

            public TTrigger Trigger { get; }

            /// <summary>
            /// Guard is the transition guard for this trigger.  Equal to
            /// TransitionGuard.Empty if there is no transition guard
            /// </summary>
            internal TransitionGuard Guard => _guard;

            /// <summary>
            /// GuardConditionsMet is true if all of the guard functions return true
            /// or if there are no guard functions
            /// </summary>
            public bool GuardConditionsMet(params object[] args) => _guard.GuardConditionsMet(args);
        }
    }
}
