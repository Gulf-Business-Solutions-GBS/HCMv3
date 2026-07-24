using System;
using System.Threading.Tasks;
using HCMv3.ProjectManagement;
using HCMv3.ProjectManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ProjectManagement;

// ReSharper disable once InconsistentNaming
public class ProjectLogsAppService_Tests : ProjectManagementAppServiceTestBase
{
    private ProjectLogsAppService CreateAppService()
    {
        return CreateService(() => new ProjectLogsAppService(FakeRepository.For<ProjectLog>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ProjectLog()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditProjectLogDto
        {
            Message = "Project costing run started",
            RunTimeStamp = new DateTime(2026, 1, 1, 8, 0, 0),
            EmployeeId = employeeId,
            Status = 1,
            InPeriod = new DateTime(2026, 1, 1),
            StopFlag = false,
            NoOfEmployee = 10
        });

        var result = await service.GetAll(new GetAllProjectLogsInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Message.ShouldBe("Project costing run started");
    }
}
