namespace ATEA_test.API.Gateways;

/// <summary>
/// Resolves the correct IPaymentGateway for a given gatewayId.
/// New gateways only need to implement IPaymentGateway and be registered in DI.
/// </summary>
public interface IPaymentGatewayRegistry
{
    IPaymentGateway Resolve(string gatewayId);
    IEnumerable<string> AvailableGatewayIds { get; }
}

public class PaymentGatewayRegistry : IPaymentGatewayRegistry
{
    private readonly Dictionary<string, IPaymentGateway> _gateways;

    public PaymentGatewayRegistry(IEnumerable<IPaymentGateway> gateways)
    {
        _gateways = gateways.ToDictionary(g => g.GatewayId, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<string> AvailableGatewayIds => _gateways.Keys;

    public IPaymentGateway Resolve(string gatewayId)
    {
        if (_gateways.TryGetValue(gatewayId, out var gateway))
            return gateway;

        throw new KeyNotFoundException($"No payment gateway registered for ID '{gatewayId}'. Available: {string.Join(", ", _gateways.Keys)}");
    }
}
