using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Info analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeInfo.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 469-1218). With <c>uscRptEmployeeCTC</c> this is the largest control in the
/// family: ~70 fixed columns plus one dynamic column per basic-pay wage type.
///
/// EVERY "as of" column is evaluated against TODAY -- legacy hid the date pickers on this screen and used
/// <c>DateTime.Now.Date</c> throughout (lines 475/787/814/...). There is no date range filter.
///
/// NOT PORTED (DevExpress/WebForms plumbing with no data meaning): the drill-up/drill-down session stack
/// (single-level here -- <c>GetKey()</c>/<c>GetTable()</c> define only "Table1", lines 380-450), the
/// ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, and the dead
/// <c>Table1_Old()</c> (lines 1219+).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeInfo)]
public class EmployeeInfoReportAppService : GridReportAppServiceBase, IEmployeeInfoReportAppService
{
    /// <summary>Legacy line 1128: wage type "1000" is basic pay and is excluded from TotalAllowance.</summary>
    private const string BasicPayWageTypeCode = "1000";

    /// <summary>Legacy lines 1183/1188: document types are matched by their ENGLISH name.</summary>
    private const string PassportDocumentTypeName = "Passport";

    private const string DrivingLicenseDocumentTypeName = "DRIVING LICENSE";

    private const string CivilIdDocumentTypeName = "Civil ID";

    /// <summary>Legacy lines 916-918: annual-leave opening / absent-unpaid opening transfer type codes.</summary>
    private const string AnnualLeaveTransferTypeCode = "1";

    private const string AbsentUnpaidTransferTypeCode = "5";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<EmployeeDocument> _employeeDocumentRepository;
    private readonly IRepository<EmployeeBank> _employeeBankRepository;
    private readonly IRepository<EmployeeSponsor> _employeeSponsorRepository;
    private readonly IRepository<EmployeeContract> _employeeContractRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<EmployeeFnFPayment> _employeeFnFPaymentRepository;
    private readonly IRepository<EmployeeLoan> _employeeLoanRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _basicPayDetailRepository;
    private readonly IRepository<TimeTransfer> _timeTransferRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeInfoReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<EmployeeDocument> employeeDocumentRepository,
        IRepository<EmployeeBank> employeeBankRepository,
        IRepository<EmployeeSponsor> employeeSponsorRepository,
        IRepository<EmployeeContract> employeeContractRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IRepository<EmployeeFnFPayment> employeeFnFPaymentRepository,
        IRepository<EmployeeLoan> employeeLoanRepository,
        IRepository<EmployeeBasicPayDetail> basicPayDetailRepository,
        IRepository<TimeTransfer> timeTransferRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeRepository = employeeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _employeeDocumentRepository = employeeDocumentRepository;
        _employeeBankRepository = employeeBankRepository;
        _employeeSponsorRepository = employeeSponsorRepository;
        _employeeContractRepository = employeeContractRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _employeeFnFPaymentRepository = employeeFnFPaymentRepository;
        _employeeLoanRepository = employeeLoanRepository;
        _basicPayDetailRepository = basicPayDetailRepository;
        _timeTransferRepository = timeTransferRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeInfo";

    protected override string ScreenCaption => "Employee Info";

