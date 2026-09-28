using OrderFlow.Client.Models.Inventory;
using OrderFlow.Client.Models.Orders;

namespace OrderFlow.Client.Pages
{
    public partial class PlaceOrder
    {
        private List<GetStockItemResponse> _stockItems = [];

        private readonly CreateOrderRequest _orderRequest = new()
        {
            CustomerId = "cust-demo",
            Lines =
            [
                new OrderLineRequest
                {
                    Quantity = 1,
                    UnitPrice = 10.00m
                }
            ]
        };

        private CreateOrderResponse? _createdOrder;
        private GetOrderResponse? _orderDetail;

        private bool _isLoadingStock;
        private bool _isSubmitting;
        private bool _isPolling;

        private string? _errorMessage;

        private CancellationTokenSource? _pollingCts;

        protected override async Task OnInitializedAsync()
        {
            await LoadStockAsync();
        }

        private async Task LoadStockAsync()
        {
            try
            {
                _isLoadingStock = true;
                _errorMessage = null;

                _stockItems = await InventoryApi.GetStockAsync();

                if (_stockItems.Count > 0)
                {
                    foreach (var line in _orderRequest.Lines)
                    {
                        line.Sku = _stockItems[0].Sku;
                    }
                }
            }
            catch (Exception ex)
            {
                _errorMessage =
                    $"Failed to load stock: {ex.Message}";
            }
            finally
            {
                _isLoadingStock = false;
            }
        }

        private void AddLine()
        {
            var defaultSku = _stockItems.FirstOrDefault()?.Sku
                ?? string.Empty;

            _orderRequest.Lines.Add(
                new OrderLineRequest
                {
                    Sku = defaultSku,
                    Quantity = 1,
                    UnitPrice = 10.00m
                });
        }

        private void RemoveLine(OrderLineRequest line)
        {
            if (_orderRequest.Lines.Count <= 1)
            {
                return;
            }

            _orderRequest.Lines.Remove(line);
        }

        private async Task PlaceOrderAsync()
        {
            _errorMessage = null;

            if (string.IsNullOrWhiteSpace(_orderRequest.CustomerId))
            {
                _errorMessage = "Customer ID is required.";
                return;
            }

            if (_orderRequest.Lines.Count == 0)
            {
                _errorMessage =
                    "At least one order line is required.";

                return;
            }

            if (_orderRequest.Lines.Any(x =>
                    string.IsNullOrWhiteSpace(x.Sku) ||
                    x.Quantity <= 0 ||
                    x.UnitPrice <= 0))
            {
                _errorMessage = "Please check SKU, quantity, and unit price.";

                return;
            }

            try
            {
                _isSubmitting = true;

                _createdOrder =
                    await OrdersApi.CreateOrderAsync(
                        _orderRequest);

                if (_createdOrder is null)
                {
                    _errorMessage =
                        "Order creation returned an empty response.";

                    return;
                }

                await PollOrderStatusAsync(
                    _createdOrder.OrderId);
            }
            catch (Exception ex)
            {
                _errorMessage =
                    $"Failed to place order: {ex.Message}";
            }
            finally
            {
                _isSubmitting = false;
            }
        }

        private async Task PollOrderStatusAsync(
            Guid orderId)
        {
            _pollingCts?.Cancel();
            _pollingCts?.Dispose();

            _pollingCts =
                new CancellationTokenSource();

            var cancellationToken =
                _pollingCts.Token;

            _isPolling = true;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    _orderDetail =
                        await OrdersApi.GetOrderAsync(
                            orderId,
                            cancellationToken);

                    if (_orderDetail is null)
                    {
                        _errorMessage = "Order not found.";
                        return;
                    }

                    _createdOrder = _createdOrder with
                    {
                            Status = _orderDetail.Status
                    };

                    await InvokeAsync(StateHasChanged);

                    if (IsTerminalStatus(_orderDetail.Status))
                    {
                        return;
                    }

                    await Task.Delay(
                        TimeSpan.FromSeconds(2),
                        cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _errorMessage =
                    $"Failed to poll order status: {ex.Message}";
            }
            finally
            {
                _isPolling = false;

                await InvokeAsync(StateHasChanged);
            }
        }

        private static bool IsTerminalStatus(
            string status)
        {
            return status.Equals(
                "Confirmed",
                StringComparison.OrdinalIgnoreCase)
                   ||
                   status.Equals(
                "Cancelled", 
                StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            _pollingCts?.Cancel();
            _pollingCts?.Dispose();
        }
    }
}
