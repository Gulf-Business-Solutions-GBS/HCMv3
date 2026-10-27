using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Localization;
using Abp.UI;
using HCMv3.Administration.Tools;
using HCMv3.Administration.Tools.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

// The list-backed IRepository fakes live next to the ProjectCostEngine tests; reused here rather than
// duplicated. See ProjectCostEngineFakes for why a plain List<T>.AsQueryable() is a complete stand-in.
using Fakes = HCMv3.Tests.ProjectManagement.ProjectCostEngineFakes;

namespace HCMv3.Tests.Administration;

/// <summary>
/// Unit tests for <see cref="WorkflowAdminToolsAppService.CopyWorkflowSetup"/> -- the port of legacy
/// <c>HCM10.Web/UserControls/uscCopyWorkflowSetup.ascx.cs</c>.
/// </summary>
public class CopyWorkflowSetup_Tests
{
    private readonly List<RequestMaster> _requestMasters = new List<RequestMaster>();
    private readonly List<RequestDetail> _requestDetails = new List<RequestDetail>();
    private readonly List<WorkFlowApprover> _approvers = new List<WorkFlowApprover>();
    private readonly List<RequestLog> _requestLogs = new List<RequestLog>();
    private readonly List<Employee> _employees = new List<Employee>();

    private WorkflowAdminToolsAppService CreateService()
    {
        var service = new WorkflowAdminToolsAppService(
            Fakes.RepoOver(_approvers),
            Fakes.RepoOver(_requestLogs),
            Fakes.RepoOver(_employees),
            Fakes.RepoOver(_requestMasters),
            Fakes.RepoOver(_requestDetails));

        ((ApplicationService)service).AbpSession = Fakes.Session();
        ((ApplicationService)service).LocalizationManager = Fakes.LocalizationManager();
        return service;
    }

    private void SeedSource(int stepCount = 2)
    {
        _requestMasters.Add(new RequestMaster
        {
            Id = 1,
            TenantId = 1,
            Code = "SRC",
            NameEn = "Source",
            NameAr = "Source",
            WorkflowSteps = 3,
            AutoClose = true,
            AutoCloseInDays = 7,
            SecurityLevel = WorkflowSecurityLevel.EmployeeManager,
            // Deliberately set: legacy does NOT copy these three, and the test asserts that.
            DueInDays = 9,
            IsCommentRequire = true
        });

        for (var i = 1; i <= stepCount; i++)
        {
            _requestDetails.Add(new RequestDetail
            {
                Id = i,
                TenantId = 1,
                RequestMasterId = 1,
                Step = i,
                EnableReadLog = true,
                ShouldEscalate = true,
                EscalateAfterDays = 4,
                // Deliberately set: legacy's copy list omits these, and the test asserts they stay default.
                CanReject = true,
                ReminderDays = 5
            });
        }
    }

    private static CopyWorkflowSetupInput ValidInput()
    {
        return new CopyWorkflowSetupInput
        {
            SourceRequestMasterId = 1,
            NewRequestCode = "NEW",
            NewRequestNameEn = "New Request"
        };
    }

    [Fact]
    public async Task Should_Clone_Master_And_Every_Detail_Row()
    {
        SeedSource(3);
        var service = CreateService();

        var result = await service.CopyWorkflowSetup(ValidInput());

        result.AffectedCount.ShouldBe(3);

        var clone = _requestMasters.Single(r => r.Code == "NEW");
        clone.NameEn.ShouldBe("New Request");
        clone.WorkflowSteps.ShouldBe(3);
        clone.AutoClose.ShouldBeTrue();
        clone.AutoCloseInDays.ShouldBe(7);
        clone.SecurityLevel.ShouldBe(WorkflowSecurityLevel.EmployeeManager);

        _requestDetails.Count(r => r.RequestMasterId == clone.Id).ShouldBe(3);
    }

