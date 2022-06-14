using MassTransit;

namespace MassTransitBug.StateMachine;

public class FormRequest  :   SagaStateMachineInstance, ISagaVersion
{
    public Guid CorrelationId { get; set; }
    public int Version { get; set; }
    public int CurrentState { get; set; }
    public int SomeProp { get; set; }
}