    [HttpPost]
    public async Task<EmployeeInfoReportOutput> GetEmployeeInfoReport(GetEmployeeInfoReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var wageTypeNames = await FillWageColumnsAsync(rows);
        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new EmployeeInfoReportOutput
        {
            TotalCount = totalCount,
            Items = rows,
            WageTypeNames = wageTypeNames
        };
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeInfoReportToExcel(GetEmployeeInfoReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        var wageTypeNames = await FillWageColumnsAsync(rows);
        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeInfo.xlsx",
            rows,
            GetExportColumns(wageTypeNames),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    /// Ports legacy <c>Table1()</c>'s "Get Data"/"Fill Data" regions (lines 766-1210).
    ///
    /// The dynamic wage-type columns and the OrgLevel columns are not part of this query; see
    /// <see cref="FillWageColumnsAsync"/> and <see cref="StampOrgLevelsAsync"/>, which both entry points
    /// also call.
    /// </summary>
    private async Task<IQueryable<EmployeeInfoReportRowDto>> BuildQueryAsync(GetEmployeeInfoReportInput input)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var employeeStatus = input.EmployeeStatus;
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var today = Today;

        var timeSettings = await SysSettingsAccessor.GetTimeSettingsAsync();
        var earliestEvaluationDate = timeSettings == null
            ? default
            : timeSettings.EarliestEvaluationDate.Date;

        var query = _employeeRepository.GetAll()
            // Legacy lines 769-777.
            .WhereIf(employeeStatus.HasValue, e => e.Status == employeeStatus.Value)
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            // Legacy line 785 ApplyRowLevelSecurity -- an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        var currentAssignments = _orgAssignmentRepository.GetAll()
            .Where(o => o.StartDate <= today && o.EndDate >= today);

        var documents = _employeeDocumentRepository.GetAll();

        return query.Select(e => new EmployeeInfoReportRowDto
        {
            // Legacy "OID" column (line 1029).
            Id = e.Id,
            EmployeeCode = e.Code,
            EmployeeName = e.FullNameEn,
            EmployeeNameAr = e.FullNameAr,
            ReferenceCode = e.ReferenceCode,
            Status = e.Status,
            EmploymentType = e.EmploymentType,
            BirthDate = e.BirthDate,
            BloodGroup = e.BloodGroup,
            ProbationDays = e.ProbationDays,
            JoiningDate = e.JoiningDate,
            ProbationEndDate = e.ProbationEndDate,
            NationalityName = e.Nationality == null
                ? string.Empty
                : (useArabicNames ? e.Nationality.NameAr : e.Nationality.NameEn),
            ReligionName = e.Religion == null
                ? string.Empty
                : (useArabicNames ? e.Religion.NameAr : e.Religion.NameEn),
            Gender = e.Gender,
            MaritalStatusName = e.MaritalStatus == null
                ? string.Empty
                : (useArabicNames ? e.MaritalStatus.NameAr : e.MaritalStatus.NameEn),
            City = e.City,
            GovernorateName = e.Governorate == null
                ? string.Empty
                : (useArabicNames ? e.Governorate.NameAr : e.Governorate.NameEn),
            CountryName = e.Country == null
                ? string.Empty
                : (useArabicNames ? e.Country.NameAr : e.Country.NameEn),
            Telephone = e.Telephone,
            Mobile = e.Mobile,
            CurrentAddress = e.CurrentAddress,
            PermanentAddress = e.PermanentAddress,
            CivilID = e.CivilID,
            LoginID = e.LoginID,
            AccessID = e.AccessID,
            Email = e.Email,
            BirthPlaceName = e.BirthPlace == null
                ? string.Empty
                : (useArabicNames ? e.BirthPlace.NameAr : e.BirthPlace.NameEn),
            QualificationName = e.Qualification == null
                ? string.Empty
                : (useArabicNames ? e.Qualification.NameAr : e.Qualification.NameEn),

            // Legacy lines 813-822 / 1181-1189: a document VALID TODAY, matched by document type name.
            // Legacy's Passport clause also required IsActive == Active; HCMv3's EmployeeDocument has no
            // such flag, so that condition is dropped.
            PassportNumber = documents
                .Where(d => d.EmployeeId == e.Id
                            && d.IssueDate <= today
                            && d.ExpiryDate >= today
                            && d.DocumentType != null
                            && d.DocumentType.NameEn == PassportDocumentTypeName)
                .Select(d => d.DocumentNum)
                .FirstOrDefault(),
            DrivingLicense = documents
                .Where(d => d.EmployeeId == e.Id
                            && d.IssueDate <= today
                            && d.ExpiryDate >= today
                            && d.DocumentType != null
                            && d.DocumentType.NameEn == DrivingLicenseDocumentTypeName)
                .Select(d => d.DocumentNum)
                .FirstOrDefault(),

            // Legacy lines 824-833 / 1108-1112: the LATEST Civil ID expiry, regardless of validity.
            CivilIdExpiryDate = documents
                .Where(d => d.EmployeeId == e.Id
                            && d.DocumentType != null
                            && d.DocumentType.NameEn == CivilIdDocumentTypeName)
                .OrderByDescending(d => d.ExpiryDate)
                .Select(d => (DateTime?)d.ExpiryDate)
                .FirstOrDefault(),

            // Legacy lines 814-821 / 1071: current manager, "code - full name".
            ManagerName = _employeeManagerRepository.GetAll()
                .Where(m => m.EmployeeId == e.Id && m.StartDate <= today && m.EndDate >= today)
                .OrderBy(m => m.StartDate).ThenBy(m => m.Id)
                .Select(m => m.Manager == null
                    ? string.Empty
                    : m.Manager.Code + " - " + (useArabicNames ? m.Manager.FullNameAr : m.Manager.FullNameEn))
                .FirstOrDefault(),

            // Legacy lines 787-812 / 1074-1102: the org assignment current as of today.
            JobTitle = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null ? string.Empty : o.JobTitle.NameEn)
                .FirstOrDefault(),
            JobTitleAr = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null ? string.Empty : o.JobTitle.NameAr)
                .FirstOrDefault(),
            PayrollAreaName = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.PayrollArea == null ? string.Empty : o.PayrollArea.Name)
                .FirstOrDefault(),
            EmployeeGroupName = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.EmployeeGroup == null
                    ? string.Empty
                    : (useArabicNames ? o.EmployeeGroup.NameAr : o.EmployeeGroup.NameEn))
                .FirstOrDefault(),
            EmployeeSubGroupName = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.EmployeeSubGroup == null
                    ? string.Empty
                    : (useArabicNames ? o.EmployeeSubGroup.NameAr : o.EmployeeSubGroup.NameEn))
                .FirstOrDefault(),
            WorkLocationName = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null
                    ? string.Empty
                    : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault(),
            CostCenterName = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.CostCenter == null
                    ? string.Empty
                    : (useArabicNames ? o.CostCenter.NameAr : o.CostCenter.NameEn))
                .FirstOrDefault(),
            OrganizationUnitId = currentAssignments
                .Where(o => o.EmployeeId == e.Id)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),

            // Legacy lines 855-869 / 1104-1111: the sponsor record current as of today.
            SponsorName = _employeeSponsorRepository.GetAll()
                .Where(s => s.EmployeeId == e.Id && s.StartDate <= today && s.EndDate >= today)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => s.Sponsor == null
                    ? string.Empty
                    : (useArabicNames ? s.Sponsor.NameAr : s.Sponsor.NameEn))
                .FirstOrDefault(),
            GovtJobTitleName = _employeeSponsorRepository.GetAll()
                .Where(s => s.EmployeeId == e.Id && s.StartDate <= today && s.EndDate >= today)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => s.GovtJobTitle == null
                    ? string.Empty
                    : (useArabicNames ? s.GovtJobTitle.NameAr : s.GovtJobTitle.NameEn))
                .FirstOrDefault(),
            ContractSalary = _employeeSponsorRepository.GetAll()
                .Where(s => s.EmployeeId == e.Id && s.StartDate <= today && s.EndDate >= today)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => s.ContractSalary)
                .FirstOrDefault(),
            ResidencyType = _employeeSponsorRepository.GetAll()
                .Where(s => s.EmployeeId == e.Id && s.StartDate <= today && s.EndDate >= today)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => (ResidencyType?)s.ResidencyType)
                .FirstOrDefault(),
            WorkVisaTypeName = _employeeSponsorRepository.GetAll()
                .Where(s => s.EmployeeId == e.Id && s.StartDate <= today && s.EndDate >= today)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => s.WorkVisaType == null ? string.Empty : s.WorkVisaType.NameEn)
                .FirstOrDefault(),

            // Legacy lines 843-854 / 1180-1188: the bank record current as of today.
            BankName = _employeeBankRepository.GetAll()
                .Where(b => b.EmployeeId == e.Id && b.StartDate <= today && b.EndDate >= today)
                .OrderBy(b => b.StartDate).ThenBy(b => b.Id)
                .Select(b => b.Bank == null
                    ? string.Empty
                    : (useArabicNames ? b.Bank.NameAr : b.Bank.NameEn))
                .FirstOrDefault(),
            PaymentType = _employeeBankRepository.GetAll()
                .Where(b => b.EmployeeId == e.Id && b.StartDate <= today && b.EndDate >= today)
                .OrderBy(b => b.StartDate).ThenBy(b => b.Id)
                .Select(b => (PaymentType?)b.PaymentType)
                .FirstOrDefault(),
            AccountNum = _employeeBankRepository.GetAll()
                .Where(b => b.EmployeeId == e.Id && b.StartDate <= today && b.EndDate >= today)
                .OrderBy(b => b.StartDate).ThenBy(b => b.Id)
                .Select(b => b.AccountNum)
                .FirstOrDefault(),
            IBAN = _employeeBankRepository.GetAll()
                .Where(b => b.EmployeeId == e.Id && b.StartDate <= today && b.EndDate >= today)
                .OrderBy(b => b.StartDate).ThenBy(b => b.Id)
                .Select(b => b.IBAN)
                .FirstOrDefault(),

            // Legacy lines 871-881 / 1191-1198: the contract current as of today.
            ContractTypeName = _employeeContractRepository.GetAll()
                .Where(c => c.EmployeeId == e.Id && c.StartDate <= today && c.EndDate >= today)
                .OrderBy(c => c.StartDate).ThenBy(c => c.Id)
                .Select(c => c.ContractType == null
                    ? string.Empty
                    : (useArabicNames ? c.ContractType.NameAr : c.ContractType.NameEn))
                .FirstOrDefault(),
            ContractStartDate = _employeeContractRepository.GetAll()
                .Where(c => c.EmployeeId == e.Id && c.StartDate <= today && c.EndDate >= today)
                .OrderBy(c => c.StartDate).ThenBy(c => c.Id)
                .Select(c => (DateTime?)c.StartDate)
                .FirstOrDefault(),
            ContractEndDate = _employeeContractRepository.GetAll()
                .Where(c => c.EmployeeId == e.Id && c.StartDate <= today && c.EndDate >= today)
                .OrderBy(c => c.StartDate).ThenBy(c => c.Id)
                .Select(c => (DateTime?)c.EndDate)
                .FirstOrDefault(),

            // Legacy lines 883-893 / 1006-1007: the latest NON-REVOKED separation.
            SeparationType = _employeeSeparationRepository.GetAll()
                .Where(s => s.EmployeeId == e.Id && s.RevokeDate == null)
                .OrderByDescending(s => s.LastDayInService)
                .Select(s => (SeparationType?)s.SeparationType)
                .FirstOrDefault(),
            SeparationDate = _employeeSeparationRepository.GetAll()
                .Where(s => s.EmployeeId == e.Id && s.RevokeDate == null)
                .OrderByDescending(s => s.LastDayInService)
                .Select(s => (DateTime?)s.SubmissionDate)
                .FirstOrDefault(),
            LastDateOfWorking = _employeeSeparationRepository.GetAll()
                .Where(s => s.EmployeeId == e.Id && s.RevokeDate == null)
                .OrderByDescending(s => s.LastDayInService)
                .Select(s => (DateTime?)s.LastDayInService)
                .FirstOrDefault(),

            // Legacy lines 1160-1178: the FnF status ladder. The latest compensation date is fetched here
            // and the three-way decision is made below, in FillWageColumnsAsync's sibling loop.
            FnfStatus = _employeeFnFPaymentRepository.GetAll()
                .Where(f => f.EmployeeId == e.Id)
                .OrderByDescending(f => f.CompensationDate)
                .Select(f => f.CompensationDate <= today ? "Fnf Done" : "Fnf Pending")
                .FirstOrDefault(),

            // Legacy lines 903-909 / 1015: SUM over ALL of the employee's loans.
            TotalLoanBalance = _employeeLoanRepository.GetAll()
                .Where(l => l.EmployeeId == e.Id)
                .Sum(l => l.BalanceDueAmount),

            // Legacy lines 887-897 / 1017-1018: opening balances are TimeTransfer rows stamped exactly on
            // the configured earliest evaluation date.
            AnnualLeaveOpening = _timeTransferRepository.GetAll()
                .Where(t => t.EmployeeId == e.Id
                            && t.TransferDate == earliestEvaluationDate
                            && t.TransferType.Code == AnnualLeaveTransferTypeCode)
                .Sum(t => t.TransferCount),
            AbsentUnpaidOpening = _timeTransferRepository.GetAll()
                .Where(t => t.EmployeeId == e.Id
                            && t.TransferDate == earliestEvaluationDate
                            && t.TransferType.Code == AbsentUnpaidTransferTypeCode)
                .Sum(t => t.TransferCount)
        });
    }

    /// <summary>
    /// Ports the dynamic basic-pay pivot (legacy lines 475-491 for the column set, 1124-1131 for the
    /// values) plus the derived Age and FnfStatus columns. Runs after materialization because the column
    /// count is data-dependent, and is shared by the paged read and the export.
    /// </summary>
    private async Task<List<string>> FillWageColumnsAsync(List<EmployeeInfoReportRowDto> rows)
    {
        var today = Today;
        var useArabicNames = UseArabicNames;

        // Legacy lines 475-491: every basic pay detail whose parent basic pay is in force TODAY, with the
        // wage-type column set ordered by PayrollOrder.
        var basicPay = _basicPayDetailRepository.GetAll()
            .Where(d => d.EmployeeBasicPay.StartDate <= today
                        && d.EmployeeBasicPay.EndDate >= today
                        && d.WageType != null);

        var wageTypes = await basicPay
            .Select(d => new
            {
                d.WageType.Code,
                Name = useArabicNames
                    ? (d.WageType.NameAr ?? d.WageType.NameEn)
                    : d.WageType.NameEn,
                d.WageType.PayrollOrder
            })
            .Distinct()
            .OrderBy(w => w.PayrollOrder)
            .ToListAsync();

        var wageTypeNames = wageTypes.Select(w => w.Name).ToList();

        if (rows.Count == 0)
        {
            return wageTypeNames;
        }

        var employeeIds = rows.Select(r => r.Id).ToList();

        var amounts = await basicPay
            .Where(d => employeeIds.Contains(d.EmployeeBasicPay.EmployeeId))
            .Select(d => new
            {
                EmployeeId = d.EmployeeBasicPay.EmployeeId,
                WageTypeCode = d.WageType.Code,
                d.Amount,
                Currency = d.Currency == null
                    ? null
                    : (useArabicNames ? d.Currency.CurrencyShortFormAr : d.Currency.CurrencyShortFormEn)
            })
            .ToListAsync();

        var byEmployee = amounts.GroupBy(a => a.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var row in rows)
        {
            // Legacy lines 1036-1041: whole years as of today.
            var age = today.Year - row.BirthDate.Year;
            if (today < row.BirthDate.AddYears(age))
            {
                age--;
            }

            row.Age = age;

            // Legacy lines 1168-1172: the notice-period case wins over the FnF payment state.
            if (row.SeparationType == null)
            {
                row.FnfStatus = string.Empty;
            }
            else if (row.SeparationDate.HasValue
                     && row.LastDateOfWorking.HasValue
                     && row.SeparationDate.Value <= today
                     && row.LastDateOfWorking.Value >= today)
            {
                row.FnfStatus = "In Notice Period";
            }
            else if (row.FnfStatus.IsNullOrEmpty())
            {
                // No FnF payment row at all -> pending (legacy line 1174).
                row.FnfStatus = "Fnf Pending";
            }

            byEmployee.TryGetValue(row.Id, out var employeeAmounts);

            row.WageAmounts = new List<EmployeeInfoWageAmountDto>(wageTypes.Count);

            foreach (var wageType in wageTypes)
            {
                var amount = employeeAmounts?
                    .Where(a => a.WageTypeCode == wageType.Code)
                    .Sum(a => a.Amount) ?? 0;

                row.WageAmounts.Add(new EmployeeInfoWageAmountDto
                {
                    WageTypeCode = wageType.Code,
                    WageTypeName = wageType.Name,
                    Amount = amount
                });
            }

            // Legacy lines 1133-1134.
            row.TotalAllowance = employeeAmounts?
                .Where(a => a.WageTypeCode != BasicPayWageTypeCode)
                .Sum(a => a.Amount) ?? 0;
            row.TotalSalary = employeeAmounts?.Sum(a => a.Amount) ?? 0;

            // Legacy lines 1128-1130: the FIRST non-empty currency wins.
            row.Currency = employeeAmounts?
                .Select(a => a.Currency)
                .FirstOrDefault(c => !string.IsNullOrEmpty(c)) ?? string.Empty;
        }

        return wageTypeNames;
    }

    /// <summary>
    /// Fills OrgLevel1..7 (English, as legacy's base columns did) and OrgLevel1Ar..7Ar. Shared by the read
    /// and the export. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<EmployeeInfoReportRowDto> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var englishLookup = await _orgLevelNameResolver.BuildLookupAsync(false);
        var arabicLookup = await _orgLevelNameResolver.BuildLookupAsync(true);

        foreach (var row in rows)
        {
            var en = englishLookup.GetLevelNames(row.OrganizationUnitId);
            row.OrgLevel1 = en[0];
            row.OrgLevel2 = en[1];
            row.OrgLevel3 = en[2];
            row.OrgLevel4 = en[3];
            row.OrgLevel5 = en[4];
            row.OrgLevel6 = en[5];
            row.OrgLevel7 = en[6];

            var ar = arabicLookup.GetLevelNames(row.OrganizationUnitId);
            row.OrgLevel1Ar = ar[0];
            row.OrgLevel2Ar = ar[1];
            row.OrgLevel3Ar = ar[2];
            row.OrgLevel4Ar = ar[3];
            row.OrgLevel5Ar = ar[4];
            row.OrgLevel6Ar = ar[5];
            row.OrgLevel7Ar = ar[6];
        }
    }

    /// <summary>The exported column set, in legacy column order. The technical OID key is omitted.</summary>
    private static IReadOnlyList<GridReportColumn<EmployeeInfoReportRowDto>> GetExportColumns(
        List<string> wageTypeNames)
    {
        var columns = new List<GridReportColumn<EmployeeInfoReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("FullNameAr", r => r.EmployeeNameAr),
            new("ReferenceCode", r => r.ReferenceCode),
            new("Status", r => r.Status.ToString()),
            new("EmploymentType", r => r.EmploymentType.ToString()),
            new("BirthDate", r => r.BirthDate),
            new("Age", r => r.Age),
            new("BloodGroup", r => r.BloodGroup),
            new("ProbationDays", r => r.ProbationDays),
            new("JoiningDate", r => r.JoiningDate),
            new("ProbationEndDate", r => r.ProbationEndDate),
            new("Nationality", r => r.NationalityName),
            new("Religion", r => r.ReligionName),
            new("Gender", r => r.Gender.ToString()),
            new("MaritalStatus", r => r.MaritalStatusName),
            new("City", r => r.City),
            new("Governorate", r => r.GovernorateName),
            new("Country", r => r.CountryName),
            new("Telephone", r => r.Telephone),
            new("Mobile", r => r.Mobile),
            new("CurrentAddress", r => r.CurrentAddress),
            new("PermanentAddress", r => r.PermanentAddress),
            new("CivilID", r => r.CivilID),
            new("LoginID", r => r.LoginID),
            new("AccessID", r => r.AccessID),
            new("PassportNumber", r => r.PassportNumber),
            new("DrivingLicense", r => r.DrivingLicense),
            new("Email", r => r.Email),
            new("BirthPlace", r => r.BirthPlaceName),
            new("Qualification", r => r.QualificationName),
            new("Manager", r => r.ManagerName),
            new("JobTitle", r => r.JobTitle),
            new("JobTitleAr", r => r.JobTitleAr),
            new("PayrollArea", r => r.PayrollAreaName),
            new("EmployeeGroup", r => r.EmployeeGroupName),
            new("EmployeeSubGroup", r => r.EmployeeSubGroupName),
            new("WorkLocation", r => r.WorkLocationName),
            new("CostCenter", r => r.CostCenterName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel1Ar", r => r.OrgLevel1Ar),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel2Ar", r => r.OrgLevel2Ar),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel3Ar", r => r.OrgLevel3Ar),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel4Ar", r => r.OrgLevel4Ar),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel5Ar", r => r.OrgLevel5Ar),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel6Ar", r => r.OrgLevel6Ar),
            new("OrgLevel7", r => r.OrgLevel7),
            new("OrgLevel7Ar", r => r.OrgLevel7Ar),
            new("Sponsor", r => r.SponsorName),
            new("GovtJobTitle", r => r.GovtJobTitleName),
            new("ContractSalary", r => r.ContractSalary),
            new("ResidencyType", r => r.ResidencyType?.ToString()),
            new("WorkVisaType", r => r.WorkVisaTypeName),
            new("CivilIdExpiryDate", r => r.CivilIdExpiryDate)
        };

        for (var i = 0; i < wageTypeNames.Count; i++)
        {
            var index = i;
            columns.Add(GridReportColumn<EmployeeInfoReportRowDto>.FromLiteralHeader(
                wageTypeNames[i],
                r => index < r.WageAmounts.Count ? r.WageAmounts[index].Amount : 0));
        }

        columns.AddRange(new List<GridReportColumn<EmployeeInfoReportRowDto>>
        {
            new("TotalAllowance", r => r.TotalAllowance),
            new("TotalSalary", r => r.TotalSalary),
            new("Currency", r => r.Currency),
            new("SeparationType", r => r.SeparationType?.ToString()),
            new("SeparationDate", r => r.SeparationDate),
            new("LastDateOfWorking", r => r.LastDateOfWorking),
            new("FnfStatus", r => r.FnfStatus),
            new("Bank", r => r.BankName),
            new("PaymentType", r => r.PaymentType?.ToString()),
            new("AccountNum", r => r.AccountNum),
            new("IBAN", r => r.IBAN),
            new("ContractType", r => r.ContractTypeName),
            new("ContractStartDate", r => r.ContractStartDate),
            new("ContractEndDate", r => r.ContractEndDate),
            new("AnnualLeaveOpening", r => r.AnnualLeaveOpening),
            new("AbsentUnpaidOpening", r => r.AbsentUnpaidOpening),
            new("TotalLoanBalance", r => r.TotalLoanBalance)
        });

        return columns;
    }
}
