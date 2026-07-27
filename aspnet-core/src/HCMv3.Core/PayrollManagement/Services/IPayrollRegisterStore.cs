using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// The ONE place that knows which concrete register entity family a payroll run writes to. Every wage rule of
/// waves P4-P6 reads/writes register lines through here, so wave P7 (collapsing the legacy
/// <c>HCM10.PayrollEngine/SimulationPayrollHelper.cs</c> clone) means adding a
/// <see cref="PayrollRegisterTarget.Simulation"/> branch inside the single implementation instead of
/// duplicating the engine. See <see cref="PayrollRegisterTarget"/>.
///
/// <para>Replaces legacy's direct <c>salaryRegister.SalaryRegisterDetail.Where(r =&gt; r.WageType.Code ==
/// "xxxx").FirstOrDefault()</c> / <c>objectSpace.CreateObject&lt;SalaryRegisterDetail&gt;()</c> idiom, which
/// appears verbatim in ~34 legacy branches.</para>
/// </summary>
public interface IPayrollRegisterStore : IDomainService
{
    /// <summary>
    /// All lines currently on the register. Legacy equivalent: <c>salaryRegister.SalaryRegisterDetail</c>.
    /// </summary>
    Task<List<PayrollRegisterLine>> GetLinesAsync(PayrollRegisterRef register);

    /// <summary>
    /// The single line for <paramref name="wageCode"/>, or <c>null</c>. Legacy equivalent:
    /// <c>salaryRegister.SalaryRegisterDetail.Where(r =&gt; r.WageType.Code == wageCode).FirstOrDefault()</c>.
    /// </summary>
    Task<PayrollRegisterLine> GetLineAsync(PayrollRegisterRef register, string wageCode);

    /// <summary>
    /// The single line for <paramref name="wageTypeId"/>, or <c>null</c>. Legacy equivalent:
    /// <c>...Where(r =&gt; r.WageType.Oid == currWageType.Oid).FirstOrDefault()</c> -- the form the calculated-pay
    /// branches use, since they already hold the WageType object.
    /// </summary>
    Task<PayrollRegisterLine> GetLineByWageTypeIdAsync(PayrollRegisterRef register, int wageTypeId);

    /// <summary>
    /// Inserts or updates the line for <paramref name="line"/>'s wage type.
    ///
    /// <para><b>Legacy "zero means delete".</b> When <paramref name="deleteWhenAmountIsZero"/> is <c>true</c>
    /// (the default) and <see cref="PayrollRegisterLine.Amount"/> is 0, any existing row is DELETED and no row
    /// is created -- reproducing the <c>if (salaryRegisterDetail.Amount == 0) salaryRegisterDetail.Delete();
    /// else salaryRegister.SalaryRegisterDetail.Add(salaryRegisterDetail);</c> pattern used by the
    /// calculated-pay branches (legacy <c>EmployeeCalculatedPayService.cs</c> lines 211-216, 245-250, 421-426,
    /// 456-461, 492-497). Pass <c>false</c> for the branches that legacy wrote unconditionally -- notably
    /// 9000 Net Pay, which legacy always adds even when the amount is 0 (legacy line 1093), and the
    /// "time type" number-only lines, which legacy gates on <c>Number != 0</c> instead
    /// (<c>Program.CalculateTimeType</c>, legacy <c>Program.cs</c> line 2719).</para>
    /// </summary>
    /// <returns>The persisted line with its <see cref="PayrollRegisterLine.Id"/> populated, or <c>null</c> if it was deleted/not created.</returns>
    Task<PayrollRegisterLine> SaveLineAsync(PayrollRegisterRef register, PayrollRegisterLine line, bool deleteWhenAmountIsZero = true);

    /// <summary>Deletes the line for <paramref name="wageTypeId"/> if it exists.</summary>
    Task DeleteLineAsync(PayrollRegisterRef register, int wageTypeId);

    /// <summary>
    /// Deletes every line of the register AND every day-detail row hanging off those lines. Legacy equivalent:
    /// the "delete SalaryRegisterDaysDetail then delete SalaryRegister.SalaryRegisterDetail" block run when an
    /// existing register is re-calculated (legacy <c>Program.cs</c> lines 1069-1077 for regular,
    /// 1316-1323 for off-cycle).
    /// </summary>
    Task ClearLinesAsync(PayrollRegisterRef register);

    /// <summary>
    /// Replaces the day-wise breakdown of one register line. Legacy equivalent:
    /// <c>Program.AddSalaryRegisterDaysDetail</c> called in a loop from <c>Program.CalculateTimeType</c>
    /// (legacy <c>Program.cs</c> lines 2742-2745).
    ///
    /// <para>Takes <see cref="SalaryRegisterDayCount"/> -- the day-count model owned by the sibling
    /// <c>IPayrollDayCountService</c> wave (<c>PayrollDayCountModels.cs</c>) -- because that is exactly what
    /// produces these rows.</para>
    /// </summary>
    Task SaveDayDetailsAsync(PayrollRegisterRef register, int lineId, IEnumerable<SalaryRegisterDayCount> days);
}
