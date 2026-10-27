using System;
using System.Threading.Tasks;
using HCMv3.ProjectManagement;
using HCMv3.ProjectManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ProjectManagement;

// ReSharper disable once InconsistentNaming
public class ProjectRegistersAppService_Tests : ProjectManagementAppServiceTestBase
{
    private ProjectRegistersAppService CreateAppService()
    {
        return CreateService(() => new ProjectRegistersAppService(FakeRepository.For<ProjectRegister>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ProjectRegister()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditProjectRegisterDto
        {
            EmployeeId = employeeId,
            Period = new DateTime(2026, 1, 1)
        });

        var result = await service.GetAll(new GetAllProjectRegistersInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
    }
}
