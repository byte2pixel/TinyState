using TUnit.Assertions.AssertConditions.Throws;

namespace TinyState.Tests;

public enum States
{
    Start,
    Processing,
    Completed,
}

public enum Triggers
{
    StartProcessing,
    CompleteProcessing,
}

public class StateMachineTests
{
    # region Happy Path (Success/Expected Usage)

    [Test]
    public async Task StateMachine_BasicTransition_Works()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine
            .Configure(States.Start)
            .Trigger(Triggers.StartProcessing)
            .TransitionTo(States.Processing);
        machine.Configure(States.Processing);
        await Assert.That(machine.State).IsEqualTo(States.Start);
# pragma warning disable S6966
        // at least one test that calls Fire directly.
        // ReSharper disable once MethodHasAsyncOverload
        machine.Fire(Triggers.StartProcessing);
# pragma warning restore S6966
        await Assert.That(machine.State).IsEqualTo(States.Processing);
    }

    [Test]
    public async Task StateMachine_ReConfigureState_Works()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine
            .Configure(States.Start)
            .Trigger(Triggers.StartProcessing)
            .TransitionTo(States.Processing);
        machine.Configure(States.Processing);
        // reconfigure Start + StartProcessing to go to Completed
        machine
            .Configure(States.Start)
            .Trigger(Triggers.StartProcessing)
            .TransitionTo(States.Completed);
        machine.Configure(States.Completed);
        await machine.FireAsync(Triggers.StartProcessing);
        await Assert.That(machine.State).IsEqualTo(States.Completed);
    }

    [Test]
    public async Task StateMachine_MultipleTransitions_Works()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine
            .Configure(States.Start)
            .Trigger(Triggers.StartProcessing)
            .TransitionTo(States.Processing);
        machine
            .Configure(States.Processing)
            .Trigger(Triggers.StartProcessing)
            .TransitionTo(States.Start);
        machine.Configure(States.Processing);
        await Assert.That(machine.State).IsEqualTo(States.Start);
        await machine.FireAsync(Triggers.StartProcessing);
        await Assert.That(machine.State).IsEqualTo(States.Processing);
        await machine.FireAsync(Triggers.StartProcessing);
        await Assert.That(machine.State).IsEqualTo(States.Start);
    }

    [Test]
    public async Task StateMachine_BasicTransition_FireAsync_Works()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine
            .Configure(States.Start)
            .Trigger(Triggers.StartProcessing)
            .TransitionTo(States.Processing);
        machine.Configure(States.Processing);
        await Assert.That(machine.State).IsEqualTo(States.Start);
        await machine.FireAsync(Triggers.StartProcessing);
        await Assert.That(machine.State).IsEqualTo(States.Processing);
    }

    [Test]
    public async Task StateMachine_Hooks_Are_Called_In_Order()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine
            .Configure(States.Start)
            .OnExitAsync(async () =>
            {
                await Task.Delay(1);
                HookCalls.Add("exitAsync:start");
            })
            .OnExit(() => HookCalls.Add("exit:start"))
            .OnTransition((tr, st) => HookCalls.Add($"transition:{tr}->{st}"))
            .OnTransitionAsync(
                async (tr, st) =>
                {
                    await Task.Delay(1);
                    HookCalls.Add($"transitionAsync:{tr}->{st}");
                }
            )
            .Trigger(Triggers.StartProcessing)
            .TransitionTo(States.Processing);
        machine
            .Configure(States.Processing)
            .OnEnter(() => HookCalls.Add("enter:processing"))
            .OnEnterAsync(async () =>
            {
                await Task.Delay(1);
                HookCalls.Add("enterAsync:processing");
            });

        var expected = new[]
        {
            "exitAsync:start",
            "exit:start",
            "transitionAsync:StartProcessing->Processing",
            "transition:StartProcessing->Processing",
            "enterAsync:processing",
            "enter:processing",
        };
        HookCalls.Clear();
        await machine.FireAsync(Triggers.StartProcessing);
        await Assert.That(HookCalls).IsEquivalentTo(expected);
    }

    private static readonly List<string> HookCalls = [];

    [Test]
    public async Task StateMachine_When_Guard_True_Allows_Transition()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine
            .Configure(States.Start)
            .Trigger(Triggers.StartProcessing)
            .When(() => true)
            .TransitionTo(States.Processing);
        machine.Configure(States.Processing);
        await Assert.That(machine.State).IsEqualTo(States.Start);
        await machine.FireAsync(Triggers.StartProcessing);
        await Assert.That(machine.State).IsEqualTo(States.Processing);
    }

    [Test]
    public async Task StateMachine_WhenAsync_Guard_True_Allows_Transition()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine
            .Configure(States.Start)
            .Trigger(Triggers.StartProcessing)
            .WhenAsync(async () =>
            {
                await Task.Delay(1);
                return true;
            })
            .TransitionTo(States.Processing);
        machine.Configure(States.Processing);
        await Assert.That(machine.State).IsEqualTo(States.Start);
        await machine.FireAsync(Triggers.StartProcessing);
        await Assert.That(machine.State).IsEqualTo(States.Processing);
    }

    #endregion

    #region Sad Path (Error/Exception Cases)

    [Test]
    public async Task StateMachine_Throws_If_TriggerNotConfigured()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        await Assert.That(machine.State).IsEqualTo(States.Start);
        await Assert.That(() => machine.Fire(Triggers.StartProcessing)).ThrowsException();
    }

    [Test]
    public async Task StateMachine_Throws_If_NoTransitionForTrigger()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine
            .Configure(States.Start)
            .Trigger(Triggers.CompleteProcessing)
            .TransitionTo(States.Completed);
        await Assert.That(() => machine.Fire(Triggers.StartProcessing)).ThrowsException();
    }

    [Test]
    public async Task StateMachine_RepeatedTrigger_Throws()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine
            .Configure(States.Start)
            .Trigger(Triggers.StartProcessing)
            .TransitionTo(States.Processing);
        machine.Configure(States.Processing);
        await Assert.That(machine.State).IsEqualTo(States.Start);
        await machine.FireAsync(Triggers.StartProcessing);
        await Assert.That(machine.State).IsEqualTo(States.Processing);
        await Assert.That(() => machine.Fire(Triggers.StartProcessing)).ThrowsException();
    }

    [Test]
    public void StateMachine_Throws_If_TransitionTo_Called_Without_Trigger()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        Assert.Throws<InvalidOperationException>(() =>
            machine.Configure(States.Start).TransitionTo(States.Processing)
        );
    }

    [Test]
    public async Task StateMachine_Throws_If_TargetState_NotConfigured1()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine
            .Configure(States.Start)
            .Trigger(Triggers.StartProcessing)
            .TransitionTo(States.Processing);
        await Assert.That(machine.State).IsEqualTo(States.Start);
        await Assert
            .That(async () => await machine.FireAsync(Triggers.StartProcessing))
            .ThrowsException();
    }

    [Test]
    public async Task StateMachine_HookThrows_ExceptionPropagatesAndStateUnchanged()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine
            .Configure(States.Start)
            .OnExit(() => throw new InvalidOperationException("Hook failed!"))
            .Trigger(Triggers.StartProcessing)
            .TransitionTo(States.Processing);
        machine.Configure(States.Processing);
        await Assert.That(machine.State).IsEqualTo(States.Start);
        await Assert
            .That(async () => await machine.FireAsync(Triggers.StartProcessing))
            .ThrowsException()
            .WithMessage("Hook failed!");
        await Assert.That(machine.State).IsEqualTo(States.Start);
    }

    [Test]
    public async Task StateMachine_ReentrantTransition_ThrowsAndStateUnchanged()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine
            .Configure(States.Start)
            .OnExit(() => machine.Fire(Triggers.StartProcessing))
            .Trigger(Triggers.StartProcessing)
            .TransitionTo(States.Processing);
        machine.Configure(States.Processing);
        await Assert.That(machine.State).IsEqualTo(States.Start);
        await Assert
            .That(async () => await machine.FireAsync(Triggers.StartProcessing))
            .ThrowsException()
            .WithMessage(
                "Reentrant transitions are not allowed. Cannot fire a trigger during another transition."
            );
        await Assert.That(machine.State).IsEqualTo(States.Start);
    }

    [Test]
    public async Task StateMachine_When_Guard_False_Blocks_Transition()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine
            .Configure(States.Start)
            .Trigger(Triggers.StartProcessing)
            .When(() => false)
            .TransitionTo(States.Processing);
        machine.Configure(States.Processing);
        await Assert.That(machine.State).IsEqualTo(States.Start);
        await Assert
            .That(async () => await machine.FireAsync(Triggers.StartProcessing))
            .ThrowsException();
        await Assert.That(machine.State).IsEqualTo(States.Start);
    }

    [Test]
    public async Task StateMachine_WhenAsync_Guard_False_Blocks_Transition()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine
            .Configure(States.Start)
            .Trigger(Triggers.StartProcessing)
            .WhenAsync(async () =>
            {
                await Task.Delay(1);
                return false;
            })
            .TransitionTo(States.Processing);
        machine.Configure(States.Processing);
        await Assert.That(machine.State).IsEqualTo(States.Start);
        await Assert
            .That(async () => await machine.FireAsync(Triggers.StartProcessing))
            .ThrowsException();
        await Assert.That(machine.State).IsEqualTo(States.Start);
    }

    #endregion
}
