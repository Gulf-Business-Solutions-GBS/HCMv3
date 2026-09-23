using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.SystemSettings;
using HCMv3.Tests.GridReports;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Legacy <c>EmployeeOrgAssignment.AddAutoWageAccount</c>: with <c>ManageFIPostingSettings.EnableAutoWageAccountEntry</c>
/// on, moving an employee to another cost centre copies their current wage accounts into rows dated like
/// the new assignment, with the "Location" cost element slot pointing at the cost element whose code is the
/// new cost centre's code.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeOrgAssignmentAutoWageAccount_Tests : AppTestBase
{
    private readonly IEmployeeOrgAssignmentsAppService _appService;
    private readonly PayrollJVReportTestDataBuilder _data;

    public EmployeeOrgAssignmentAutoWageAccount_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeOrgAssignmentsAppService>();
        _data = new PayrollJVReportTestDataBuilder(UsingDbContext, TenantId);
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    /// <summary>Real wage accounts run to 31/12/9999; legacy copies only those covering the new assignment's end.</summary>
    private static readonly DateTime OpenEnded = new(9999, 12, 31);

    private void SetAutoWageAccountEntry(bool enabled)
    {
        UsingDbContext(context =>
        {
            var settings = context.Set<ManageFIPostingSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                           ?? context.Set<ManageFIPostingSettings>().Add(new ManageFIPostingSettings { TenantId = TenantId }).Entity;
            settings.EnableAutoWageAccountEntry = enabled;
        });
    }

    /// <summary>The cost element type named "Location" occupying slot 3, as legacy's CostService constants named it.</summary>
    private void CreateLocationCostElementType()
    {
        UsingDbContext(context =>
        {
            if (!context.Set<CostElementType>().Any(t => t.NameEn == "Location"))
            {
                context.Set<CostElementType>().Add(new CostElementType
                {
                    TenantId = TenantId,
                    Code = CostElementNames.CostElement3,
                    NameEn = "Location",
                    NameAr = "الموقع"
                });
            }
        });
    }

    [Fact]
    public async Task Moving_To_Another_Cost_Centre_Should_Copy_The_Wage_Accounts_With_The_New_Location()
    {
        SetAutoWageAccountEntry(true);
        CreateLocationCostElementType();

        var employee = _data.CreateEmployee("AWA100");
        var wageType = _data.CreateWageType("1000", BillingType.Earning);
        var netPay = _data.CreateWageType("9000", BillingType.Earning);
        var account = _data.CreateAccount("50001", "Salaries");
        var oldCostCentre = _data.CreateCostCenter("CC-OLD");
        var newCostCentre = _data.CreateCostCenter("CC-NEW");
        var oldLocation = _data.CreateCostElement("CC-OLD");
        var newLocation = _data.CreateCostElement("CC-NEW");

        _data.CreateOrgAssignment(employee, null, new DateTime(2024, 1, 1), new DateTime(2099, 12, 31), costCenter: oldCostCentre);
        _data.CreateWageAccount(employee, wageType, account, costElement3: oldLocation, endDate: OpenEnded);
        _data.CreateWageAccount(employee, netPay, account, costElement3: oldLocation, endDate: OpenEnded);

        await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2024, 7, 1),
            CostCenterId = newCostCentre.Id
        });

        var copied = UsingDbContext(context => context.Set<EmployeeWageAccount>()
            .Where(w => w.EmployeeId == employee.Id && w.StartDate == new DateTime(2024, 7, 1))
            .ToList());

        // Net Pay (9000) is never copied, exactly as legacy.
        copied.Count.ShouldBe(1);
        copied[0].WageTypeId.ShouldBe(wageType.Id);
        copied[0].CostElement3Id.ShouldBe(newLocation.Id);
        copied[0].AccountInfoId.ShouldBe(account.Id);
    }

    [Fact]
    public async Task With_The_Setting_Off_Nothing_Is_Copied()
    {
        SetAutoWageAccountEntry(false);
        CreateLocationCostElementType();

        var employee = _data.CreateEmployee("AWA110");
        var wageType = _data.CreateWageType("1001", BillingType.Earning);
        var account = _data.CreateAccount("50002", "Salaries 2");
        var oldCostCentre = _data.CreateCostCenter("CC-OLD2");
        var newCostCentre = _data.CreateCostCenter("CC-NEW2");

        _data.CreateOrgAssignment(employee, null, new DateTime(2024, 1, 1), new DateTime(2099, 12, 31), costCenter: oldCostCentre);
        _data.CreateWageAccount(employee, wageType, account, endDate: OpenEnded);

        await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2024, 7, 1),
            CostCenterId = newCostCentre.Id
        });

        UsingDbContext(context => context.Set<EmployeeWageAccount>()
            .Count(w => w.EmployeeId == employee.Id)).ShouldBe(1);
    }
}
