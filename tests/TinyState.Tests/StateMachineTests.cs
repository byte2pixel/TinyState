namespace TinyState.Tests;

public enum Sates
{
    Start,
    Processing,
}
public enum Triggers
{
    StartProcessing,
}

public class StateMachineTests
{
    [Test]
    public async Task StateMachine_BasicTransition_Works()
    {
        var machine = new StateMachine<Sates, Triggers>(Sates.Start);
        machine.Configure(Sates.Start)
            .When(Triggers.StartProcessing)
            .GoTo(Sates.Processing);
        await Assert.That(machine.State).IsEqualTo(Sates.Start);
        machine.Fire(Triggers.StartProcessing);
        await Assert.That(machine.State).IsEqualTo(Sates.Processing);
    }

    [Test]
    public async Task StateMachine_Throws_WhenTriggerNotConfigured()
    {
        var machine = new StateMachine<Sates, Triggers>(Sates.Start);
        await Assert.That(machine.State).IsEqualTo(Sates.Start);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await Task.Run(() => machine.Fire(Triggers.StartProcessing)));
    }

    [Test]
    public async Task StateMachine_MultipleTransitions_Works()
    {
        var machine = new StateMachine<Sates, Triggers>(Sates.Start);
        machine.Configure(Sates.Start)
            .When(Triggers.StartProcessing)
            .GoTo(Sates.Processing);
        machine.Configure(Sates.Processing)
            .When(Triggers.StartProcessing)
            .GoTo(Sates.Start);
        await Assert.That(machine.State).IsEqualTo(Sates.Start);
        machine.Fire(Triggers.StartProcessing);
        await Assert.That(machine.State).IsEqualTo(Sates.Processing);
        machine.Fire(Triggers.StartProcessing);
        await Assert.That(machine.State).IsEqualTo(Sates.Start);
    }

    [Test]
    public async Task StateMachine_RepeatedTrigger_DoesNotChangeState()
    {
        var machine = new StateMachine<Sates, Triggers>(Sates.Start);
        machine.Configure(Sates.Start)
            .When(Triggers.StartProcessing)
            .GoTo(Sates.Processing);
        await Assert.That(machine.State).IsEqualTo(Sates.Start);
        machine.Fire(Triggers.StartProcessing);
        await Assert.That(machine.State).IsEqualTo(Sates.Processing);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await Task.Run(() => machine.Fire(Triggers.StartProcessing)));
    }
}
