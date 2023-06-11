﻿using System;
using Xunit;

namespace Stateless.Tests
{
    public class IgnoredTriggerBehaviourFixture
    {
        protected bool False(params object[] args)
        {
            return false;
        }

        [Fact]
        public void WhenGuardConditionFalse_IsGuardConditionMetIsFalse()
        {
            var ignored = new IgnoredTriggerBehaviour(new TransitionGuard(False));

            Assert.False(ignored.GuardConditionsMet());
        }

        protected bool True(params object[] args)
        {
            return true;
        }

        [Fact]
        public void WhenGuardConditionTrue_IsGuardConditionMetIsTrue()
        {
            var ignored = new IgnoredTriggerBehaviour(new TransitionGuard(True));

            Assert.True(ignored.GuardConditionsMet());
        }
        [Fact]
        public void IgnoredTriggerMustBeIgnoredSync()
        {
            bool internalActionExecuted = false;
            var stateMachine = new StateMachine<State, Trigger>(State.B);
            stateMachine.Configure(State.A)
                .Permit(Trigger.X, State.C);

            stateMachine.Configure(State.B)
                .SubstateOf(State.A)
                .Ignore(Trigger.X);

            try
            {
                // >>> The following statement should not execute the internal action
                stateMachine.Fire(Trigger.X);
            }
            catch (NullReferenceException)
            {
                internalActionExecuted = true;
            }

            Assert.False(internalActionExecuted);
        }

        [Fact]
        public void IgnoreIfTrueTriggerMustBeIgnored()
        {
            var stateMachine = new StateMachine<State, Trigger>(State.B);
            stateMachine.Configure(State.A)
                .Permit(Trigger.X, State.C);

            stateMachine.Configure(State.B)
                .SubstateOf(State.A)
                .IgnoreIf(Trigger.X, () => true);

                stateMachine.Fire(Trigger.X);

            Assert.Equal(State.B, stateMachine.State);
        }
        [Fact]
        public void IgnoreIfFalseTriggerMustNotBeIgnored()
        {
            var stateMachine = new StateMachine<State, Trigger>(State.B);
            stateMachine.Configure(State.A)
                .Permit(Trigger.X, State.C);

            stateMachine.Configure(State.B)
                .SubstateOf(State.A)
                .IgnoreIf(Trigger.X, () => false);

            stateMachine.Fire(Trigger.X);

            Assert.Equal(State.C, stateMachine.State);
        }
    }
}
