using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="CostRegister"/>.
/// </summary>
public class CostRegisterDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public DateTime Period { get; set; }

    public PayrollRunType PayrollRunType { get; set; }
}
