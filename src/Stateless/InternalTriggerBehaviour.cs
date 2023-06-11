using System;
using System.Threading.Tasks;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal sealed class InternalTriggerBehaviour : TriggerBehaviour
        {
            private readonly Delegate _action;

            public InternalTriggerBehaviour(TransitionGuard guard, Action action) : base(guard) => _action = action;
            public InternalTriggerBehaviour(TransitionGuard guard, Action<Transition> action) : base(guard) => _action = action;
            public InternalTriggerBehaviour(TransitionGuard guard, Action<Transition, object[]> action) : base(guard) => _action = action;

            public InternalTriggerBehaviour(TransitionGuard guard, Func<Task> action) : base(guard) => _action = action;
            public InternalTriggerBehaviour(TransitionGuard guard, Func<Transition, Task> action) : base(guard) => _action = action;
            public InternalTriggerBehaviour(TransitionGuard guard, Func<Transition, object[], Task> action) : base(guard) => _action = action;

            public void Execute(TState state, TTrigger trigger, object[] args)
            {
                switch (_action)
                {
                    case Action act: act(); break;
                    case Action<Transition> act2: act2(new(state, state, trigger, args)); break;
                    case Action<Transition, object[]> act3: act3(new(state, state, trigger, args), args); break;
                    default: ThrowSyncOverAsync(); break;
                }
            }

            private static void ThrowSyncOverAsync() => throw new InvalidOperationException("Running Async internal actions in synchronous mode is not allowed");

            public Task ExecuteAsync(TState state, TTrigger trigger, object[] args)
            {
                switch (_action)
                {
                    case Func<Task> act: return act();
                    case Func<Transition, Task> act2: return act2(new(state, state, trigger, args));
                    case Func<Transition, object[], Task> act3: return act3(new(state, state, trigger, args), args);
                }
                Execute(state, trigger, args);
                return Task.CompletedTask;
            }
        }
    }
}