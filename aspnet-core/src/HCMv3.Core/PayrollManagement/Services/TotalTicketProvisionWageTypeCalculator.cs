using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Wave P5 -- wage code <b>3005, Total Ticket Provision</b> (closing). Ported from legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 363-393 (branch) and 2979-2998
/// (<c>GetTotalTicketProvision</c>). The <c>_Simu</c> clone (legacy line 6910) is NOT ported -- wave P7.
///
/// <para>Pure register arithmetic, no master data at all: closing ticket days are the sum of the "4028" and
/// "5028" lines' <c>Number</c>, and the per-ticket rate is reused from the "3002" line's <c>Rate</c> (which
/// <see cref="TicketProvisionWageTypeCalculator"/> has already posted, since 3002 precedes 3005 in
/// <c>PayrollOrder</c>). No rounding anywhere -- legacy has none here.</para>
///
/// <para><b>Preserved legacy detail:</b> each of the three source lookups is a <c>FirstOrDefault</c> over the
/// register, i.e. a missing line contributes 0 rather than being an error. In particular, when 3002 was
/// deleted by its own zero-means-delete the rate is 0, the amount is 0, and 3005 is then deleted too.</para>
/// </summary>
public class TotalTicketProvisionWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    /// <summary>Legacy line 2985: ticket accrual closing days.</summary>
    private const string WageCodeTicketAccrualClosing = "4028";

    /// <summary>Legacy line 2986: ditto, off-cycle series.</summary>
    private const string WageCodeTicketAccrualClosingAlt = "5028";

    /// <summary>Legacy line 2988: the monthly ticket provision line supplying the per-ticket rate.</summary>
    private const string WageCodeTicketProvision = "3002";

    private readonly IPayrollRegisterStore _registerStore;

    public TotalTicketProvisionWageTypeCalculator(IPayrollRegisterStore registerStore)
    {
        _registerStore = registerStore;
    }

    /// <inheritdoc/>
    public string WageCode => "3005";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy line 366.
        if (!ProvisionCalculationGates.AllowEndOfServiceProvision(context))
        {
            return;
        }

        // Legacy lines 368-376.
        var line = await ProvisionRegisterLineHelper.GetOrCreateLineAsync(_registerStore, context);

        var lines = await _registerStore.GetLinesAsync(context.Register);

        // Legacy lines 2983-2986.
        var count = NumberOf(lines, WageCodeTicketAccrualClosing) + NumberOf(lines, WageCodeTicketAccrualClosingAlt);

        // Legacy lines 2988-2991.
        var currTicketProvisionRate = lines
            .Where(l => l.WageCode == WageCodeTicketProvision)
            .Select(l => l.Rate)
            .FirstOrDefault();

        line.Rate = currTicketProvisionRate;
        line.Number = count;
        line.Amount = currTicketProvisionRate * count;

        // Legacy lines 385-390: zero means delete.
        await _registerStore.SaveLineAsync(context.Register, line);
    }

    private static double NumberOf(System.Collections.Generic.List<PayrollRegisterLine> lines, string wageCode)
    {
        return lines.Where(l => l.WageCode == wageCode).Select(l => l.Number).FirstOrDefault();
    }
}
