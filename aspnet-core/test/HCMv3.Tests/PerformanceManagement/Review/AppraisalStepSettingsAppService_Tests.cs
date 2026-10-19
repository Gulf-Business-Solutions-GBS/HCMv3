using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PerformanceManagement.Review;
using HCMv3.PerformanceManagement.Review.Dto;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement.Review;

/// <summary>The "Appraisal WF Setup" screen (legacy <c>PARequest</c> and its validation).</summary>
// ReSharper disable once InconsistentNaming
public class AppraisalStepSettingsAppService_Tests : AppraisalReviewTestBase
{
    private AppraisalStepSettingsAppService CreateAppService()
    {
        return WireApp(new AppraisalStepSettingsAppService(
            ReviewFakeRepository.For<AppraisalStepSetting>(Context),
            ReviewFakeRepository.For<AppraisalStepSettingRelationship>(Context),
            ReviewFakeRepository.For<RequestMaster>(Context),
            ReviewFakeRepository.For<RequestDetail>(Context),
            ReviewFakeRepository.For<RelationshipType>(Context)));
    }

    private static CreateOrEditAppraisalStepSettingDto Row(int masterId, int step, params int[] relationshipTypeIds) => new()
    {
        RequestMasterId = masterId,
        Step = step,
        CanAddRatings = true,
        AutoAddRelationShip = relationshipTypeIds.Length > 0,
        AllowedPeerCount = 2,
        RelationshipTypeIds = relationshipTypeIds.ToList()
    };

    [Fact]
    public async Task Should_Create_And_List_A_Step_With_Its_Relationship_Types()
    {
        var master = CreateRequestMaster(1, 2);
        var dottedLine = CreateRelationshipType("Dotted Line");
        var service = CreateAppService();

        await service.CreateOrEdit(Row(master.Id, 2, dottedLine));

        var list = await service.GetAll(new GetAllAppraisalStepSettingsInput { MaxResultCount = 10 });
        list.TotalCount.ShouldBe(1);
        list.Items[0].Step.ShouldBe(2);
        list.Items[0].CanAddRatings.ShouldBeTrue();
        list.Items[0].AllowedPeerCount.ShouldBe(2);
        list.Items[0].RelationshipTypeNames.ShouldBe("Dotted Line");
    }

    [Fact]
    public async Task Every_Flag_Should_Default_To_Off()
    {
        var master = CreateRequestMaster(1);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAppraisalStepSettingDto { RequestMasterId = master.Id, Step = 1 });

        var row = Context.AppraisalStepSettings.Single();
        row.CanEmployeeSelfRate.ShouldBeFalse();
        row.CanAddRatings.ShouldBeFalse();
        row.AutoAddManager.ShouldBeFalse();
        row.AutoAddRelationShip.ShouldBeFalse();
        row.AutoSubmitOnLastFill.ShouldBeFalse();
        row.AllowedPeerCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_Step_The_Request_Master_Does_Not_Define_Should_Be_Refused()
    {
        // Legacy: "Request : X has not defined the step : 3".
        var master = CreateRequestMaster(1, 2);

        await Should.ThrowAsync<UserFriendlyException>(() => CreateAppService().CreateOrEdit(Row(master.Id, 3)));
    }

    [Fact]
    public async Task A_Step_Of_Zero_Should_Be_Refused()
    {
        var master = CreateRequestMaster(1);

        await Should.ThrowAsync<UserFriendlyException>(() => CreateAppService().CreateOrEdit(Row(master.Id, 0)));
    }

    [Fact]
    public async Task A_Second_Row_For_The_Same_Step_Should_Be_Refused()
    {
        var master = CreateRequestMaster(1, 2);
        var service = CreateAppService();
        await service.CreateOrEdit(Row(master.Id, 2));

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(Row(master.Id, 2)));
    }

    [Fact]
    public async Task A_Relationship_Type_Twice_On_One_Step_Should_Be_Refused()
    {
        // Legacy PARequestRelationship: "RelationshipType : X is already assigned Step : 2".
        var master = CreateRequestMaster(1, 2);
        var type = CreateRelationshipType("Mentor");

        await Should.ThrowAsync<UserFriendlyException>(() => CreateAppService().CreateOrEdit(Row(master.Id, 2, type, type)));
    }

    [Fact]
    public async Task Editing_Should_Replace_The_Relationship_Types()
    {
        var master = CreateRequestMaster(1, 2);
        var mentor = CreateRelationshipType("Mentor");
        var dottedLine = CreateRelationshipType("Dotted Line");
        var service = CreateAppService();
        await service.CreateOrEdit(Row(master.Id, 2, mentor));
        var id = Context.AppraisalStepSettings.Single().Id;

        var edit = Row(master.Id, 2, dottedLine);
        edit.Id = id;
        await service.CreateOrEdit(edit);

        var forEdit = await service.GetAppraisalStepSettingForEdit(new EntityDto(id));
        forEdit.AppraisalStepSetting.RelationshipTypeIds.ShouldBe(new List<int> { dottedLine });
    }

    [Fact]
    public async Task Deleting_Should_Remove_Its_Relationship_Types()
    {
        var master = CreateRequestMaster(1, 2);
        var service = CreateAppService();
        await service.CreateOrEdit(Row(master.Id, 2, CreateRelationshipType("Mentor")));

        await service.Delete(new EntityDto(Context.AppraisalStepSettings.Single().Id));

        Context.AppraisalStepSettings.Count().ShouldBe(0);
        Context.AppraisalStepSettingRelationships.Count().ShouldBe(0);
    }

    [Fact]
    public async Task The_Lookups_Should_List_Each_Masters_Steps()
    {
        var master = CreateRequestMaster(1, 2, 3);
        CreateRelationshipType("Mentor");

        var lookups = await CreateAppService().GetLookups();

        lookups.RequestMasters.Single(m => m.Id == master.Id).Steps.ShouldBe(new List<int> { 1, 2, 3 });
        lookups.RelationshipTypes.Count.ShouldBe(1);
    }
}
