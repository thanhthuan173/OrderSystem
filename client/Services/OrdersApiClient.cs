using System.Net.Http.Json;
using OrderFlow.Client.Models.Orders;

namespace OrderFlow.Client.Services
{
    public sealed class OrdersApiClient(
    HttpClient httpClient,
    IConfiguration configuration)
    {
        private readonly string _baseUrl =
            configuration["ApiBaseUrls:Orders"]
            ?? throw new InvalidOperationException(
                "Orders API base URL is not configured.");

        public async Task<CreateOrderResponse?> CreateOrderAsync(
            CreateOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            using var response = await httpClient.PostAsJsonAsync(
                $"{_baseUrl}",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<CreateOrderResponse>(
                cancellationToken);
        }

        public async Task<GetOrderResponse?> GetOrderAsync(
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return await httpClient.GetFromJsonAsync<GetOrderResponse>(
                $"{_baseUrl}/{orderId}",
                cancellationToken);
        }

        public async Task<List<GetOrderSummaryResponse>> GetOrdersAsync(
            string? customerId = null,
            CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/all";

            if (!string.IsNullOrWhiteSpace(customerId))
            {
                url += $"?customerId={Uri.EscapeDataString(customerId)}";
            }

            return await httpClient.GetFromJsonAsync<List<GetOrderSummaryResponse>>(
                       url,
                       cancellationToken)
                   ?? [];
        }
    }
}
