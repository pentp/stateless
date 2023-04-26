using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal readonly struct EntryActionBehavior
        {
            readonly Delegate _action;
            readonly string _description;

            public readonly TTrigger Trigger;
            public readonly bool From;

            private EntryActionBehavior(Delegate action, string description)
            {
                _action = action;
                _description = description;
            }

            public EntryActionBehavior(Action action, string description) : this((Delegate)action, description) { }
            public EntryActionBehavior(Action<Transition> action, string description) : this((Delegate)action, description) { }
            public EntryActionBehavior(Action<object[]> action, string description) : this((Delegate)action, description) { }
            public EntryActionBehavior(Action<Transition, object[]> action, string description) : this((Delegate)action, description) { }

            public EntryActionBehavior(Func<Task> action, string description) : this((Delegate)action, description) { }
            public EntryActionBehavior(Func<Transition, Task> action, string description) : this((Delegate)action, description) { }
            public EntryActionBehavior(Func<object[], Task> action, string description) : this((Delegate)action, description) { }
            public EntryActionBehavior(Func<Transition, object[], Task> action, string description) : this((Delegate)action, description) { }

            private EntryActionBehavior(Delegate action, string description, TTrigger trigger) : this(action, description)
            {
                Trigger = trigger;
                From = true;
            }

            public EntryActionBehavior EntryFrom(TTrigger trigger) => new(_action, _description, trigger);

            public Reflection.InvocationInfo Description => new(_action, _description, sync: _action is Action or Action<Transition> or Action<object[]> or Action<Transition, object[]>);

            private static bool TriggerEquals(TTrigger x, TTrigger y) => EqualityComparer<TTrigger>.Default.Equals(x, y);

            public void Execute(Transition transition, object[] args)
            {
                if (From && !TriggerEquals(transition.Trigger, Trigger))
                    return;

                switch (_action)
                {
                    case Action act: act(); break;
                    case Action<Transition> act2: act2(transition); break;
                    case Action<object[]> act3: act3(args); break;
                    case Action<Transition, object[]> act4: act4(transition, args); break;
                    default: ThrowSyncOverAsync(transition); break;
                }
            }

            private static void ThrowSyncOverAsync(Transition transition)
            {
                throw new InvalidOperationException(
                    $"Cannot execute asynchronous action specified in OnEntry event for '{transition.Destination}' state. " +
                    $"Use asynchronous version of Fire [FireAsync]");
            }

            public Task ExecuteAsync(Transition transition, object[] args)
            {
                if (From && !TriggerEquals(transition.Trigger, Trigger))
                    return Task.CompletedTask;

                switch (_action)
                {
                    case Func<Task> act: return act();
                    case Func<Transition, Task> act2: return act2(transition);
                    case Func<object[], Task> act3: return act3(args);
                    case Func<Transition, object[], Task> act4: return act4(transition, args);
                }
                Execute(transition, args);
                return Task.CompletedTask;
            }
        }
    }
}
