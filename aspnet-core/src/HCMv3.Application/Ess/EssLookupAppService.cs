using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.TimeManagement;
using HCMv3.TravelManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IEssLookupAppService"/>
/// <remarks>
/// <para>
/// One service rather than a lookup method bolted onto each <c>My*AppService</c>, because the same list is
/// needed by several screens (document types by both the document and letter forms; currencies and expense
/// types by travel and, in time, travel claims) and because fifteen scattered methods is fifteen places for
/// the next person to forget the permission.
/// </para>
///
/// <para>
/// <b>Row filtering.</b> Every entity behind these lists is a <c>FullAuditedEntity</c> implementing
/// <c>ISoftDelete</c> and <c>IMustHaveTenant</c>, so ABP's global filters already drop deleted rows and
/// scope to the caller's tenant. Nothing here disables those filters -- unlike the <c>My*</c> services,
/// which must disable <c>ApprovedOnly</c> to show an employee their own pending rows. There is no
/// equivalent need for reference data.
/// </para>
///
/// <para>
/// Ordering is by English name, matching <c>GetTravelTypes</c> and <c>GetClearanceTypes</c>. The admin
/// lists sort by <c>Code</c>, which is right for a maintenance grid and wrong for a dropdown somebody is
/// scanning for a word.
/// </para>
/// </remarks>
[AbpAuthorize(AppPermissions.Ess)]
public class EssLookupAppService : HCMv3AppServiceBase, IEssLookupAppService
{
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<Asset> _assetRepository;
    private readonly IRepository<AttendanceType> _attendanceTypeRepository;
    private readonly IRepository<Bank> _bankRepository;
    private readonly IRepository<Currency> _currencyRepository;
    private readonly IRepository<DocumentType> _documentTypeRepository;
    private readonly IRepository<ExpenseType> _expenseTypeRepository;
    private readonly IRepository<HRServiceType> _hrServiceTypeRepository;
    private readonly IRepository<Hotel> _hotelRepository;
    private readonly IRepository<LoanType> _loanTypeRepository;
    private readonly IRepository<OvertimeType> _overtimeTypeRepository;
    private readonly IRepository<TicketType> _ticketTypeRepository;
    private readonly IRepository<TransType> _transTypeRepository;
    private readonly IRepository<TravelAirline> _travelAirlineRepository;
    private readonly IRepository<TravelDestination> _travelDestinationRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IEmployeeSessionService _employeeSessionService;
    private readonly HCMv3.SystemSettings.Services.ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IRepository<HCMv3.OrganizationManagement.OrganizationUnitType> _organizationUnitTypeRepository;
    private readonly HCMv3.OrganizationManagement.Services.IOrgHierarchyService _orgHierarchyService;
    private readonly IRepository<EmployeeDocument> _employeeDocumentRepository;
    private readonly IWithdrawableDocumentTypeResolver _withdrawableDocumentTypeResolver;

