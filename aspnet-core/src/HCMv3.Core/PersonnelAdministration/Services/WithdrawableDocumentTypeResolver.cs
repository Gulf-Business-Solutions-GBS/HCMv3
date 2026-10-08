using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;
using HCMv3.GeneralMasterInfo;
using HCMv3.SystemSettings.Services;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Which document type employees may ask to withdraw from company custody.
///
/// <para>
/// One answer for the two places that ask: the self-service request, which refuses any other type, and the
/// dropdown that offers the documents in the first place. Were they to resolve it separately, the dropdown
/// could offer a document the request then refuses.
/// </para>
/// </summary>
public interface IWithdrawableDocumentTypeResolver : IDomainService
{
    /// <summary>
    /// The type whose code matches the tenant config <c>PassportWithdrawalDocumentTypeCode</c>, defaulting to
    /// legacy's "2". Null when no type carries that code, which leaves any document withdrawable rather than
    /// none.
    /// </summary>
    Task<int?> ResolveDocumentTypeIdAsync();
}

/// <inheritdoc cref="IWithdrawableDocumentTypeResolver"/>
public class WithdrawableDocumentTypeResolver : HCMv3DomainServiceBase, IWithdrawableDocumentTypeResolver
{
    /// <summary>Tenant config key for the document type employees may withdraw (legacy's JSON setting).</summary>
    public const string DocumentTypeCodeKey = "PassportWithdrawalDocumentTypeCode";

    /// <summary>Legacy's default: document type code "2", the passport.</summary>
    public const string DefaultDocumentTypeCode = "2";

    private readonly IRepository<DocumentType> _documentTypeRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public WithdrawableDocumentTypeResolver(
        IRepository<DocumentType> documentTypeRepository,
        ISysSettingsAccessor sysSettingsAccessor)
    {
        _documentTypeRepository = documentTypeRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    [UnitOfWork]
    public virtual async Task<int?> ResolveDocumentTypeIdAsync()
    {
        var code = await _sysSettingsAccessor.GetSysTenantConfigAsync(DocumentTypeCodeKey);
        if (string.IsNullOrWhiteSpace(code))
        {
            code = DefaultDocumentTypeCode;
        }

        return await _documentTypeRepository.GetAll()
            .Where(t => t.Code == code)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();
    }
}
