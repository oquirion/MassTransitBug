using MassTransitBug.Contract;

namespace MassTransitBug.StateMachine;

using System;
using MassTransit;

public class FormRequestStateMachine : MassTransitStateMachine<FormRequest>
{

    
    public FormRequestStateMachine()
    {
        //Never change the order of the state.
        InstanceState(x => x.CurrentState, Active, Completed, Expired);
        
        Initially(
            When(InitializeSaga)
                .Then(x =>
                { 
                    x.Publish(new SendNotification() { CorrelationId = x.Message.CorrelationId, SomeProp = 1});
                    x.SchedulePublish(TimeSpan.FromDays(3.5), new SendNotification() { CorrelationId = x.Message.CorrelationId, SomeProp = 2} );
                    
                })
                .TransitionTo(Active));

        During(Active,

            When(SendNotification)
                .Then(x =>
                {
                    x.Saga.SomeProp = x.Message.SomeProp;
                }));
    }
    
    /* States */
    public State? Active { get; }
    public State? Completed { get; }
    public State? Expired { get; }
    
    /* Events */
    private Event<SendNotification>? SendNotification { get; }
    private Event<InitializeSaga>? InitializeSaga { get; }
}