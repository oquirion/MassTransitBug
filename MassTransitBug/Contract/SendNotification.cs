namespace MassTransitBug.Contract;

public record SendNotification()
{
    public Guid CorrelationId { get; init; }
    public int SomeProp { get; init; }
}