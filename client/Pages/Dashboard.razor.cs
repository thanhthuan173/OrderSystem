using OrderFlow.Client.Models.Inventory;
using OrderFlow.Client.Models.Orders;

namespace OrderFlow.Client.Pages
{
    public partial class Dashboard
    {
        private List<GetOrderSummaryResponse> _orders = [];
        private List<GetStockItemResponse> _stockItems = [];

        private bool _isLoading;
        private string? _errorMessage;

        protected override async Task OnInitializedAsync()
        {
            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            try
            {
                _isLoading = true;
                _errorMessage = null;

                var ordersTask = OrdersApi.GetOrdersAsync();
                var stockTask = InventoryApi.GetStockAsync();

                await Task.WhenAll(
                    ordersTask,
                    stockTask);

                _orders = ordersTask.Result
                    .Take(10)
                    .ToList();

                _stockItems = stockTask.Result
                    .OrderBy(x => x.Sku)
                    .ToList();
            }
            catch (Exception ex)
            {
                _errorMessage = $"Failed to load dashboard: {ex.Message}";
            }
            finally
            {
                _isLoading = false;
            }
        }
    }
}
