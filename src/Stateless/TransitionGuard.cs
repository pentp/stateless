using System;
using System.Collections.Generic;

namespace Stateless
{
    internal readonly struct TransitionGuard
    {
        internal readonly GuardCondition[] Conditions;

        internal TransitionGuard(Tuple<Func<bool>, string>[] guards)
            => Conditions = guards.Length == 0 ? null : Array.ConvertAll(guards, g => new GuardCondition(g.Item1, g.Item2));

        internal TransitionGuard(Func<bool> guard, string description = null)
            => Conditions = new[] { new GuardCondition(guard, description) };

        internal static TransitionGuard Get<TArg0>(Tuple<Func<TArg0, bool>, string>[] guards)
            => new(guards.Length == 0 ? null : Array.ConvertAll(guards, g => new GuardCondition(g.Item1.Unpack, g.Item2)));

        internal static TransitionGuard Get<TArg0, TArg1>(Tuple<Func<TArg0, TArg1, bool>, string>[] guards)
            => new(guards.Length == 0 ? null : Array.ConvertAll(guards, g => new GuardCondition(g.Item1.Unpack, g.Item2)));

        internal static TransitionGuard Get<TArg0, TArg1, TArg2>(Tuple<Func<TArg0, TArg1, TArg2, bool>, string>[] guards)
            => new(guards.Length == 0 ? null : Array.ConvertAll(guards, g => new GuardCondition(g.Item1.Unpack, g.Item2)));

        private TransitionGuard(GuardCondition[] guards) => Conditions = guards;

        internal TransitionGuard(Func<object[], bool> guard, string description = null)
            => Conditions = new[] { new GuardCondition(guard, description) };

        /// <summary>
        /// GuardConditionsMet is true if all of the guard functions return true
        /// or if there are no guard functions
        /// </summary>
        public bool GuardConditionsMet(object[] args)
        {
            if (Conditions != null)
                foreach (var c in Conditions)
                    if (!c.Guard(args))
                        return false;
            return true;
        }

        /// <summary>
        /// UnmetGuardConditions is a list of the descriptions of all guard conditions
        /// whose guard function returns false
        /// </summary>
        public List<string> UnmetGuardConditions(object[] args)
        {
            var res = new List<string>();
            if (Conditions != null)
                foreach (var c in Conditions)
                    if (!c.Guard(args))
                        res.Add(c.Description);
            return res;
        }
    }    
}