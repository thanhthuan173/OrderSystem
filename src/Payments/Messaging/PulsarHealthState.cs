using DotPulsar;

namespace Payments.Messaging
{
    public sealed class PulsarHealthState
    {
        private readonly Dictionary<string, ConsumerState> _consumerStates = [];
        private readonly object _lock = new();

        public void Update(ConsumerStateChanged stateChanged)
        {
            var key = $"{stateChanged.Consumer.Topic} | {stateChanged.Consumer.SubscriptionName}";

            lock (_lock)
            {
                _consumerStates[key] = stateChanged.ConsumerState;
            }
        }

        public bool IsHealthy
        {
            get
            {
                if (_consumerStates.Count == 0)
                {
                    return false;
                }

                return _consumerStates.Values.All(state => state == ConsumerState.Active);
            }
        }
    }
}
