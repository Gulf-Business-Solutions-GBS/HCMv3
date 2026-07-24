using System;
using Abp.Application.Services.Dto;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SponsorBank"/>.
/// </summary>
public class SponsorBankDto : EntityDto<int>
{
    public int SponsorId { get; set; }

    public string SponsorName { get; set; }

    public int? EmployeeBankId { get; set; }

    public string EmployeeBankName { get; set; }

    public int? PaymentBankId { get; set; }

    public string PaymentBankName { get; set; }

    public string AccountNumber { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string IBAN { get; set; }
}
