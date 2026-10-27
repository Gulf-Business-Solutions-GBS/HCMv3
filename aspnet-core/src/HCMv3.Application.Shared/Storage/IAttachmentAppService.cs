using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.Storage.Dto;

namespace HCMv3.Storage;

/// <summary>
/// Files attached to any record. One service for every screen: the screen names its record by entity type
/// and id, and who may see or change the files is decided by that entity type's owner policy.
///
/// <para>
/// An entity type with no registered policy is refused outright. Attachments are opt-in per type, so a
/// caller cannot reach the files of a record type nobody has decided the rules for.
/// </para>
/// </summary>
public interface IAttachmentAppService : IApplicationService
{
    Task<AttachmentSettingsDto> GetAttachmentSettings();

    Task<GetAttachmentsOutput> GetAttachments(AttachmentOwnerInput input);

    Task<AttachmentDto> AddAttachment(AddAttachmentInput input);

    Task DeleteAttachment(EntityDto input);

    /// <summary>Puts the file in the temp cache and returns the token <c>File/DownloadTempFile</c> serves.</summary>
    Task<FileDto> GetAttachmentDownload(EntityDto input);
}
