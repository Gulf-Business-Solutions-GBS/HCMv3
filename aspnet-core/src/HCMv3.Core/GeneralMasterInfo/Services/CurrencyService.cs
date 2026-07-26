using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GeneralMasterInfo.Services;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.Services.CurrencyService</c>.
///
/// ADAPTATION for <see cref="RoundAsync"/>: legacy read <c>Employee.CurrentPayrollArea</c>, a Wave-B-era
/// XAF computed property that has no equivalent on the ported <c>HCMv3.PersonnelAdministration.Employee</c>
/// entity (grepped fully -- no <c>CurrentPayrollArea</c>/<c>OrgLevel*</c> members exist on it; org
/// assignment was collapsed into the separate, date-bound <see cref="EmployeeOrgAssignment"/> entity with
/// a single <c>OrganizationUnitId</c> FK, see its class remarks). This port instead resolves the
/// employee's most-recent (by <c>EndDate</c>) <see cref="EmployeeOrgAssignment"/>, follows its
/// <c>OrganizationUnitId</c> to the matching <see cref="OrgUnitProfile"/> row (which carries the
/// <c>PayrollAreaId</c> FK added in the Wave 4 gap-fill), and reads <c>PayrollArea.Currency.DecimalCount</c>
/// from there -- functionally equivalent to the legacy "employee's current payroll area's currency" lookup.
///
/// NOTE: <c>HCMv3.Core</c> has no reference to EntityFrameworkCore (per architecture rules), so all
/// queries here use the plain synchronous <c>IQueryable</c> returned by <c>IRepository&lt;T&gt;.GetAll()</c>
/// (same pattern as the already-shipped <c>PasswordExpirationService</c> domain service), wrapped in
/// <c>Task.FromResult</c>/<c>Task.Run</c> for interface consistency with the rest of this port's domain
/// services.
/// </summary>
[UnitOfWork]
public class CurrencyService : HCMv3DomainServiceBase, ICurrencyService
{
    private readonly IRepository<Currency> _currencyRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<OrgUnitProfile> _orgUnitProfileRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;

    public CurrencyService(
        IRepository<Currency> currencyRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<OrgUnitProfile> orgUnitProfileRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository)
    {
        _currencyRepository = currencyRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgUnitProfileRepository = orgUnitProfileRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
    }

    public Task<Currency> GetDefaultCurrencyAsync()
    {
        return Task.FromResult(_currencyRepository.GetAll().FirstOrDefault(c => c.Code == "KWD"));
    }

    public Task<double> RoundAsync(int employeeId, double value)
    {
        var latestAssignment = _employeeOrgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeId == employeeId && a.OrganizationUnitId != null)
            .OrderByDescending(a => a.EndDate)
            .FirstOrDefault();

        if (latestAssignment?.OrganizationUnitId == null)
        {
            return Task.FromResult(value);
        }

        var profile = _orgUnitProfileRepository
            .GetAllIncluding(p => p.PayrollArea, p => p.PayrollArea.Currency)
            .FirstOrDefault(p => p.OrganizationUnitId == latestAssignment.OrganizationUnitId.Value && p.PayrollAreaId != null);

        if (profile?.PayrollArea?.Currency == null)
        {
            return Task.FromResult(value);
        }

        return Task.FromResult(System.Math.Round(value, profile.PayrollArea.Currency.DecimalCount));
    }

    public Task<Currency> GetEmployeeCurrencyAsync(int employeeId)
    {
        var latestBasicPay = _employeeBasicPayRepository.GetAll()
            .Where(bp => bp.EmployeeId == employeeId)
            .OrderByDescending(bp => bp.EndDate)
            .FirstOrDefault();

        if (latestBasicPay == null)
        {
            return Task.FromResult<Currency>(null);
        }

        var currency = _employeeBasicPayDetailRepository.GetAll()
            .Where(d => d.EmployeeBasicPayId == latestBasicPay.Id)
            .Select(d => d.Currency)
            .FirstOrDefault();

        return Task.FromResult(currency);
    }
}
