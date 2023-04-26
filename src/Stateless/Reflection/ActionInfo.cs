﻿namespace Stateless.Reflection
{
    /// <summary>
    /// Information on entry and exit actions
    /// </summary>
    public class ActionInfo
    {
        /// <summary>
        /// The method invoked during the action (entry or exit)
        /// </summary>
        public InvocationInfo Method { get; internal set; }

        /// <summary>
        /// If non-null, specifies the "from" trigger that must be present for this method to be invoked
        /// </summary>
        public string FromTrigger { get; internal set; }

        internal static ActionInfo Create<TState, TTrigger>(StateMachine<TState, TTrigger>.EntryActionBehavior entryAction)
        {
            var fromTrigger = entryAction switch
            {
                StateMachine<TState, TTrigger>.EntryActionBehavior.SyncFrom syncFrom => syncFrom.Trigger.ToString(),
                StateMachine<TState, TTrigger>.EntryActionBehavior.AsyncFrom asyncFrom => asyncFrom.Trigger.ToString(),
                _ => null
            };

            return new ActionInfo(entryAction.Description, fromTrigger);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ActionInfo(InvocationInfo method, string fromTrigger)
        {
            Method = method;
            FromTrigger = fromTrigger;
        }
    }
}
