using System.Linq;
using HCMv3.Editions;
using HCMv3.EntityFrameworkCore;
using HCMv3.ExtraProperties;
using HCMv3.MultiTenancy.Payments;

namespace HCMv3.Test.Base.TestData;

public class TestSubscriptionPaymentBuilder
{
    private readonly HCMv3DbContext _context;
    private readonly int _tenantId;

    public TestSubscriptionPaymentBuilder(HCMv3DbContext context, int tenantId)
    {
        _context = context;
        _tenantId = tenantId;
    }

    public void Create()
    {
        CreatePayments();
    }

    private void CreatePayments()
    {
        var defaultEdition = _context.Editions.First(e => e.Name == EditionManager.DefaultEditionName);

        CreatePayment(1, defaultEdition.Id, _tenantId, 1, "147741");
        CreatePayment(19, defaultEdition.Id, _tenantId, 30, "1477419");
    }

    private void CreatePayment(decimal amount, int editionId, int tenantId, int dayCount, string paymentId)
    {
        var payment = new SubscriptionPayment
        {
            TenantId = tenantId,
            DayCount = dayCount,
            ExternalPaymentId = paymentId
        };

        _context.SubscriptionPayments.Add(payment);
        _context.SaveChanges();

        var product = new SubscriptionPaymentProduct(
            payment.Id,
            "Test product",
            amount,
            1,
            amount,
            new ExtraPropertyDictionary
            {
                ["EditionId"] = editionId
            });

        _context.SubscriptionPaymentProducts.Add(product);
        _context.SaveChanges();
    }
}