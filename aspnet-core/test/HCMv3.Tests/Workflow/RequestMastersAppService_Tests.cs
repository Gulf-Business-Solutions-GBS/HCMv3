using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
public class RequestMastersAppService_Tests : WorkflowAppServiceTestBase
{
    private RequestMastersAppService CreateAppService()
    {
        return CreateService(() => new RequestMastersAppService(
            FakeRepository.For<RequestMaster>(Context),
            FakeRepository.For<RequestDetail>(Context),
            FakeRepository.For<RequestDetailParallel>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_RequestMaster()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRequestMasterDto
        {
            NameEn = "Leave Request Workflow",
            NameAr = "سير عمل طلب الإجازة",
            SecurityLevel = WorkflowSecurityLevel.EmployeeManager,
            DueInDays = 5
        });

        var result = await service.GetAll(new GetAllRequestMastersInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Leave Request Workflow");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRequestMasterDto
        {
            NameEn = "Leave Request Workflow",
            NameAr = "A",
            SecurityLevel = WorkflowSecurityLevel.Everyone
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditRequestMasterDto
            {
                NameEn = "Leave Request Workflow",
                NameAr = "B",
                SecurityLevel = WorkflowSecurityLevel.Everyone
            }));
    }

    private RequestMaster SeedChain(string code = "LEAVE-NEW")
    {
        var tenantId = AbpSession.TenantId ?? 1;
        var master = new RequestMaster
        {
            TenantId = tenantId,
            Code = code,
            NameEn = "Leave New",
            NameAr = "إجازة",
            WorkflowSteps = 2,
            AutoClose = true,
            AutoCloseInDays = 7,
            IsCommentRequire = true,
            DueInDays = 3
        };
        Context.RequestMasters.Add(master);
        Context.SaveChanges();

        Context.RequestDetails.Add(new RequestDetail
        {
            TenantId = tenantId, RequestMasterId = master.Id, Step = 1,
            ApproverPerson = WorkflowApproverPersonType.None, Person = WorkflowPersonScope.Themselves
        });
        var approverStep = new RequestDetail
        {
            TenantId = tenantId, RequestMasterId = master.Id, Step = 2,
            ApproverPerson = WorkflowApproverPersonType.PersonManager, ApprovalRequire = ApprovalRequire.Parallel,
            CanReject = true, CanSendBack = true, ShowPayment = true, ReminderDays = 1, EscalateAfterDays = 3,
            SignatoryStep = true
        };
        Context.RequestDetails.Add(approverStep);
        Context.SaveChanges();

        var role = new WorkflowRole { TenantId = tenantId, Code = "HR", NameEn = "HR", NameAr = "HR" };
        Context.WorkflowRoles.Add(role);
        Context.SaveChanges();

        Context.RequestDetailParallels.Add(new RequestDetailParallel
        {
            TenantId = tenantId, RequestDetailId = approverStep.Id, WorkflowRoleId = role.Id
        });
        Context.SaveChanges();

        return master;
    }

    [Fact]
    public async Task Should_Copy_The_Chain_With_All_Its_Steps_And_Settings()
    {
        var source = SeedChain();
        var service = CreateAppService();

        var created = await service.Copy(new EntityDto(source.Id));

        var copy = Context.RequestMasters.Single(m => m.Id == created.Id);
        copy.Code.ShouldBe("LEAVE-NEW-COPY");
        copy.NameEn.ShouldBe("Leave New (Copy)");
        copy.WorkflowSteps.ShouldBe(2);
        copy.AutoClose.ShouldBeTrue();
        copy.AutoCloseInDays.ShouldBe(7);
        copy.IsCommentRequire.ShouldBeTrue();
        copy.DueInDays.ShouldBe(3);

        var steps = Context.RequestDetails.Where(d => d.RequestMasterId == copy.Id).OrderBy(d => d.Step).ToList();
        steps.Count.ShouldBe(2);
        steps[0].ApproverPerson.ShouldBe(WorkflowApproverPersonType.None);
        steps[0].Person.ShouldBe(WorkflowPersonScope.Themselves);
        steps[1].ApproverPerson.ShouldBe(WorkflowApproverPersonType.PersonManager);
        steps[1].ApprovalRequire.ShouldBe(ApprovalRequire.Parallel);
        steps[1].CanReject.ShouldBeTrue();
        steps[1].CanSendBack.ShouldBeTrue();
        steps[1].ShowPayment.ShouldBeTrue();
        steps[1].ReminderDays.ShouldBe(1);
        steps[1].EscalateAfterDays.ShouldBe(3);
        steps[1].SignatoryStep.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Copy_Each_Steps_Parallel_Approvers()
    {
        var source = SeedChain();
        var service = CreateAppService();

        var created = await service.Copy(new EntityDto(source.Id));

        var copiedStepIds = Context.RequestDetails.Where(d => d.RequestMasterId == created.Id).Select(d => d.Id).ToList();
        Context.RequestDetailParallels.Count(p => copiedStepIds.Contains(p.RequestDetailId)).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Leave_The_Source_Chain_Untouched()
    {
        var source = SeedChain();
        var service = CreateAppService();

        await service.Copy(new EntityDto(source.Id));

        Context.RequestDetails.Count(d => d.RequestMasterId == source.Id).ShouldBe(2);
        Context.RequestMasters.Single(m => m.Id == source.Id).Code.ShouldBe("LEAVE-NEW");
    }

    [Fact]
    public async Task Should_Number_A_Second_Copy_So_Code_And_Names_Stay_Unique()
    {
        var source = SeedChain();
        var service = CreateAppService();

        await service.Copy(new EntityDto(source.Id));
        var second = await service.Copy(new EntityDto(source.Id));

        var copy = Context.RequestMasters.Single(m => m.Id == second.Id);
        copy.Code.ShouldBe("LEAVE-NEW-COPY2");
        copy.NameEn.ShouldBe("Leave New (Copy)2");
    }
}
