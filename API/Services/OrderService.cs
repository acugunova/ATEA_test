using AutoMapper;
using ATEA_test.API.Gateways;
using ATEA_test.API.Models;
using static ATEA_test.API.AutoMapperSetup.AutoMapperConfig;

namespace ATEA_test.API.Services;
public interface IOrderService
{
    Task<(Receipt? Receipt, string? Error)> ProcessOrderAsync(Order request, CancellationToken ct = default);
    IReadOnlyDictionary<string, Receipt> GetAllOrders();
}

public class OrderService : IOrderService
{
    private readonly IPaymentGatewayRegistry _registry;
    private readonly ILogger<OrderService> _logger;

    private readonly Dictionary<string, Receipt> _orders = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _lock = new(1, 1);

    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(200);

    public OrderService(IPaymentGatewayRegistry registry, ILogger<OrderService> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public IReadOnlyDictionary<string, Receipt> GetAllOrders() => _orders;

    public async Task<(Receipt? Receipt, string? Error)> ProcessOrderAsync(
        Order request, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (_orders.TryGetValue(request.OrderNumber, out var existing))
            {
                _logger.LogInformation("Idempotent hit for order {OrderNumber} – returning cached result", request.OrderNumber);
                if (existing is not null && existing.PaymentConfirmation != null)
                    return (MapperWrapper.Mapper.Map<Receipt>(existing), null);
                return (null, $"Order '{request.OrderNumber}' was previously attempted and failed.");
            }

            _logger.LogInformation($"Processing order {request.OrderNumber} via gateway {request.GatewayId}", request.OrderNumber, request.GatewayId);

            IPaymentGateway gateway;
            try
            {
                gateway = _registry.Resolve(request.GatewayId);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Unknown gateway requested for order {request.OrderNumber}", request.OrderNumber);
                return (null, ex.Message);
            }

            Receipt? receipt = null;
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    receipt = await gateway.ProcessPaymentAsync(request, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Attempt {attempt}/{MaxRetries} failed for order {request.OrderNumber}: {ex.Message}",
                    attempt, MaxRetries, request.OrderNumber, ex);
                }


                if (attempt < MaxRetries)
                    await Task.Delay(RetryDelay * attempt, ct);
                if (receipt != null)
                    break;
            }

            if (receipt != null)
            {
                _orders[request.OrderNumber] = receipt;

                _logger.LogInformation($"Order {request.OrderNumber} succeeded. Confirmation: {receipt.PaymentConfirmation}",
                    request.OrderNumber, receipt.PaymentConfirmation);

                return (receipt, null);
            }

            _orders[request.OrderNumber] = MapperWrapper.Mapper.Map<Receipt>(existing);

            _logger.LogError($"Order {request.OrderNumber} failed after {MaxRetries} attempts.",
                request.OrderNumber, MaxRetries);

            return (null,  $"Payment failed. Order {request.OrderNumber} failed after {MaxRetries} attempts.");
        }
        finally
        {
            _lock.Release();
        }
    }
}
