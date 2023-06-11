#if TASKS

using System;
using System.Threading.Tasks;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal partial class StateRepresentation
        {
            public async Task ActivateAsync()
            {
                if (_superstate != null)
                    await _superstate.ActivateAsync().ConfigureAwait(_retainSynchronizationContext);

                if (ActivateActions != null)
                    foreach (var action in ActivateActions)
                        await action.ExecuteAsync().ConfigureAwait(_retainSynchronizationContext);
            }

            public async Task DeactivateAsync()
            {
                if (DeactivateActions != null)
                    foreach (var action in DeactivateActions)
                        await action.ExecuteAsync().ConfigureAwait(_retainSynchronizationContext);

                if (_superstate != null)
                    await _superstate.DeactivateAsync().ConfigureAwait(_retainSynchronizationContext);
            }

            public async Task EnterAsync(Transition transition, params object[] entryArgs)
            {
                if (transition.IsReentry)
                {
                }
                else if (!Includes(transition.Source))
                {
                    if (_superstate != null && !(transition is InitialTransition))
                        await _superstate.EnterAsync(transition, entryArgs).ConfigureAwait(_retainSynchronizationContext);
                }
                else return;

                if (EntryActions != null)
                    foreach (var action in EntryActions)
                        await action.ExecuteAsync(transition, entryArgs).ConfigureAwait(_retainSynchronizationContext);
            }

            public async Task ExitAsync(Transition transition)
            {
                if (transition.IsReentry)
                {
                    if (ExitActions != null)
                        await ExecuteExitActionsAsync(transition).ConfigureAwait(_retainSynchronizationContext);
                }
                else if (!Includes(transition.Destination))
                {
                    if (ExitActions != null)
                        await ExecuteExitActionsAsync(transition).ConfigureAwait(_retainSynchronizationContext);

                    // Must check if there is a superstate, and if we are leaving that superstate
                    if (_superstate != null)
                    {
                        // Check if destination is within the state list
                        if (_superstate.IsIncludedIn(transition.Destination))
                        {
                            // Destination state is within the list, exit first superstate only if it is NOT the first
                            if (StateEquals(_superstate.UnderlyingState, transition.Destination))
                                return;
                        }

                        // Exit the superstate as well
                        await _superstate.ExitAsync(transition).ConfigureAwait(_retainSynchronizationContext);
                    }
                }
            }

            async Task ExecuteExitActionsAsync(Transition transition)
            {
                foreach (var action in ExitActions)
                    await action.ExecuteAsync(transition).ConfigureAwait(_retainSynchronizationContext);
            }
        }
    }
}

#endif
