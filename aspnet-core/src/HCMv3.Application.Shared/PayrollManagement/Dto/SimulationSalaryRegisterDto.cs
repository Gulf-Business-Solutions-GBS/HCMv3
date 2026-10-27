using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SimulationSalaryRegister"/>.
/// </summary>
public class SimulationSalaryRegisterDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public DateTime ForPeriod { get; set; }

    public DateTime InPeriod { get; set; }

    public PayrollRunType PayrollRunType { get; set; }

    public PayrollResultType PayrollResultType { get; set; }

    public int PayrollReasonTypeId { get; set; }

    public string PayrollReasonTypeName { get; set; }
}
