using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>One stretch of a sick leave paid at the same rate.</summary>
public class SickLeaveDeductionSegment
{
    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    /// <summary>What is deducted for those days -- a percentage of pay, or a number of days.</summary>
    public double DeductionNumber { get; set; }

    public DeductionUnit DeductionUnit { get; set; }

    /// <summary>True for the tail past the last slab, which legacy reports as a flat 100% deduction.</summary>
    public bool IsBeyondTheLastSlab { get; set; }
}

/// <summary>
/// What a sick leave will actually be paid, slab by slab -- legacy
/// <c>EmployeeAbsence.GetSickLeaveUnpaidInfo</c>, which showed the employee a breakdown such as
/// "01/03 - 10/03 (25%), 11/03 - 20/03 (50%)" before they committed to the request.
///
/// <para>
/// The slabs are cumulative ceilings on the sick days already taken this accrual year, so where a request falls
/// depends on what the employee has already used -- which is exactly why it is worth showing rather than leaving
/// them to find out on the payslip. The payroll engine applies the same rules independently
/// (<c>EmployeeLeavePayCalculationService.GetActualSickLeaveDaysAsync</c>); this only explains them.
/// </para>
/// </summary>
public interface ISickLeaveDeductionPreview : IDomainService
{
    /// <summary>
    /// The breakdown, or an empty list when it does not apply: a type that is not sick leave, an employee with
    /// no sub-group, or no deduction rule configured.
    /// </summary>
    Task<List<SickLeaveDeductionSegment>> GetSegmentsAsync(int employeeId, int absenceTypeId, DateTime startDate, DateTime endDate);
}
