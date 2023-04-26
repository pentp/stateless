using System;
using System.Collections.Generic;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal sealed partial class StateRepresentation
        {
            readonly TState _state;
            private readonly bool _retainSynchronizationContext;

            internal readonly Dictionary<TTrigger, List<TriggerBehaviour>> TriggerBehaviours = new();
            internal List<EntryActionBehavior> EntryActions;
            internal List<ExitActionBehavior> ExitActions;
            internal List<ActivateActionBehaviour> ActivateActions;
            internal List<ActivateActionBehaviour> DeactivateActions;

            StateRepresentation _superstate; // null

            internal List<StateRepresentation> Substates;
            public TState InitialTransitionTarget { get; private set; }

            public StateRepresentation(TState state, bool retainSynchronizationContext = false)
            {
                _state = state;
                _retainSynchronizationContext = retainSynchronizationContext;
            }

            public bool CanHandle(TTrigger trigger)
            {
                var found = false;
                for (var state = this; state != null; state = state._superstate)
                {
                    if (!state.TriggerBehaviours.TryGetValue(trigger, out var possible))
                        continue;

                    for (int i = 0; i < possible.Count; i++)
                    {
                        if (!possible[i].GuardConditionsMet()) continue;
                        if (found) state.ThrowMultipleTransitionsPermitted(trigger);
                        found = true;
                    }

                    if (found) break;
                }
                return found;
            }

            public bool CanHandle(TTrigger trigger, out ICollection<string> unmetGuards)
            {
                bool handlerFound = TryFindHandler(trigger, null, out TriggerBehaviourResult result);
                unmetGuards = result.UnmetGuardConditions;
                return handlerFound;
            }

            public bool TryFindHandler(TTrigger trigger, object[] args, out TriggerBehaviourResult handler)
            {
                args ??= Array.Empty<object>();

                if (TryFindLocalHandler(trigger, args, out handler))
                    return true;

                if (Superstate?.TryFindHandler(trigger, args, out var superStateHandler) == true)
                {
                    handler = superStateHandler;
                    return true;
                }

                return false;
            }

            private bool TryFindLocalHandler(TTrigger trigger, object[] args, out TriggerBehaviourResult handlerResult)
            {
                handlerResult = default;

                // Get list of candidate trigger handlers
                if (!TriggerBehaviours.TryGetValue(trigger, out var possible))
                    return false;

                if (possible.Count == 1)
                {
                    var h = possible[0];
                    handlerResult = new(h, h.Guard.UnmetGuardConditions(args));
                    return handlerResult.UnmetGuardConditions.Count == 0;
                }

                // Guard functions are executed here
                var actual = new TriggerBehaviourResult[possible.Count];
                for (int i = 0; i < actual.Length; i++)
                {
                    var h = possible[i];
                    actual[i] = new(h, h.Guard.UnmetGuardConditions(args));
                }

                // Find a handler for the trigger
                var found = false;
                foreach (var r in actual)
                    if (r.UnmetGuardConditions.Count == 0)
                    {
                        if (found) ThrowMultipleTransitionsPermitted(trigger);
                        handlerResult = r;
                        found = true;
                    }

                if (found)
                    return true;

                handlerResult = TryFindLocalHandlerResultWithUnmetGuardConditions(actual);
                return false;
            }

            private static TriggerBehaviourResult TryFindLocalHandlerResultWithUnmetGuardConditions(TriggerBehaviourResult[] results)
            {
                var res = results[0];

                // Add other unmet conditions to first result
                for (var i = 1; i < results.Length; i++)
                    foreach (var condition in results[i].UnmetGuardConditions)
                        if (!res.UnmetGuardConditions.Contains(condition))
                            res.UnmetGuardConditions.Add(condition);

                return res;
            }

            private void ThrowMultipleTransitionsPermitted(TTrigger trigger)
            {
                var message = string.Format(StateRepresentationResources.MultipleTransitionsPermitted, trigger, _state);
                throw new InvalidOperationException(message);
            }

            public void AddActivateAction(ActivateActionBehaviour action) => (ActivateActions ??= new()).Add(action);

            public void AddDeactivateAction(ActivateActionBehaviour action) => (DeactivateActions ??= new()).Add(action);

            public void AddEntryAction(EntryActionBehavior action) => (EntryActions ??= new()).Add(action);

            public void AddEntryAction(TTrigger trigger, EntryActionBehavior action) => AddEntryAction(action.EntryFrom(trigger));

            public void AddExitAction(ExitActionBehavior action) => (ExitActions ??= new()).Add(action);

            public void Activate()
            {
                _superstate?.Activate();

                if (ActivateActions != null)
                    foreach (var action in ActivateActions)
                        action.Execute(_state, "Activate");
            }

            public void Deactivate()
            {
                if (DeactivateActions != null)
                    foreach (var action in DeactivateActions)
                        action.Execute(_state, "Deactivate");

                _superstate?.Deactivate();
            }

            public void Enter(Transition transition, params object[] entryArgs)
            {
                if (transition.IsReentry)
                {
                    ExecuteEntryActions(transition, entryArgs);
                }
                else if (!Includes(transition.Source))
                {
                    if (_superstate != null && !(transition is InitialTransition))
                        _superstate.Enter(transition, entryArgs);

                    ExecuteEntryActions(transition, entryArgs);
                }
            }

            public Transition Exit(Transition transition)
            {
                if (transition.IsReentry)
                {
                    ExecuteExitActions(transition);
                }
                else if (!Includes(transition.Destination))
                {
                    ExecuteExitActions(transition);

                    // Must check if there is a superstate, and if we are leaving that superstate
                    if (_superstate != null)
                    {
                        // Check if destination is within the state list
                        if (IsIncludedIn(transition.Destination))
                        {
                            // Destination state is within the list, exit first superstate only if it is NOT the the first
                            if (!StateEquals(_superstate.UnderlyingState, transition.Destination))
                            {
                                return _superstate.Exit(transition);
                            }
                        }
                        else
                        {
                            // Exit the superstate as well
                            return _superstate.Exit(transition);
                        }
                    }
                }
                return transition;
            }

            void ExecuteEntryActions(Transition transition, object[] entryArgs)
            {
                if (EntryActions != null)
                    foreach (var action in EntryActions)
                        action.Execute(transition, entryArgs);
            }

            void ExecuteExitActions(Transition transition)
            {
                if (ExitActions != null)
                    foreach (var action in ExitActions)
                        action.Execute(transition);
            }

            public void AddTriggerBehaviour(TriggerBehaviour triggerBehaviour)
            {
                if (!TriggerBehaviours.TryGetValue(triggerBehaviour.Trigger, out var allowed))
                {
                    TriggerBehaviours.Add(triggerBehaviour.Trigger, allowed = new());
                }
                allowed.Add(triggerBehaviour);
            }

            public StateRepresentation Superstate
            {
                get
                {
                    return _superstate;
                }
                set
                {
                    _superstate = value;
                }
            }

            public TState UnderlyingState
            {
                get
                {
                    return _state;
                }
            }

            public void AddSubstate(StateRepresentation substate)
            {
                (Substates ??= new()).Add(substate);
            }

            public bool HasSubstate(TState state)
            {
                if (Substates != null)
                    foreach (var s in Substates)
                        if (StateEquals(s.UnderlyingState, state))
                            return true;
                return false;
            }

            /// <summary>
            /// Checks if the state is in the set of this state or its sub-states
            /// </summary>
            /// <param name="state">The state to check</param>
            /// <returns>True if included</returns>
            public bool Includes(TState state)
            {
                if (StateEquals(_state, state))
                    return true;

                if (Substates != null)
                    foreach (var s in Substates)
                        if (s.Includes(state))
                            return true;

                return false;
            }

            /// <summary>
            /// Checks if the state is in the set of this state or a super-state
            /// </summary>
            /// <param name="state">The state to check</param>
            /// <returns>True if included</returns>
            public bool IsIncludedIn(TState state)
            {
                return StateEquals(_state, state) || _superstate?.IsIncludedIn(state) is true;
            }

            public List<TTrigger> GetPermittedTriggers(params object[] args)
            {
                var result = new List<TTrigger>();
                HashSet<TTrigger> unique = null;
                for (var state = this; state != null; state = state._superstate)
                {
                    if (result.Count > 0)
                        unique ??= new(result);

                    foreach (var t in state.TriggerBehaviours)
                    {
                        foreach (var a in t.Value)
                        {
                            if (a.Guard.GuardConditionsMet(args))
                            {
                                if (unique is null || unique.Add(t.Key))
                                    result.Add(t.Key);
                                break;
                            }
                        }
                    }
                }
                return result;
            }

            internal void SetInitialTransition(TState state)
            {
                InitialTransitionTarget = state;
                HasInitialTransition = true;
            }
            public bool HasInitialTransition { get; private set; }
        }
    }
}
