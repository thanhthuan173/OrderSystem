namespace Messaging
{
    public interface IPulsarEventPublisher
    {
        Task PublishAsync(
            string topic,
            Guid orderId,
            string payload,
            CancellationToken cancellationToken);
    }
}
