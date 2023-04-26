using System;
using System.Threading.Tasks;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal abstract class EntryActionBehavior
        {
            protected EntryActionBehavior(Reflection.InvocationInfo description)
            {
                Description = description;
            }

            public Reflection.InvocationInfo Description { get; }

            public abstract void Execute(Transition transition, object[] args);
            public abstract Task ExecuteAsync(Transition transition, object[] args);

            public class Sync : EntryActionBehavior
            {
                readonly Delegate _action;

                public Sync(Action action, Reflection.InvocationInfo description) : base(description) => _action = action;
                public Sync(Action<Transition> action, Reflection.InvocationInfo description) : base(description) => _action = action;
                public Sync(Action<object[]> action, Reflection.InvocationInfo description) : base(description) => _action = action;
                public Sync(Action<Transition, object[]> action, Reflection.InvocationInfo description) : base(description) => _action = action;

                public override void Execute(Transition transition, object[] args)
                {
                    switch (_action)
                    {
                        case Action act: act(); break;
                        case Action<Transition> act2: act2(transition); break;
                        case Action<object[]> act3: act3(args); break;
                        default: ((Action<Transition, object[]>)_action)(transition, args); break;
                    }
                }

                public override Task ExecuteAsync(Transition transition, object[] args)
                {
                    Execute(transition, args);
                    return TaskResult.Done;
                }
            }

            public sealed class SyncFrom : Sync
            {
                internal readonly TTrigger Trigger;

                public SyncFrom(TTrigger trigger, Action action, Reflection.InvocationInfo description) : base(action, description) => Trigger = trigger;
                public SyncFrom(TTrigger trigger, Action<Transition> action, Reflection.InvocationInfo description) : base(action, description) => Trigger = trigger;
                public SyncFrom(TTrigger trigger, Action<object[]> action, Reflection.InvocationInfo description) : base(action, description) => Trigger = trigger;
                public SyncFrom(TTrigger trigger, Action<Transition, object[]> action, Reflection.InvocationInfo description) : base(action, description) => Trigger = trigger;

                public override void Execute(Transition transition, object[] args)
                {
                    if (transition.Trigger.Equals(Trigger))
                        base.Execute(transition, args);
                }
            }

            public class Async : EntryActionBehavior
            {
                readonly Delegate _action;

                public Async(Func<object[], Task> action, Reflection.InvocationInfo description) : base(description) => _action = action;
                public Async(Func<Transition, object[], Task> action, Reflection.InvocationInfo description) : base(description) => _action = action;

                public override void Execute(Transition transition, object[] args)
                {
                    throw new InvalidOperationException(
                        $"Cannot execute asynchronous action specified in OnEntry event for '{transition.Destination}' state. " +
                        $"Use asynchronous version of Fire [FireAsync]");
                }

                public override Task ExecuteAsync(Transition transition, object[] args) => _action switch
                {
                    Func<object[], Task> act => act(args),
                    _ => ((Func<Transition, object[], Task>)_action)(transition, args),
                };
            }
            
            public sealed class AsyncFrom : Async
            {
                internal readonly TTrigger Trigger;

                public AsyncFrom(TTrigger trigger, Func<object[], Task> action, Reflection.InvocationInfo description)
                    : base(action, description) => Trigger = trigger;

                public AsyncFrom(TTrigger trigger, Func<Transition, object[], Task> action, Reflection.InvocationInfo description)
                    : base(action, description) => Trigger = trigger;

                public override void Execute(Transition transition, object[] args)
                {
                    if (transition.Trigger.Equals(Trigger))
                        base.Execute(transition, args);
                }

                public override Task ExecuteAsync(Transition transition, object[] args)
                {
                    if (transition.Trigger.Equals(Trigger))
                        return base.ExecuteAsync(transition, args);
                    return TaskResult.Done;
                }
            }
        }
    }
}
