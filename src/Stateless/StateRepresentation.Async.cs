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

                await ExecuteActivationActionsAsync().ConfigureAwait(_retainSynchronizationContext);
            }

            public async Task DeactivateAsync()
            {
                await ExecuteDeactivationActionsAsync().ConfigureAwait(_retainSynchronizationContext);

                if (_superstate != null)
                    await _superstate.DeactivateAsync().ConfigureAwait(_retainSynchronizationContext);
            }

            async Task ExecuteActivationActionsAsync()
            {
                if (ActivateActions != null)
                    foreach (var action in ActivateActions)
                        await action.ExecuteAsync().ConfigureAwait(_retainSynchronizationContext);
            }

            async Task ExecuteDeactivationActionsAsync()
            {
                if (DeactivateActions != null)
                    foreach (var action in DeactivateActions)
                        await action.ExecuteAsync().ConfigureAwait(_retainSynchronizationContext);
            }


            public async Task EnterAsync(Transition transition, params object[] entryArgs)
            {
                if (transition.IsReentry)
                {
                    await ExecuteEntryActionsAsync(transition, entryArgs).ConfigureAwait(_retainSynchronizationContext);
                }
                else if (!Includes(transition.Source))
                {
                    if (_superstate != null && !(transition is InitialTransition))
                        await _superstate.EnterAsync(transition, entryArgs).ConfigureAwait(_retainSynchronizationContext);

                    await ExecuteEntryActionsAsync(transition, entryArgs).ConfigureAwait(_retainSynchronizationContext);
                }
            }
            
            public async Task<Transition> ExitAsync(Transition transition)
            {
                if (transition.IsReentry)
                {
                    await ExecuteExitActionsAsync(transition).ConfigureAwait(_retainSynchronizationContext);
                }
                else if (!Includes(transition.Destination))
                {
                    await ExecuteExitActionsAsync(transition).ConfigureAwait(_retainSynchronizationContext);

                    // Must check if there is a superstate, and if we are leaving that superstate
                    if (_superstate != null)
                    {
                        // Check if destination is within the state list
                        if (IsIncludedIn(transition.Destination))
                        {
                            // Destination state is within the list, exit first superstate only if it is NOT the first
                            if (!StateEquals(_superstate.UnderlyingState, transition.Destination))
                            {
                                return await _superstate.ExitAsync(transition).ConfigureAwait(_retainSynchronizationContext);
                            }
                        }
                        else
                        {
                            // Exit the superstate as well
                            return await _superstate.ExitAsync(transition).ConfigureAwait(_retainSynchronizationContext);
                        }
                    }
                }
                return transition;
            }

            async Task ExecuteEntryActionsAsync(Transition transition, object[] entryArgs)
            {
                if (EntryActions != null)
                    foreach (var action in EntryActions)
                        await action.ExecuteAsync(transition, entryArgs).ConfigureAwait(_retainSynchronizationContext);
            }

            async Task ExecuteExitActionsAsync(Transition transition)
            {
                if (ExitActions != null)
                    foreach (var action in ExitActions)
                        await action.ExecuteAsync(transition).ConfigureAwait(_retainSynchronizationContext);
            }
        }
    }
}

#endif
