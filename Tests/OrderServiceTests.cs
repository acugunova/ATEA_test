namespace Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using ATEA_test.API.Gateways;
using ATEA_test.API.Models;
using ATEA_test.API.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ATEA_test.API.DependencyInjection;
using ATEA_test.API.AutoMapperSetup;

[TestFixture]
public class OrderServiceTests
{
    private OrderService CreateService(params IPaymentGateway[] gateways)
    {
        var registry = new PaymentGatewayRegistry(gateways);
        return new OrderService(registry, NullLogger<OrderService>.Instance);
    }

    private static Order MakeRequest(string orderNumber = "ORD-001", string gatewayId = "test-gw", decimal amount = 100m)
        => new() { OrderNumber = orderNumber, UserId = "user-1", Amount = amount, GatewayId = gatewayId, Description = "Test order" };

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddAutoMapper([typeof(ProfileAssemblyInfo)]);

        var provider = services.BuildServiceProvider();
        DIFactory.Initialize(provider);
    }

    #region Success path

    [Test]
    public async Task ProcessOrder_SuccessfulGateway_ReturnsReceipt()
    {
        var mockGateway = new Mock<IPaymentGateway>();
        mockGateway.Setup(g => g.GatewayId).Returns("test-gw");
        mockGateway.Setup(g => g.ProcessPaymentAsync(It.IsAny<Order>(), default))
                   .ReturnsAsync(new Receipt() { OrderNumber = "ORD-001", PaymentConfirmation = "CONF-123", Timestamp = DateTime.UtcNow });

        var svc = CreateService(mockGateway.Object);
        var (receipt, error) = await svc.ProcessOrderAsync(MakeRequest());

        Assert.That(receipt, Is.Not.Null);
        Assert.That(receipt!.OrderNumber, Is.EqualTo("ORD-001"));
        Assert.That(receipt.PaymentConfirmation, Is.EqualTo("CONF-123"));
    }

    [Test]
    public async Task ProcessOrder_SuccessfulGateway_StoresOrderRecord()
    {
        var mockGateway = new Mock<IPaymentGateway>();
        mockGateway.Setup(g => g.GatewayId).Returns("test-gw");
        mockGateway.Setup(g => g.ProcessPaymentAsync(It.IsAny<Order>(), default))
                   .ReturnsAsync(new Receipt() { OrderNumber = "ORD-001", PaymentConfirmation = "CONF-X", Timestamp = DateTime.UtcNow });

        var svc = CreateService(mockGateway.Object);
        await svc.ProcessOrderAsync(MakeRequest());

        var allOrders = svc.GetAllOrders();
        Assert.That(allOrders.ContainsKey("ORD-001"), Is.True);
        Assert.That(allOrders["ORD-001"].PaymentConfirmation, Is.Not.Null);
    }

    #endregion

    #region Failure path

    [Test]
    public async Task ProcessOrder_FailingGateway_ReturnsError()
    {
        var mockGateway = new Mock<IPaymentGateway>();
        mockGateway.Setup(g => g.GatewayId).Returns("test-gw");
        mockGateway.Setup(g => g.ProcessPaymentAsync(It.IsAny<Order>(), default))
                   .Throws(new Exception("Transient network error"));

        var svc = CreateService(mockGateway.Object);
        var (receipt, error) = await svc.ProcessOrderAsync(MakeRequest());

        Assert.That(receipt, Is.Null);
        Assert.That(error, Does.Contain("Payment failed"));
    }

    [Test]
    public async Task ProcessOrder_FailingGateway_RetriesThreeTimes()
    {
        var mockGateway = new Mock<IPaymentGateway>();
        mockGateway.Setup(g => g.GatewayId).Returns("test-gw");
        mockGateway.Setup(g => g.ProcessPaymentAsync(It.IsAny<Order>(), default))
                   .Throws(new Exception("Transient error"));

        var svc = CreateService(mockGateway.Object);
        await svc.ProcessOrderAsync(MakeRequest());

        mockGateway.Verify(g => g.ProcessPaymentAsync(It.IsAny<Order>(), default), Times.Exactly(3));
    }

    [Test]
    public async Task ProcessOrder_SucceedsOnSecondAttempt_ReturnsReceipt()
    {
        var callCount = 0;
        var mockGateway = new Mock<IPaymentGateway>();
        mockGateway.Setup(g => g.GatewayId).Returns("test-gw");
        mockGateway.Setup(g => g.ProcessPaymentAsync(It.IsAny<Order>(), default))
                   .ReturnsAsync(() =>
                   {
                       callCount++;
                       if (callCount == 1) throw new Exception("Transient error");
                       else return new Receipt() { OrderNumber = "ORD-001", PaymentConfirmation = "CONF-RETRY", Timestamp = DateTime.UtcNow };
                   });

        var svc = CreateService(mockGateway.Object);
        var (receipt, error) = await svc.ProcessOrderAsync(MakeRequest());

        Assert.That(receipt, Is.Not.Null);
        Assert.That(receipt!.PaymentConfirmation, Is.EqualTo("CONF-RETRY"));
        mockGateway.Verify(g => g.ProcessPaymentAsync(It.IsAny<Order>(), default), Times.Exactly(2));
    }

    #endregion

    #region Idempotency 

    [Test]
    public async Task ProcessOrder_SameOrderTwice_CallsGatewayOnlyOnce()
    {
        var mockGateway = new Mock<IPaymentGateway>();
        mockGateway.Setup(g => g.GatewayId).Returns("test-gw");
        mockGateway.Setup(g => g.ProcessPaymentAsync(It.IsAny<Order>(), default))
                   .ReturnsAsync(new Receipt() { OrderNumber = "ORD-001", PaymentConfirmation = "CONF-IDEM", Timestamp = DateTime.UtcNow });

        var svc = CreateService(mockGateway.Object);
        var req = MakeRequest();

        var (receipt1, _) = await svc.ProcessOrderAsync(req);
        var (receipt2, _) = await svc.ProcessOrderAsync(req);

        Assert.That(receipt1, Is.Not.Null);
        Assert.That(receipt2, Is.Not.Null);
        Assert.That(receipt1!.PaymentConfirmation, Is.EqualTo(receipt2!.PaymentConfirmation));
        mockGateway.Verify(g => g.ProcessPaymentAsync(It.IsAny<Order>(), default), Times.Once);
    }

    [Test]
    public async Task ProcessOrder_SameOrderNumberDifferentData_ReturnsOriginalReceipt()
    {
        var gateway = new AlphaPaymentGateway();

        var svc = CreateService(gateway);

        await svc.ProcessOrderAsync(MakeRequest("ORD-DUP", "gateway-alpha", amount: 50m));
        var (receipt2, _) = await svc.ProcessOrderAsync(MakeRequest("ORD-DUP", amount: 999m));

        Assert.That(receipt2!.Amount, Is.EqualTo(50m));
    }

    #endregion

    #region Unknown gateway

    [Test]
    public async Task ProcessOrder_UnknownGateway_ReturnsError()
    {
        var svc = CreateService();
        var (receipt, error) = await svc.ProcessOrderAsync(MakeRequest(gatewayId: "nonexistent"));

        Assert.That(receipt, Is.Null);
        Assert.That(error, Does.Contain("nonexistent"));
    }

    #endregion

    #region Gateway-specific behaviours

    [Test]
    public async Task AlphaGateway_AlwaysSucceeds()
    {
        var gw = new AlphaPaymentGateway();
        for (int i = 0; i < 10; i++)
        {
            var result = await gw.ProcessPaymentAsync(MakeRequest(amount: 9999m));
            Assert.That(result.PaymentConfirmation, Is.Not.Null);
            Assert.That(result.PaymentConfirmation, Does.StartWith("ALPHA-"));
        }
    }

    [Test]
    public void BetaGateway_FailsForAmountOver1000()
    {
        var gw = new BetaPaymentGateway();
        var ex = Assert.ThrowsAsync<Exception>(async () => await gw.ProcessPaymentAsync(MakeRequest(amount: 1001m)));
        Assert.That(ex?.Message, Does.Contain("1,000"));
    }

    [Test]
    public async Task BetaGateway_SucceedsForAmountUnder1000()
    {
        var gw = new BetaPaymentGateway();
        var result = await gw.ProcessPaymentAsync(MakeRequest(amount: 999.99m));
        Assert.That(result.PaymentConfirmation, Is.Not.Null);
        Assert.That(result.PaymentConfirmation, Does.StartWith("BETA-"));
    }
    #endregion

    #region Registry

    [Test]
    public void Registry_ResolvesCorrectGateway()
    {
        var alpha = new AlphaPaymentGateway();
        var registry = new PaymentGatewayRegistry(new IPaymentGateway[] { alpha });
        var resolved = registry.Resolve("gateway-alpha");
        Assert.That(resolved, Is.SameAs(alpha));
    }

    [Test]
    public void Registry_ThrowsForUnknownId()
    {
        var registry = new PaymentGatewayRegistry(Array.Empty<IPaymentGateway>());
        Assert.Throws<KeyNotFoundException>(() => registry.Resolve("delta"));
    }

    #endregion
}