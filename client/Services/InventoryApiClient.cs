using System.Net.Http.Json;
using OrderFlow.Client.Models.Inventory;

namespace OrderFlow.Client.Services
{
    public sealed class InventoryApiClient(
    HttpClient httpClient,
    IConfiguration configuration)
    {
        private readonly string _baseUrl =
            configuration["ApiBaseUrls:Inventory"]
            ?? throw new InvalidOperationException(
                "Inventory API base URL is not configured.");

        public async Task<List<GetStockItemResponse>> GetStockAsync(
            CancellationToken cancellationToken = default)
        {
            return await httpClient.GetFromJsonAsync<List<GetStockItemResponse>>(
                       $"{_baseUrl}/stock",
                       cancellationToken)
                   ?? [];
        }
    }
}
