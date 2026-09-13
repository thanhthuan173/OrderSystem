using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;

namespace Messaging
{
    public class PulsarEventPublisher : IPulsarEventPublisher
    {
        private readonly IPulsarClient _client;

        public PulsarEventPublisher(IPulsarClient client)
        {
            _client = client;
        }

        public async Task PublishAsync(
            string topic,
            Guid orderId,
            string payload,
            CancellationToken cancellationToken)
        {
            await using var producer = _client
                .NewProducer(Schema.String)
                .Topic(topic)
                .Create();

            var metadata = new MessageMetadata
            {
                Key = orderId.ToString()
            };

            await producer.Send(
                metadata,
                payload,
                cancellationToken);
        }
    }
}
