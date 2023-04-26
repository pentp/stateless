using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        struct OnTransitionedEvent
        {
            Action<Transition> _onTransitioned;
            List<Func<Transition, Task>> _onTransitionedAsync;

            public void Invoke(Transition transition)
            {
                if (_onTransitionedAsync != null)
                    ThrowSyncOverAsync();

                _onTransitioned?.Invoke(transition);
            }

            public Action<Transition> GetInvoke()
            {
                if (_onTransitionedAsync != null)
                    ThrowSyncOverAsync();

                return _onTransitioned;
            }

            private static void ThrowSyncOverAsync()
            {
                throw new InvalidOperationException(
                    "Cannot execute asynchronous action specified as OnTransitioned callback. " +
                    "Use asynchronous version of Fire [FireAsync]");
            }

#if TASKS
            public async Task InvokeAsync(Transition transition, bool retainSynchronizationContext)
            {
                _onTransitioned?.Invoke(transition);

                if (_onTransitionedAsync != null)
                    foreach (var callback in _onTransitionedAsync)
                        await callback(transition).ConfigureAwait(retainSynchronizationContext);
            }
#endif

            public void Register(Action<Transition> action)
            {
                _onTransitioned += action;
            }

            public void Register(Func<Transition, Task> action)
            {
                (_onTransitionedAsync ??= new()).Add(action);
            }
        }
    }
}
