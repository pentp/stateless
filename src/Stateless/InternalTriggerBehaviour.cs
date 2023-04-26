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

                public Sync(TTrigger trigger, Func<object[], bool> guard, Action action, string guardDescription = null)
                    : base(trigger, new(guard, guardDescription)) => _action = action;

                public Sync(TTrigger trigger, Func<object[], bool> guard, Action<Transition> action, string guardDescription = null)
                    : base(trigger, new(guard, guardDescription)) => _action = action;

                public Sync(TTrigger trigger, Func<object[], bool> guard, Action<Transition, object[]> action, string guardDescription = null)
                    : base(trigger, new(guard, guardDescription)) => _action = action;

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

                public Async(TTrigger trigger, Func<bool> guard, Func<Task> action, string guardDescription = null)
                    : base(trigger, new TransitionGuard(guard, guardDescription)) => _action = action;

                public Async(TTrigger trigger, Func<bool> guard, Func<Transition, Task> action, string guardDescription = null)
                    : base(trigger, new TransitionGuard(guard, guardDescription)) => _action = action;

                public Async(TTrigger trigger, Func<bool> guard, Func<Transition, object[], Task> action, string guardDescription = null)
                    : base(trigger, new TransitionGuard(guard, guardDescription)) => _action = action;

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