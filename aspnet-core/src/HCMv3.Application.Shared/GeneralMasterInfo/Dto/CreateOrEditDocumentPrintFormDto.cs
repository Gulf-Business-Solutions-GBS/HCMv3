namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="DocumentPrintForm"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditDocumentPrintFormDto
{
    public int? Id { get; set; }

    public int? DocumentTypeId { get; set; }
}
