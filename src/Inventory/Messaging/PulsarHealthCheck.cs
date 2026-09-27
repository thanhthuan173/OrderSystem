using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Inventory.Messaging
{
    public sealed class PulsarHealthCheck : IHealthCheck
    {
        private readonly PulsarHealthState _state;

        public PulsarHealthCheck(PulsarHealthState state)
        {
            _state = state;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (_state.IsHealthy)
            {
                return Task.FromResult(HealthCheckResult.Healthy());
            }

            return Task.FromResult(HealthCheckResult.Unhealthy());
        }
    }
}
