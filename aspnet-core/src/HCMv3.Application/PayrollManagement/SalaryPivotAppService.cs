using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.PayrollManagement;

/// <inheritdoc cref="ISalaryPivotAppService"/>
public class SalaryPivotAppService : HCMv3AppServiceBase, ISalaryPivotAppService
{
    /// <summary>Legacy line 166 excludes the Net Pay wage type so it is not double counted.</summary>
    private const string WageCodeNetPay = "9000";

    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;

    public SalaryPivotAppService(
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
    }

    /// <summary>
    /// Legacy line 166's projection, verbatim: <c>Amount != 0 &amp;&amp; WageType.Code != "9000"</c>.
    ///
    /// <para>Legacy's <c>Employee.OrgAssignment[0]</c> is XPO's "first row of the aggregated collection",
    /// i.e. lowest key -- reproduced here as the employee's lowest-Id org assignment, NOT the current-dated
    /// one. Preserved deliberately so the Department column matches the legacy screen.</para>
    ///
    /// <para><c>[HttpPost]</c> per this repo's NSwag rule for a custom input DTO.</para>
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
    public async Task<SalaryPivotDataDto> GetSalaryPivotData(GetSalaryPivotDataInput input)
    {
        var firstOrgAssignments = _employeeOrgAssignmentRepository.GetAll()
            .GroupBy(r => r.EmployeeId)
            .Select(g => g.OrderBy(r => r.Id).FirstOrDefault());

        var query = _salaryRegisterDetailRepository.GetAll()
            .Where(r => r.Amount != 0 && r.WageType.Code != WageCodeNetPay)
            .WhereIf(input.FromPeriod.HasValue, r => r.SalaryRegister.ForPeriod >= input.FromPeriod.Value)
            .WhereIf(input.ToPeriod.HasValue, r => r.SalaryRegister.ForPeriod <= input.ToPeriod.Value)
            .Select(r => new SalaryPivotRowDto
            {
                Employee = r.SalaryRegister.Employee.FullNameEn,
                Code = r.SalaryRegister.Employee.Code,
                ForPeriod = r.SalaryRegister.ForPeriod,
                WageType = r.WageType.NameEn,
                BillingType = r.WageType.BillingType,
                Amount = r.Amount,
                Department = firstOrgAssignments
                    .Where(oa => oa.EmployeeId == r.SalaryRegister.EmployeeId)
                    .Select(oa => oa.EmployeeGroup.NameEn)
                    .FirstOrDefault()
            });

        return await Task.FromResult(new SalaryPivotDataDto { Rows = query.ToList() });
    }
}
