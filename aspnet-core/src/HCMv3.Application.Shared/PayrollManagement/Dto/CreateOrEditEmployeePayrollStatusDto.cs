using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeePayrollStatus"/>. <c>Id == null</c> means create;
/// otherwise update. The one-row-per-Employee rule is enforced only on create.
/// </summary>
public class CreateOrEditEmployeePayrollStatusDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public DateTime EarliestRetroAccountingDate { get; set; }

    public DateTime AccountedTo { get; set; }

    public DateTime RunPayrollUpto { get; set; }

    public DateTime EarliestMasterDataChange { get; set; }

    public DateTime DoNoAccountAfter { get; set; }

    public bool PersonLocked { get; set; }

    public bool PayrollCorrection { get; set; }

    public DateTime EarliestRecalculationDate { get; set; }

    public DateTime PersonalCalendarFrom { get; set; }

    public DateTime NextTimeEvaluationFrom { get; set; }
}