    /// <summary>
    /// Legacy lines 130-133 copy only four master fields. Widening that would silently change cloned
    /// workflows, so the omission is asserted rather than merely commented.
    /// </summary>
    [Fact]
    public async Task Should_Not_Copy_Master_Fields_Legacy_Leaves_Out()
    {
        SeedSource();
        var service = CreateService();

        await service.CopyWorkflowSetup(ValidInput());

        var clone = _requestMasters.Single(r => r.Code == "NEW");
        clone.DueInDays.ShouldBe(0);
        clone.IsCommentRequire.ShouldBeFalse();
        clone.DefaultApproverId.ShouldBeNull();
    }

    /// <summary>Legacy lines 143-157 copy twelve detail fields; everything else stays at its default.</summary>
    [Fact]
    public async Task Should_Copy_Only_The_Detail_Fields_Legacy_Copies()
    {
        SeedSource(1);
        var service = CreateService();

        await service.CopyWorkflowSetup(ValidInput());

        var clone = _requestMasters.Single(r => r.Code == "NEW");
        var clonedDetail = _requestDetails.Single(r => r.RequestMasterId == clone.Id);

        clonedDetail.Step.ShouldBe(1);
        clonedDetail.EnableReadLog.ShouldBeTrue();
        clonedDetail.ShouldEscalate.ShouldBeTrue();
        clonedDetail.EscalateAfterDays.ShouldBe(4);

        clonedDetail.CanReject.ShouldBeFalse();
        clonedDetail.ReminderDays.ShouldBe(0);
    }

    /// <summary>
    /// This port's own addition -- <c>NameAr</c> is [Required] here but legacy's form had no Arabic field.
    /// </summary>
    [Fact]
    public async Task Should_Fall_Back_To_English_Name_For_Arabic_Name()
    {
        SeedSource();
        var service = CreateService();

        await service.CopyWorkflowSetup(ValidInput());

        _requestMasters.Single(r => r.Code == "NEW").NameAr.ShouldBe("New Request");
    }

    [Fact]
    public async Task Should_Trim_The_New_Request_Code()
    {
        SeedSource();
        var service = CreateService();
        var input = ValidInput();
        input.NewRequestCode = "  NEW  ";

        await service.CopyWorkflowSetup(input);

        _requestMasters.Any(r => r.Code == "NEW").ShouldBeTrue();
    }

    /// <summary>Legacy lines 113-114 / 177.</summary>
    [Fact]
    public async Task Should_Reject_A_Duplicate_Request_Code()
    {
        SeedSource();
        _requestMasters.Add(new RequestMaster { Id = 2, TenantId = 1, Code = "NEW", NameEn = "X", NameAr = "X" });
        var service = CreateService();

        await Assert.ThrowsAsync<UserFriendlyException>(() => service.CopyWorkflowSetup(ValidInput()));
    }

    /// <summary>Legacy lines 119-120 / 167: a source with no steps creates NOTHING at all.</summary>
    [Fact]
    public async Task Should_Reject_A_Source_With_No_Detail_Rows_And_Create_Nothing()
    {
        SeedSource(0);
        var service = CreateService();

        await Assert.ThrowsAsync<UserFriendlyException>(() => service.CopyWorkflowSetup(ValidInput()));

        _requestMasters.Count.ShouldBe(1);
    }

    /// <summary>Legacy lines 116-117 / 172.</summary>
    [Fact]
    public async Task Should_Reject_A_Missing_Source_Request()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<UserFriendlyException>(() => service.CopyWorkflowSetup(ValidInput()));
    }

    /// <summary>Legacy lines 99-107.</summary>
    [Theory]
    [InlineData("", "Name")]
    [InlineData("   ", "Name")]
    [InlineData("CODE", "")]
    public async Task Should_Reject_Blank_Code_Or_Name(string code, string name)
    {
        SeedSource();
        var service = CreateService();

        var input = ValidInput();
        input.NewRequestCode = code;
        input.NewRequestNameEn = name;

        await Assert.ThrowsAsync<UserFriendlyException>(() => service.CopyWorkflowSetup(input));
    }
}
