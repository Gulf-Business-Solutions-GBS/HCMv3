using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TimeTransfer"/>.
/// </summary>
public class TimeTransferDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public DateTime TransferDate { get; set; }

    public int TransferTypeId { get; set; }

    public string TransferTypeName { get; set; }

    public double TransferCount { get; set; }

    public string Comments { get; set; }
}
