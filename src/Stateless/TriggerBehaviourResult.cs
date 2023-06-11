using System.Collections.Generic;

namespace Stateless
{
    internal readonly struct TriggerBehaviourResult
    {
        public TriggerBehaviourResult(TriggerBehaviour handler, List<string> unmetGuardConditions)
        {
            Handler = handler;
            UnmetGuardConditions = unmetGuardConditions;
        }

        public readonly TriggerBehaviour Handler;
        public readonly List<string> UnmetGuardConditions;
    }
}
