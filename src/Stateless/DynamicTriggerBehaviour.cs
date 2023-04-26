using System;
using System.Linq;
using Stateless.Reflection;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal sealed class DynamicTriggerBehaviour : TriggerBehaviour
        {
            private readonly Delegate _destination;
            private readonly string _description;
            private readonly DynamicStateInfos _possibleStates;

            private DynamicTriggerBehaviour(TTrigger trigger, Delegate destination, TransitionGuard guard, string description, DynamicStateInfos possibleStates)
                : base(trigger, guard)
            {
                _destination = destination ?? throw new ArgumentNullException(nameof(destination));
                _description = description;
                _possibleStates = possibleStates;
            }

            public DynamicTriggerBehaviour(TTrigger trigger, Func<TState> destination, TransitionGuard guard, string destinationDescription, DynamicStateInfos possibleStates)
                : this(trigger, (Delegate)destination, guard, destinationDescription, possibleStates) { }

            public DynamicTriggerBehaviour(TTrigger trigger, Func<object[], TState> destination, TransitionGuard guard, string destinationDescription, DynamicStateInfos possibleStates)
                : this(trigger, (Delegate)destination, guard, destinationDescription, possibleStates) { }

            public TState Destination(object[] args) => _destination switch
            {
                Func<TState> func => func(),
                var func => ((Func<object[], TState>)func)(args),
            };

            internal DynamicTransitionInfo TransitionInfo
                => DynamicTransitionInfo.Create(Trigger, Guard.Conditions?.Select(x => x.MethodDescription), new(_destination, _description), _possibleStates);
        }
    }
}
