using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="AXBatch"/>.
/// </summary>
public class AXBatchDto : EntityDto<int>
{
    public string JournalBatchNumber { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }
}
