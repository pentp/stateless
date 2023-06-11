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

            private DynamicTriggerBehaviour(Delegate destination, TransitionGuard guard, string description, DynamicStateInfos possibleStates) : base(guard)
            {
                _destination = destination ?? throw new ArgumentNullException(nameof(destination));
                _description = description;
                _possibleStates = possibleStates;
            }

            public DynamicTriggerBehaviour(Func<TState> destination, TransitionGuard guard, string destinationDescription, DynamicStateInfos possibleStates)
                : this((Delegate)destination, guard, destinationDescription, possibleStates) { }

            public DynamicTriggerBehaviour(Func<object[], TState> destination, TransitionGuard guard, string destinationDescription, DynamicStateInfos possibleStates)
                : this((Delegate)destination, guard, destinationDescription, possibleStates) { }

            public TState Destination(object[] args) => _destination switch
            {
                Func<TState> func => func(),
                var func => ((Func<object[], TState>)func)(args),
            };

            internal DynamicTransitionInfo GetTransitionInfo(TTrigger trigger)
                => new(trigger, Guard.Conditions?.Select(x => x.MethodDescription), new(_destination, _description), _possibleStates);
        }
    }
}
