using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GeneralMasterInfo;

/// <summary>
/// Clearance request field details, as listed on a clearance request field's own page: the list narrows to
/// one field when its id is given, and still lists every field's details when it is not (the standalone screen).
/// </summary>
// ReSharper disable once InconsistentNaming
public class ClearanceRequestFieldDetailsAppService_Tests : AppTestBase
{
    private readonly IClearanceRequestFieldDetailsAppService _appService;

    public ClearanceRequestFieldDetailsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IClearanceRequestFieldDetailsAppService>();
    }

    private (int LaptopFieldId, int BadgeFieldId, int ItRoleId, int SecurityRoleId) Arrange()
    {
        var ids = (0, 0, 0, 0);
        UsingDbContext(context =>
        {
            var tenantId = AbpSession.TenantId ?? 1;
            var laptop = context.ClearanceRequestFields.Add(new ClearanceRequestField
            {
                TenantId = tenantId, Code = "CRF-LAP", NameEn = "Laptop returned", NameAr = "إعادة الحاسوب", IsActive = true
            }).Entity;
            var badge = context.ClearanceRequestFields.Add(new ClearanceRequestField
            {
                TenantId = tenantId, Code = "CRF-BDG", NameEn = "Badge handed in", NameAr = "تسليم البطاقة", IsActive = true
            }).Entity;
            var it = context.WorkflowRoles.Add(new WorkflowRole
            {
                TenantId = tenantId, Code = "WR-IT", NameEn = "IT", NameAr = "تقنية المعلومات"
            }).Entity;
            var security = context.WorkflowRoles.Add(new WorkflowRole
            {
                TenantId = tenantId, Code = "WR-SEC", NameEn = "Security", NameAr = "الأمن"
            }).Entity;
            context.SaveChanges();
            ids = (laptop.Id, badge.Id, it.Id, security.Id);
        });
        return ids;
    }

    [Fact]
    public async Task Should_List_Only_The_Details_Of_The_Given_Field()
    {
        var (laptopId, badgeId, itRoleId, securityRoleId) = Arrange();
        await _appService.CreateOrEdit(new CreateOrEditClearanceRequestFieldDetailDto { ClearanceRequestFieldId = laptopId, WorkflowRoleId = itRoleId });
        await _appService.CreateOrEdit(new CreateOrEditClearanceRequestFieldDetailDto { ClearanceRequestFieldId = badgeId, WorkflowRoleId = securityRoleId });

        var result = await _appService.GetAll(new GetAllClearanceRequestFieldDetailsInput { ClearanceRequestFieldId = laptopId, MaxResultCount = 10 });

        result.TotalCount.ShouldBe(1);
        var row = result.Items.Single();
        row.ClearanceRequestFieldId.ShouldBe(laptopId);
        row.WorkflowRoleName.ShouldBe("IT");
    }

    [Fact]
    public async Task Should_List_Every_Fields_Details_When_No_Field_Is_Given()
    {
        var (laptopId, badgeId, itRoleId, securityRoleId) = Arrange();
        await _appService.CreateOrEdit(new CreateOrEditClearanceRequestFieldDetailDto { ClearanceRequestFieldId = laptopId, WorkflowRoleId = itRoleId });
        await _appService.CreateOrEdit(new CreateOrEditClearanceRequestFieldDetailDto { ClearanceRequestFieldId = badgeId, WorkflowRoleId = securityRoleId });

        var result = await _appService.GetAll(new GetAllClearanceRequestFieldDetailsInput { MaxResultCount = 1000 });

        var fieldIds = result.Items.Select(i => i.ClearanceRequestFieldId).ToList();
        fieldIds.ShouldContain((int?)laptopId);
        fieldIds.ShouldContain((int?)badgeId);
    }
}