    public EssLookupAppService(
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<Asset> assetRepository,
        IRepository<AttendanceType> attendanceTypeRepository,
        IRepository<Bank> bankRepository,
        IRepository<Currency> currencyRepository,
        IRepository<DocumentType> documentTypeRepository,
        IRepository<ExpenseType> expenseTypeRepository,
        IRepository<HRServiceType> hrServiceTypeRepository,
        IRepository<Hotel> hotelRepository,
        IRepository<LoanType> loanTypeRepository,
        IRepository<OvertimeType> overtimeTypeRepository,
        IRepository<TicketType> ticketTypeRepository,
        IRepository<TransType> transTypeRepository,
        IRepository<TravelAirline> travelAirlineRepository,
        IRepository<TravelDestination> travelDestinationRepository,
        IRepository<Employee> employeeRepository,
        IEmployeeSessionService employeeSessionService,
        HCMv3.SystemSettings.Services.ISysSettingsAccessor sysSettingsAccessor,
        IRepository<HCMv3.OrganizationManagement.OrganizationUnitType> organizationUnitTypeRepository,
        HCMv3.OrganizationManagement.Services.IOrgHierarchyService orgHierarchyService,
        IRepository<EmployeeDocument> employeeDocumentRepository,
        IWithdrawableDocumentTypeResolver withdrawableDocumentTypeResolver)
    {
        _employeeDocumentRepository = employeeDocumentRepository;
        _withdrawableDocumentTypeResolver = withdrawableDocumentTypeResolver;
        _sysSettingsAccessor = sysSettingsAccessor;
        _organizationUnitTypeRepository = organizationUnitTypeRepository;
        _orgHierarchyService = orgHierarchyService;
        _absenceTypeRepository = absenceTypeRepository;
        _assetRepository = assetRepository;
        _attendanceTypeRepository = attendanceTypeRepository;
        _bankRepository = bankRepository;
        _currencyRepository = currencyRepository;
        _documentTypeRepository = documentTypeRepository;
        _expenseTypeRepository = expenseTypeRepository;
        _hrServiceTypeRepository = hrServiceTypeRepository;
        _hotelRepository = hotelRepository;
        _loanTypeRepository = loanTypeRepository;
        _overtimeTypeRepository = overtimeTypeRepository;
        _ticketTypeRepository = ticketTypeRepository;
        _transTypeRepository = transTypeRepository;
        _travelAirlineRepository = travelAirlineRepository;
        _travelDestinationRepository = travelDestinationRepository;
        _employeeRepository = employeeRepository;
        _employeeSessionService = employeeSessionService;
    }

    // ------------------------------------------------------------------ the lists

    /// <remarks>
    /// <c>AbsenceType.IsActive</c> is deliberately NOT filtered on. It defaults to <c>false</c>, has no
    /// database default, is set only from the admin form's checkbox, and is queried nowhere else in the
    /// solution -- including by the admin list this replaces. Filtering on it would silently empty the leave
    /// dropdown for any tenant whose types predate the flag, which is the same bug this service exists to
    /// fix. Wiring the flag up properly is a separate job; see the report accompanying this change.
    /// </remarks>
    public Task<List<SimpleLookupDto>> GetAbsenceTypes() =>
        ListAsync(_absenceTypeRepository, e => new SimpleLookupDto
        {
            Id = e.Id,
            Code = e.Code,
            NameEn = e.NameEn,
            NameAr = e.NameAr
        });

    /// <remarks>
    /// Retired assets are excluded. Safe to filter, unlike the absence-type flag above, because
    /// <c>AssetStatus.Active</c> is <c>0</c> -- the CLR and database default -- so an untouched row reads as
    /// active rather than disappearing. Allocation status is left alone on purpose: an asset already issued
    /// to somebody else is still a thing an employee may reasonably ask for.
    /// <para>
    /// Only assets whose type has <c>IncludeInWorkflow</c> are offered, as legacy ESS did
    /// (<c>GeneralController</c>: <c>r.AssetType.IncludeInWorkflow</c>); an asset with no type is not offered.
    /// </para>
    /// </remarks>
    public Task<List<SimpleLookupDto>> GetAssets() =>
        ListAsync(_assetRepository, e => new SimpleLookupDto
        {
            Id = e.Id,
            Code = e.Code,
            NameEn = e.NameEn,
            NameAr = e.NameAr
        },
        e => e.Status == AssetStatus.Active && e.AssetType != null && e.AssetType.IncludeInWorkflow);

    public Task<List<SimpleLookupDto>> GetAttendanceTypes() =>
        ListAsync(_attendanceTypeRepository, e => new SimpleLookupDto
        {
            Id = e.Id,
            Code = e.Code,
            NameEn = e.NameEn,
            NameAr = e.NameAr
        });

    public Task<List<SimpleLookupDto>> GetBanks() =>
        ListAsync(_bankRepository, e => new SimpleLookupDto
        {
            Id = e.Id,
            Code = e.Code,
            NameEn = e.NameEn,
            NameAr = e.NameAr
        });

