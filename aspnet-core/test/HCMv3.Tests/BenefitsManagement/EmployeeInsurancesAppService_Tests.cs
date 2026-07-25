using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.BenefitsManagement;
using HCMv3.BenefitsManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.BenefitsManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeInsurancesAppService_Tests : BenefitsManagementAppServiceTestBase
{
    private EmployeeInsurancesAppService CreateAppService()
    {
        return CreateService(() => new EmployeeInsurancesAppService(FakeRepository.For<EmployeeInsurance>(Context)));
    }

    private CreateOrEditEmployeeInsuranceDto BuildValidDto(int employeeId, int planId, int providerId)
    {
        return new CreateOrEditEmployeeInsuranceDto
        {
            EmployeeId = employeeId,
            InsurancePlanId = planId,
            InsuranceProviderId = providerId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            InsuranceAmount = 1200,
            CompanyContribution = 600,
            EmployeeContribution = 600
        };
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_EmployeeInsurance()
    {
        var employeeId = CreateEmployee();
        var planId = CreateInsurancePlan();
        var providerId = CreateInsuranceProvider();
        var service = CreateAppService();

        await service.CreateOrEdit(BuildValidDto(employeeId, planId, providerId));

        var result = await service.GetAll(new GetAllEmployeeInsurancesInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].InsuranceAmount.ShouldBe(1200);
    }

    [Fact]
    public async Task Should_Default_InstallmentStartDate_And_Balances_On_Create()
    {
        var employeeId = CreateEmployee();
        var planId = CreateInsurancePlan();
        var providerId = CreateInsuranceProvider();
        var service = CreateAppService();

        await service.CreateOrEdit(BuildValidDto(employeeId, planId, providerId));

        var result = await service.GetAll(new GetAllEmployeeInsurancesInput { EmployeeId = employeeId });
        var dto = result.Items[0];

        dto.InstallmentStartDate.ShouldBe(new DateTime(2026, 2, 1));
        dto.BalanceCompanyContribution.ShouldBe(600);
        dto.BalanceEmployeeContribution.ShouldBe(600);
        dto.InstallmentCompanyDedAmount.ShouldBeGreaterThan(0);
        dto.InstallmentEmployeeDedAmount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Before_StartDate()
    {
        var employeeId = CreateEmployee();
        var planId = CreateInsurancePlan();
        var providerId = CreateInsuranceProvider();
        var service = CreateAppService();

        var input = BuildValidDto(employeeId, planId, providerId);
        input.EndDate = new DateTime(2025, 1, 1);

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(input));
    }

    [Fact]
    public async Task Should_Throw_When_InsuranceAmount_Zero()
    {
        var employeeId = CreateEmployee();
        var planId = CreateInsurancePlan();
        var providerId = CreateInsuranceProvider();
        var service = CreateAppService();

        var input = BuildValidDto(employeeId, planId, providerId);
        input.InsuranceAmount = 0;
        input.CompanyContribution = 0;
        input.EmployeeContribution = 0;

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(input));
    }

    [Fact]
    public async Task Should_Throw_When_Contributions_Do_Not_Match_InsuranceAmount()
    {
        var employeeId = CreateEmployee();
        var planId = CreateInsurancePlan();
        var providerId = CreateInsuranceProvider();
        var service = CreateAppService();

        var input = BuildValidDto(employeeId, planId, providerId);
        input.EmployeeContribution = 500; // 500 + 600 != 1200

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(input));
    }
}
