using System.Collections.Generic;
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
/// Which document types employees may ask to withdraw from company custody.
///
/// <para>
/// One answer for every place that asks: the self-service request, which refuses any other type; the
/// dropdown that offers the documents in the first place; and the document list, which offers Withdraw on a
/// row. Were they to resolve it separately, one could offer a document another then refuses.
/// </para>
/// </summary>
public interface IWithdrawableDocumentTypeResolver : IDomainService
{
    /// <summary>
    /// The types employees may withdraw: every type ticked <see cref="DocumentType.IsWithdrawable"/>. A
    /// tenant with none ticked falls back to the legacy rule -- the one type
    /// <see cref="ResolveDocumentTypeIdAsync"/> names. Null when that names no type either, which leaves any
    /// document withdrawable rather than none.
    /// </summary>
    Task<IReadOnlyList<int>> ResolveDocumentTypeIdsAsync();

    /// <summary>
    /// The single type whose code matches the tenant config <c>PassportWithdrawalDocumentTypeCode</c>,
    /// defaulting to legacy's "2". Null when no type carries that code. For the one caller that needs THE
    /// passport rather than "a withdrawable document": the request raised automatically from a leave.
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
    public virtual async Task<IReadOnlyList<int>> ResolveDocumentTypeIdsAsync()
    {
        var ticked = await _documentTypeRepository.GetAll()
            .Where(t => t.IsWithdrawable)
            .Select(t => t.Id)
            .ToListAsync();

        if (ticked.Count > 0)
        {
            return ticked;
        }

        // Nothing ticked: a tenant set up before the switch existed. It behaves as it always did.
        var legacyTypeId = await ResolveDocumentTypeIdAsync();

        return legacyTypeId.HasValue ? new[] { legacyTypeId.Value } : null;
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
