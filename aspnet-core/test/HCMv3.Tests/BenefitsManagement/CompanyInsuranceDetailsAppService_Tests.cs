using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.BenefitsManagement;
using HCMv3.BenefitsManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.BenefitsManagement;

// ReSharper disable once InconsistentNaming
public class CompanyInsuranceDetailsAppService_Tests : BenefitsManagementAppServiceTestBase
{
    private CompanyInsuranceDetailsAppService CreateAppService()
    {
        return CreateService(() => new CompanyInsuranceDetailsAppService(
            FakeRepository.For<CompanyInsuranceDetail>(Context),
            FakeRepository.For<EmployeeInsurance>(Context)));
    }

    [Fact]
    public async Task Should_Create_Detail_And_Recalculate_Parent_Balance()
    {
        var employeeId = CreateEmployee();
        var planId = CreateInsurancePlan();
        var providerId = CreateInsuranceProvider();
        var employeeInsuranceId = CreateEmployeeInsurance(employeeId, planId, providerId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCompanyInsuranceDetailDto
        {
            EmployeeInsuranceId = employeeInsuranceId,
            InstallmentDate = new DateTime(2026, 2, 1),
            InstallmentAmount = 150,
            InstallmentType = InstallmentType.External
        });

        var result = await service.GetAll(new GetAllCompanyInsuranceDetailsInput { EmployeeInsuranceId = employeeInsuranceId });
        result.TotalCount.ShouldBe(1);

        var parent = Context.EmployeeInsurances.Find(employeeInsuranceId);
        parent.BalanceCompanyContribution.ShouldBe(450);
    }

    [Fact]
    public async Task Should_Throw_When_InstallmentAmount_Exceeds_CompanyContribution()
    {
        var employeeId = CreateEmployee();
        var planId = CreateInsurancePlan();
        var providerId = CreateInsuranceProvider();
        var employeeInsuranceId = CreateEmployeeInsurance(employeeId, planId, providerId, companyContribution: 200);
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(new CreateOrEditCompanyInsuranceDetailDto
        {
            EmployeeInsuranceId = employeeInsuranceId,
            InstallmentDate = new DateTime(2026, 2, 1),
            InstallmentAmount = 300,
            InstallmentType = InstallmentType.External
        }));
    }

    [Fact]
    public async Task Should_Throw_When_InstallmentDate_Before_InstallmentStartDate()
    {
        var employeeId = CreateEmployee();
        var planId = CreateInsurancePlan();
        var providerId = CreateInsuranceProvider();
        var employeeInsuranceId = CreateEmployeeInsurance(employeeId, planId, providerId);
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(new CreateOrEditCompanyInsuranceDetailDto
        {
            EmployeeInsuranceId = employeeInsuranceId,
            InstallmentDate = new DateTime(2026, 1, 1),
            InstallmentAmount = 100,
            InstallmentType = InstallmentType.External
        }));
    }

    [Fact]
    public async Task Should_Recalculate_Balance_On_Delete()
    {
        var employeeId = CreateEmployee();
        var planId = CreateInsurancePlan();
        var providerId = CreateInsuranceProvider();
        var employeeInsuranceId = CreateEmployeeInsurance(employeeId, planId, providerId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCompanyInsuranceDetailDto
        {
            EmployeeInsuranceId = employeeInsuranceId,
            InstallmentDate = new DateTime(2026, 2, 1),
            InstallmentAmount = 150,
            InstallmentType = InstallmentType.External
        });

        var result = await service.GetAll(new GetAllCompanyInsuranceDetailsInput { EmployeeInsuranceId = employeeInsuranceId });
        var detailId = result.Items[0].Id;

        await service.Delete(new EntityDto(detailId));

        var parent = Context.EmployeeInsurances.Find(employeeInsuranceId);
        parent.BalanceCompanyContribution.ShouldBe(600);
    }
}
