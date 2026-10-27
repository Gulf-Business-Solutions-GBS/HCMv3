using HCMv3.EntityFrameworkCore;

namespace HCMv3.Test.Base.TestData;

public class TestDataBuilder
{
    private readonly HCMv3DbContext _context;
    private readonly int _tenantId;

    public TestDataBuilder(HCMv3DbContext context, int tenantId)
    {
        _context = context;
        _tenantId = tenantId;
    }

    public void Create()
    {
        new TestOrganizationUnitsBuilder(_context, _tenantId).Create();
        new TestSubscriptionPaymentBuilder(_context, _tenantId).Create();
        new TestEditionsBuilder(_context).Create();

        _context.SaveChanges();
    }
}
