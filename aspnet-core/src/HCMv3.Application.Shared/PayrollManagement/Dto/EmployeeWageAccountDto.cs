using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeWageAccount"/>.
/// </summary>
public class EmployeeWageAccountDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; }

    public int WageTypeId { get; set; }

    public string WageTypeName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int AccountInfoId { get; set; }

    public string AccountInfoName { get; set; }

    public int? OffsetAccountInfoId { get; set; }

    public string OffsetAccountInfoName { get; set; }

    public int? CostElement1Id { get; set; }

    public int? CostElement2Id { get; set; }

    public int? CostElement3Id { get; set; }

    public int? CostElement4Id { get; set; }

    public int? CostElement5Id { get; set; }

    public int? CostElement6Id { get; set; }

    public int? CostElement7Id { get; set; }

    public double CostPercentage { get; set; }

    public bool MergeOffsetAccount { get; set; }
}
