using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.OrganizationManagement;
using HCMv3.OrganizationManagement.Dto;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.OrganizationManagement;

/// <summary>
/// Legacy <c>JobTitleDetail</c>: the default workflow roles of a job title. A role is required, and the same
/// role is not listed twice on one job title.
/// </summary>
// ReSharper disable once InconsistentNaming
public class JobTitleDetailsAppService_Tests : AppTestBase
{
    private readonly IJobTitleDetailsAppService _appService;

    public JobTitleDetailsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IJobTitleDetailsAppService>();
    }

    private (int JobTitleId, int RoleId) Arrange()
    {
        var ids = (0, 0);
        UsingDbContext(context =>
        {
            var tenantId = AbpSession.TenantId ?? 1;
            var jobTitle = context.JobTitles.Add(new JobTitle
            {
                TenantId = tenantId, Code = "JT-HRM", NameEn = "HR Manager", NameAr = "مدير الموارد البشرية"
            }).Entity;
            var role = context.WorkflowRoles.Add(new WorkflowRole
            {
                TenantId = tenantId, Code = "WR-HR", NameEn = "HR Approver", NameAr = "معتمد الموارد البشرية"
            }).Entity;
            context.SaveChanges();
            ids = (jobTitle.Id, role.Id);
        });
        return ids;
    }

    [Fact]
    public async Task Should_Save_And_List_A_Default_Role_For_A_Job_Title()
    {
        var (jobTitleId, roleId) = Arrange();

        await _appService.CreateOrEdit(new CreateOrEditJobTitleDetailDto { JobTitleId = jobTitleId, WorkflowRoleId = roleId });

        var row = (await _appService.GetAll(new GetAllJobTitleDetailsInput { JobTitleId = jobTitleId, MaxResultCount = 10 }))
            .Items.Single();
        row.JobTitleName.ShouldBe("HR Manager");
        row.WorkflowRoleName.ShouldBe("HR Approver");
    }

    [Fact]
    public async Task Should_Refuse_A_Row_Without_A_Role()
    {
        var (jobTitleId, _) = Arrange();

        await Should.ThrowAsync<UserFriendlyException>(
            () => _appService.CreateOrEdit(new CreateOrEditJobTitleDetailDto { JobTitleId = jobTitleId }));
    }

    [Fact]
    public async Task Should_Refuse_The_Same_Role_Twice_On_One_Job_Title()
    {
        var (jobTitleId, roleId) = Arrange();
        await _appService.CreateOrEdit(new CreateOrEditJobTitleDetailDto { JobTitleId = jobTitleId, WorkflowRoleId = roleId });

        await Should.ThrowAsync<UserFriendlyException>(
            () => _appService.CreateOrEdit(new CreateOrEditJobTitleDetailDto { JobTitleId = jobTitleId, WorkflowRoleId = roleId }));
    }
}