    public Task<List<SimpleLookupDto>> GetCurrencies() =>
        ListAsync(_currencyRepository, e => new SimpleLookupDto
        {
            Id = e.Id,
            Code = e.Code,
            NameEn = e.NameEn,
            NameAr = e.NameAr
        });

    /// <remarks>
    /// The types an employee may record a document of: offered in self-service, not hidden from employees,
    /// and not a letter -- a letter is asked for, never recorded (legacy <c>AllowUpdate</c>). The rule is
    /// <see cref="DocumentTypeEssRules.CanBeRecordedByEmployee"/>, which the request itself enforces.
    /// </remarks>
    public Task<List<SimpleLookupDto>> GetDocumentTypes() =>
        ListAsync(_documentTypeRepository, e => new SimpleLookupDto
        {
            Id = e.Id,
            Code = e.Code,
            NameEn = e.NameEn,
            NameAr = e.NameAr
        },
        DocumentTypeEssRules.CanBeRecordedByEmployee);

    /// <remarks>
    /// A passport or a residency permit is a document HR holds, not something that can be "requested as a
    /// letter", so it must never reach the letter form's dropdown. The rule is
    /// <see cref="DocumentTypeEssRules.CanBeRequestedAsLetter"/>, which the request itself enforces.
    /// </remarks>
    public Task<List<SimpleLookupDto>> GetLetterTypes() =>
        ListAsync(_documentTypeRepository, e => new SimpleLookupDto
        {
            Id = e.Id,
            Code = e.Code,
            NameEn = e.NameEn,
            NameAr = e.NameAr
        },
        DocumentTypeEssRules.CanBeRequestedAsLetter);

    public Task<List<SimpleLookupDto>> GetExpenseTypes() =>
        ListAsync(_expenseTypeRepository, e => new SimpleLookupDto
        {
            Id = e.Id,
            Code = e.Code,
            NameEn = e.NameEn,
            NameAr = e.NameAr
        });

    public Task<List<SimpleLookupDto>> GetHRServiceTypes() =>
        ListAsync(_hrServiceTypeRepository, e => new SimpleLookupDto
        {
            Id = e.Id,
            Code = e.Code,
            NameEn = e.NameEn,
            NameAr = e.NameAr
        });

    public Task<List<SimpleLookupDto>> GetHotels() =>
        ListAsync(_hotelRepository, e => new SimpleLookupDto
        {
            Id = e.Id,
            Code = e.Code,
            NameEn = e.NameEn,
            NameAr = e.NameAr
        });

    public Task<List<SimpleLookupDto>> GetLoanTypes() =>
        ListAsync(_loanTypeRepository, e => new SimpleLookupDto
        {
            Id = e.Id,
            Code = e.Code,
            NameEn = e.NameEn,
            NameAr = e.NameAr
        });

    public Task<List<SimpleLookupDto>> GetOvertimeTypes() =>
        ListAsync(_overtimeTypeRepository, e => new SimpleLookupDto
        {
            Id = e.Id,
            Code = e.Code,
            NameEn = e.NameEn,
            NameAr = e.NameAr
        });

    public Task<List<SimpleLookupDto>> GetTicketTypes() =>
        ListAsync(_ticketTypeRepository, e => new SimpleLookupDto
        {
            Id = e.Id,
            Code = e.Code,
            NameEn = e.NameEn,
            NameAr = e.NameAr
        });

    public Task<List<SimpleLookupDto>> GetPunchTypes() =>
        ListAsync(_transTypeRepository, e => new SimpleLookupDto
        {
            Id = e.Id,
            Code = e.Code,
            NameEn = e.NameEn,
            NameAr = e.NameAr
        });

    public Task<List<SimpleLookupDto>> GetTravelAirlines() =>
        ListAsync(_travelAirlineRepository, e => new SimpleLookupDto
        {
            Id = e.Id,
            Code = e.Code,
            NameEn = e.NameEn,
            NameAr = e.NameAr
        });

