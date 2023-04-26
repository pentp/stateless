using System;

namespace Stateless
{
    internal readonly struct GuardCondition
    {
        private readonly Delegate _guard;
        private readonly string _description;

        internal GuardCondition(Func<bool> guard, string description) : this((Delegate)guard, description) { }

        internal GuardCondition(Func<object[], bool> guard, string description) : this((Delegate)guard, description) { }

        private GuardCondition(Delegate guard, string description)
        {
            _guard = guard;
            _description = description;
        }

        internal bool Guard(params object[] args) => _guard is Func<bool> simple ? simple() : ((Func<object[], bool>)_guard)(args);

        // Return the description of the guard method: the caller-defined description if one
        // was provided, else the name of the method itself
        internal string Description => _description ?? Reflection.InvocationInfo.GetDescription(_guard);

        internal Reflection.InvocationInfo MethodDescription => new(_guard, _description);
    }
}