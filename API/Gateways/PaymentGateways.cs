using ATEA_test.API.Models;
using static ATEA_test.API.AutoMapperSetup.AutoMapperConfig;

namespace ATEA_test.API.Gateways;

/// <summary>
/// Contract for all payment gateway implementations.
/// </summary>
public interface IPaymentGateway
{
    string GatewayId { get; }
    Task<Receipt> ProcessPaymentAsync(Order order, CancellationToken ct = default);
}

/// <summary>
/// Mock gateway that always succeeds.
/// GatewayId: "gateway-alpha"
/// </summary>
public class AlphaPaymentGateway : IPaymentGateway
{
    public string GatewayId => "gateway-alpha";

    public Task<Receipt> ProcessPaymentAsync(Order order, CancellationToken ct = default)
    {
        var confirmation = $"ALPHA-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        var receipt = MapperWrapper.Mapper.Map<Receipt>(order);
        receipt.PaymentConfirmation = confirmation;
        return Task.FromResult(receipt);
    }
}

/// <summary>
/// Mock gateway that fails for amounts over 1000.
/// GatewayId: "gateway-beta"
/// </summary>
public class BetaPaymentGateway : IPaymentGateway
{
    public string GatewayId => "gateway-beta";

    public Task<Receipt> ProcessPaymentAsync(Order order, CancellationToken ct = default)
    {
        if (order.Amount > 1000)
            throw new Exception("Beta Gateway: Amount exceeds single-transaction limit of $1,000.00");

        var confirmation = $"BETA-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var receipt = MapperWrapper.Mapper.Map<Receipt>(order);
        receipt.PaymentConfirmation = confirmation;
        return Task.FromResult(receipt);
    }
}

/// <summary>
/// Mock gateway that randomly fails ~30% of the time (simulates network issues).
/// GatewayId: "gateway-gamma"
/// </summary>
public class GammaPaymentGateway : IPaymentGateway
{
    private static readonly Random _rng = new();
    public string GatewayId => "gateway-gamma";

    public Task<Receipt> ProcessPaymentAsync(Order order, CancellationToken ct = default)
    {
        if (_rng.NextDouble() < 0.30)
            throw new Exception("Gamma Gateway: Transient network error. Please retry.");

        var confirmation = $"GAMMA-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var receipt = MapperWrapper.Mapper.Map<Receipt>(order);
        receipt.PaymentConfirmation = confirmation;
        return Task.FromResult(receipt);
    }
}
