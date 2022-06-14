using MassTransit;
using MassTransitBug.Contract;
using MassTransitBug.StateMachine;
using NUnit.Framework;

namespace MassTransitBug.Test;

public class TestScheduledEvent : StateMachineTestFixture<FormRequestStateMachine, FormRequest>
{
    [Test]
    public async Task When_schedule_is_reached_Then_reminder_is_sent()
    {
        var reminderIn = TimeSpan.FromDays(3.5);
        var guid = Guid.NewGuid();
        
        await TestHarness.Bus.Publish(new InitializeSaga(){ CorrelationId = guid});

        var existsId = await SagaHarness.Exists(guid, x => x.Active);
        Assert.IsTrue(existsId.HasValue, "Saga did not exist");
        await SagaHarness.Consumed.Any<SendNotification>();
        
        var sagaInstance = await SagaHarness.Sagas.SelectAsync(x => x.CorrelationId == guid).FirstOrDefault();
        Assert.IsTrue(sagaInstance.Saga.SomeProp == 1);

        await AdvanceSystemTime(reminderIn);
        
        var found = await SagaHarness.Consumed
            .SelectAsync<SendNotification>(received => received.Context.Message.SomeProp == 2)
            .Any();
        
        Assert.IsTrue(found);
    }
}