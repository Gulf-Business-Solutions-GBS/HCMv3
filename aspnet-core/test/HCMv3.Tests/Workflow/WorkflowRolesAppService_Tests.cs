using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
public class WorkflowRolesAppService_Tests : WorkflowAppServiceTestBase
{
    private WorkflowRolesAppService CreateAppService()
    {
        return CreateService(() => new WorkflowRolesAppService(FakeRepository.For<WorkflowRole>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_WorkflowRole()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkflowRoleDto
        {
            NameEn = "HR Manager",
            NameAr = "مدير الموارد البشرية",
            WorkflowRoleGroup = WorkflowRoleGroup.Department
        });

        var result = await service.GetAll(new GetAllWorkflowRolesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("HR Manager");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkflowRoleDto
        {
            NameEn = "HR Manager",
            NameAr = "A",
            WorkflowRoleGroup = WorkflowRoleGroup.Department
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditWorkflowRoleDto
            {
                NameEn = "HR Manager",
                NameAr = "B",
                WorkflowRoleGroup = WorkflowRoleGroup.Company
            }));
    }
}
