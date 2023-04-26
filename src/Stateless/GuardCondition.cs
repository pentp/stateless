using System;

namespace Stateless
{
    internal readonly struct GuardCondition
    {
        private readonly Delegate _guard;
        internal readonly Reflection.InvocationInfo MethodDescription;

        internal GuardCondition(Func<bool> guard, string description) : this((Delegate)guard, description) { }

        internal GuardCondition(Func<object[], bool> guard, string description) : this((Delegate)guard, description) { }

        private GuardCondition(Delegate guard, string description)
        {
            _guard = guard;
            MethodDescription = Reflection.InvocationInfo.Create(guard, description);
        }

        internal bool Guard(object[] args) => _guard is Func<bool> simple ? simple() : ((Func<object[], bool>)_guard)(args);

        // Return the description of the guard method: the caller-defined description if one
        // was provided, else the name of the method itself
        internal string Description => MethodDescription.Description;
    }
}