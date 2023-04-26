using System;
using System.Threading.Tasks;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal abstract class InternalTriggerBehaviour : TriggerBehaviour
        {
            protected InternalTriggerBehaviour(TTrigger trigger, TransitionGuard guard) : base(trigger, guard)
            {
            }

            public sealed class Sync : InternalTriggerBehaviour
            {
                private readonly Delegate _action;

                public Sync(TTrigger trigger, TransitionGuard guard, Action action) : base(trigger, guard) => _action = action;
                public Sync(TTrigger trigger, TransitionGuard guard, Action<Transition> action) : base(trigger, guard) => _action = action;
                public Sync(TTrigger trigger, TransitionGuard guard, Action<Transition, object[]> action) : base(trigger, guard) => _action = action;

                public void Execute(Transition transition, object[] args)
                {
                    switch (_action)
                    {
                        case Action act: act(); break;
                        case Action<Transition> act2: act2(transition); break;
                        default: ((Action<Transition, object[]>)_action)(transition, args); break;
                    }
                }
            }

            public sealed class Async : InternalTriggerBehaviour
            {
                private readonly Delegate _action;

                public Async(TTrigger trigger, TransitionGuard guard, Func<Task> action) : base(trigger, guard) => _action = action;
                public Async(TTrigger trigger, TransitionGuard guard, Func<Transition, Task> action) : base(trigger, guard) => _action = action;
                public Async(TTrigger trigger, TransitionGuard guard, Func<Transition, object[], Task> action) : base(trigger, guard) => _action = action;

                public Task ExecuteAsync(Transition transition, object[] args) => _action switch
                {
                    Func<Task> act => act(),
                    Func<Transition, Task> act2 => act2(transition),
                    _ => ((Func<Transition, object[], Task>)_action)(transition, args),
                };
            }
        }
    }
}