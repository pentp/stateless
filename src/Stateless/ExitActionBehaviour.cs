using System;
using System.Threading.Tasks;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal readonly struct ExitActionBehavior
        {
            readonly Delegate _action;
            readonly string _description;

            private ExitActionBehavior(Delegate action, string actionDescription)
            {
                _action = action;
                _description = actionDescription;
            }

            public ExitActionBehavior(Action action, string actionDescription) : this((Delegate)action, actionDescription) { }
            public ExitActionBehavior(Action<Transition> action, string actionDescription) : this((Delegate)action, actionDescription) { }
            public ExitActionBehavior(Func<Task> action, string actionDescription) : this((Delegate)action, actionDescription) { }
            public ExitActionBehavior(Func<Transition, Task> action, string actionDescription) : this((Delegate)action, actionDescription) { }

            public Reflection.InvocationInfo Description => new(_action, _description, sync: _action is Action or Action<Transition>);

            public void Execute(Transition transition)
            {
                switch (_action)
                {
                    case Action act: act(); break;
                    case Action<Transition> act2: act2(transition); break;
                    default: ThrowSyncOverAsync(transition); break;
                }
            }

            private static void ThrowSyncOverAsync(Transition transition)
            {
                throw new InvalidOperationException(
                    $"Cannot execute asynchronous action specified in OnExit event for '{transition.Source}' state. " +
                     "Use asynchronous version of Fire [FireAsync]");
            }

            public Task ExecuteAsync(Transition transition)
            {
                switch (_action)
                {
                    case Func<Task> act: return act();
                    case Func<Transition, Task> act2: return act2(transition);
                }
                Execute(transition);
                return Task.CompletedTask;
            }
        }
    }
}
