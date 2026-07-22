using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="DocumentPrintForm"/>.
/// </summary>
public class DocumentPrintFormDto : EntityDto<int>
{
    public int? DocumentTypeId { get; set; }

    public string DocumentTypeName { get; set; }
}
