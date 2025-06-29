namespace TinyState.Tests;

public enum States
{
    Start,
    Processing,
    Completed
}

public enum Triggers
{
    StartProcessing,
    CompleteProcessing
}

public class StateMachineTests
{
    # region Happy Path (Success/Expected Usage)

    [Test]
    public async Task StateMachine_BasicTransition_Works()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine.Configure(States.Start).When(Triggers.StartProcessing).GoTo(States.Processing);
        await Assert.That(machine.State).IsEqualTo(States.Start);
        machine.Fire(Triggers.StartProcessing);
        await Assert.That(machine.State).IsEqualTo(States.Processing);
    }

    [Test]
    public async Task StateMachine_ReConfigureState_Works()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine.Configure(States.Start).When(Triggers.StartProcessing).GoTo(States.Processing);
        // Reconfigure the state
        machine.Configure(States.Start).When(Triggers.StartProcessing).GoTo(States.Completed);
        machine.Fire(Triggers.StartProcessing);
        await Assert.That(machine.State).IsEqualTo(States.Completed);
    }

    [Test]
    public async Task StateMachine_MultipleTransitions_Works()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine.Configure(States.Start).When(Triggers.StartProcessing).GoTo(States.Processing);
        machine.Configure(States.Processing).When(Triggers.StartProcessing).GoTo(States.Start);
        await Assert.That(machine.State).IsEqualTo(States.Start);
        machine.Fire(Triggers.StartProcessing);
        await Assert.That(machine.State).IsEqualTo(States.Processing);
        machine.Fire(Triggers.StartProcessing);
        await Assert.That(machine.State).IsEqualTo(States.Start);
    }

    #endregion

    #region Sad Path (Error/Exception Cases)

    [Test]
    public async Task StateMachine_Throws_WhenTriggerNotConfigured()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        await Assert.That(machine.State).IsEqualTo(States.Start);
        Assert.Throws<InvalidOperationException>(() => machine.Fire(Triggers.StartProcessing));
    }

    [Test]
    public void StateMachine_Throws_WhenNoTransitionForTrigger()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine.Configure(States.Start).When(Triggers.CompleteProcessing).GoTo(States.Completed);
        Assert.Throws<InvalidOperationException>(() => machine.Fire(Triggers.StartProcessing));
    }

    [Test]
    public async Task StateMachine_RepeatedTrigger_Throws()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        machine.Configure(States.Start).When(Triggers.StartProcessing).GoTo(States.Processing);
        await Assert.That(machine.State).IsEqualTo(States.Start);
        machine.Fire(Triggers.StartProcessing);
        await Assert.That(machine.State).IsEqualTo(States.Processing);
        Assert.Throws<InvalidOperationException>(() => machine.Fire(Triggers.StartProcessing));
    }

    [Test]
    public void StateMachine_Throws_WhenGoTo_CalledWithoutWhen()
    {
        var machine = new StateMachine<States, Triggers>(States.Start);
        Assert.Throws<InvalidOperationException>(() =>
            machine.Configure(States.Start).GoTo(States.Processing)
        );
    }

    #endregion
}
