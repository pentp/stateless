using System;
using System.Threading.Tasks;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal readonly struct ActivateActionBehaviour
        {
            readonly Delegate _action;
            readonly string _description;

            public ActivateActionBehaviour(Action action, string actionDescription)
            {
                _action = action;
                _description = actionDescription;
            }

            public ActivateActionBehaviour(Func<Task> action, string actionDescription)
            {
                _action = action;
                _description = actionDescription;
            }

            public Reflection.InvocationInfo Description => new(_action, _description, sync: _action is Action);

            public void Execute(TState state, string variant)
            {
                if (_action is Action act)
                {
                    act();
                    return;
                }

                ThrowSyncOverAsync(state, variant);
            }

            private static void ThrowSyncOverAsync(TState state, string variant)
            {
                throw new InvalidOperationException(
                    $"Cannot execute asynchronous action specified in On{variant}Async for '{state}' state. " +
                    $"Use asynchronous version of {variant} [{variant}Async]");
            }

            public Task ExecuteAsync()
            {
                if (_action is Action act)
                {
                    act();
                    return Task.CompletedTask;
                }

                return ((Func<Task>)_action)();
            }
        }
    }
}