    public Task<List<SimpleLookupDto>> GetTravelDestinations() =>
        ListAsync(_travelDestinationRepository, e => new SimpleLookupDto
        {
            Id = e.Id,
            Code = e.Code,
            NameEn = e.NameEn,
            NameAr = e.NameAr
        });

    // ------------------------------------------------------------------ the colleague picker

    /// <remarks>
    /// <para>
    /// <b>Scope: active employees in the tenant, minus the caller. Deliberately not narrowed further.</b>
    /// </para>
    ///
    /// <para>
    /// Narrowing to the caller's own company or department was considered and rejected. <c>Employee</c>
    /// carries no company or department column at all -- org data lives on <c>EmployeeOrgAssignment</c>,
    /// which is date-bound and whose org keys are every one of them nullable. Filtering on it would drop
    /// anybody between assignments, anybody whose current assignment leaves the relevant key null, and
    /// anybody whose assignment rows have not been backfilled -- an invisible, data-dependent failure that
    /// hands some employees an empty picker and no way to tell why. That is the bug this service exists to
    /// fix, wearing a different coat.
    /// </para>
    ///
    /// <para>
    /// It is also the wrong rule on its own terms. Cover is most often needed precisely when it crosses a
    /// department -- a manager away for a fortnight hands their approvals to a peer in another function.
    /// The delegation still routes through the normal approval chain, so a delegation nobody should have
    /// made is caught where every other request is caught, by an approver, rather than by a dropdown that
    /// quietly omitted the right person.
    /// </para>
    ///
    /// <para>
    /// Self-exclusion is enforced here so the option is never offered, and again in
    /// <c>MyDelegationAppService.RequestDelegation</c> and in the workflow handler, because a filtered
    /// dropdown is a convenience and not a rule. An unlinked user (no <c>Employee</c> row -- admin accounts,
    /// typically) simply has nobody to exclude; they still cannot submit a delegation, which the request
    /// path refuses on its own.
    /// </para>
    /// </remarks>
    public async Task<List<EssColleagueLookupDto>> GetDelegationCandidates()
    {
        var myEmployeeId = await _employeeSessionService.GetCurrentEmployeeIdAsync();

        var query = _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active);

        if (myEmployeeId.HasValue)
        {
            query = query.Where(e => e.Id != myEmployeeId.Value);

            // Legacy ESS BaseRepository.GetDelegateEmployeeFilterOid: with ManageWorkflowSettings.DelegateEmployeeListBy
            // set to an organisation unit type, only colleagues under the same unit at that level are offered.
            var sameUnitIds = await SameOrgUnitColleagueIdsAsync(myEmployeeId.Value);
            if (sameUnitIds != null)
            {
                query = query.Where(e => sameUnitIds.Contains(e.Id));
            }
        }

