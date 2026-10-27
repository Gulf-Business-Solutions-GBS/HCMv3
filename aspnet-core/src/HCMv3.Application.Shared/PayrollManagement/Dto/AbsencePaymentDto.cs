using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="AbsencePayment"/>.
/// </summary>
public class AbsencePaymentDto : EntityDto<int>
{
    public int AbsenceTypeId { get; set; }

    public string AbsenceTypeName { get; set; }

    public int WageTypeId { get; set; }

    public string WageTypeName { get; set; }
}
