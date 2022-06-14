namespace MassTransitBug.Contract;

public record InitializeSaga()
{
    public Guid CorrelationId { get; init; }
}