        return await query
            .Select(e => new EssColleagueLookupDto
            {
                Id = e.Id,
                Code = e.Code,
                FullNameEn = e.FullNameEn,
                FullNameAr = e.FullNameAr
            })
            .OrderBy(e => e.FullNameEn)
            .ToListAsync();
    }

    /// <summary>
    /// The ids of the colleagues whose organisation unit at the configured level is the current
    /// employee's, or null when no level is configured or the employee has no unit at that level (then
    /// nobody is filtered out, as legacy's null filter did).
    /// </summary>
    private async Task<List<int>> SameOrgUnitColleagueIdsAsync(int myEmployeeId)
    {
        var workflowSettings = await _sysSettingsAccessor.GetWorkflowSettingsAsync();
        var levelTypeId = workflowSettings?.DelegateEmployeeListByOrganizationUnitTypeId;
        if (!levelTypeId.HasValue)
        {
            return null;
        }

        var levelCode = await _organizationUnitTypeRepository.GetAll()
            .Where(t => t.Id == levelTypeId.Value)
            .Select(t => t.Code)
            .FirstOrDefaultAsync();
        if (string.IsNullOrEmpty(levelCode))
        {
            return null;
        }

        var myUnit = await _orgHierarchyService.GetOrgUnitAtLevelForEmployeeAsync(myEmployeeId, levelCode);
        if (!myUnit.HasValue)
        {
            return null;
        }

        var candidateIds = await _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active && e.Id != myEmployeeId)
            .Select(e => e.Id)
            .ToListAsync();

        var sameUnit = new List<int>();
        foreach (var candidateId in candidateIds)
        {
            var unit = await _orgHierarchyService.GetOrgUnitAtLevelForEmployeeAsync(candidateId, levelCode);
            if (unit == myUnit)
            {
                sameUnit.Add(candidateId);
            }
        }

        return sameUnit;
    }

    // ------------------------------------------------------------------ the employee's own documents

    /// <remarks>
    /// <para>
    /// Not reference data like the lists above: these are the caller's own rows, scoped by employee id here
    /// and again on the write in <c>MyPassportWithdrawalAppService</c>. A filtered dropdown is a convenience,
    /// not the rule.
    /// </para>
    ///
    /// <para>
    /// The type restriction is the one the request enforces, from the same resolver: when the tenant has no
    /// document type carrying the configured code, nothing is restricted, exactly as the request then
    /// accepts any document. The <c>ApprovedOnly</c> filter is left on, so a document that is itself still
    /// awaiting approval is not offered for withdrawal.
    /// </para>
    ///
    /// <para>
    /// A user with no employee record gets an empty list rather than an error, as the colleague picker
    /// does; the request path refuses them on its own.
    /// </para>
    /// </remarks>
    [AbpAuthorize(AppPermissions.Ess_MyPassportWithdrawals)]
    public async Task<List<EssWithdrawableDocumentDto>> GetMyWithdrawableDocuments()
    {
        var myEmployeeId = await _employeeSessionService.GetCurrentEmployeeIdAsync();
        if (!myEmployeeId.HasValue)
        {
            return new List<EssWithdrawableDocumentDto>();
        }

        var withdrawableTypeIds = await _withdrawableDocumentTypeResolver.ResolveDocumentTypeIdsAsync();

        var query = _employeeDocumentRepository.GetAll()
            .Where(d => d.EmployeeId == myEmployeeId.Value);

        if (withdrawableTypeIds != null)
        {
            query = query.Where(d => withdrawableTypeIds.Contains(d.DocumentTypeId));
        }

        return await query
            .OrderBy(d => d.DocumentType.NameEn)
            .ThenBy(d => d.DocumentNum)
            .Select(d => new EssWithdrawableDocumentDto
            {
                Id = d.Id,
                DocumentNum = d.DocumentNum,
                DocumentTypeId = d.DocumentTypeId,
                DocumentTypeNameEn = d.DocumentType.NameEn,
                DocumentTypeNameAr = d.DocumentType.NameAr,
                IssueDate = d.IssueDate,
                ExpiryDate = d.ExpiryDate
            })
            .ToListAsync();
    }

    // ------------------------------------------------------------------ plumbing

    /// <summary>
    /// Projects a reference table straight to <see cref="SimpleLookupDto"/> in the database, so the full
    /// entity -- which for several of these carries payroll and configuration columns an employee has no
    /// business reading -- is never materialised.
    /// </summary>
    private static async Task<List<SimpleLookupDto>> ListAsync<TEntity>(
        IRepository<TEntity> repository,
        Expression<Func<TEntity, SimpleLookupDto>> selector,
        Expression<Func<TEntity, bool>> filter = null)
        where TEntity : class, IEntity<int>
    {
        var query = repository.GetAll();

        if (filter != null)
        {
            query = query.Where(filter);
        }

        return await query
            .Select(selector)
            .OrderBy(d => d.NameEn)
            .ToListAsync();
    }
}
