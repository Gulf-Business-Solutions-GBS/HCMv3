using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GeneralMasterInfo;

/// <summary>
/// Include in Workflow decides which clearance types employees can raise in ESS. The types screen reads it with
/// <see cref="IClearanceRequestTypesAppService.GetIdsIncludedInWorkflow"/> and sets it one row at a time.
/// </summary>
// ReSharper disable once InconsistentNaming
public class ClearanceRequestTypesAppService_Tests : AppTestBase
{
    private readonly IClearanceRequestTypesAppService _appService;

    public ClearanceRequestTypesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IClearanceRequestTypesAppService>();
    }

    private int AddType(string code, bool includeInWorkflow)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var entity = context.ClearanceRequestTypes.Add(new ClearanceRequestType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = "Type " + code,
                NameAr = "نوع " + code,
                IncludeInWorkflow = includeInWorkflow
            }).Entity;
            context.SaveChanges();
            id = entity.Id;
        });
        return id;
    }

    [Fact]
    public async Task Should_Return_Only_The_Types_Included_In_Workflow()
    {
        var included = AddType("1", includeInWorkflow: true);
        var excluded = AddType("2", includeInWorkflow: false);

        var ids = (await _appService.GetIdsIncludedInWorkflow()).Items;

        ids.ShouldContain(included);
        ids.ShouldNotContain(excluded);
    }

    [Fact]
    public async Task Ticking_A_Type_Should_Add_It_To_The_Included_List()
    {
        var id = AddType("1", includeInWorkflow: false);

        await _appService.SetIncludeInWorkflow(new SetClearanceRequestTypeIncludeInWorkflowInput
        {
            Id = id,
            IncludeInWorkflow = true
        });

        (await _appService.GetIdsIncludedInWorkflow()).Items.ShouldContain(id);
    }

    [Fact]
    public async Task Unticking_A_Type_Should_Remove_It_From_The_Included_List()
    {
        var id = AddType("1", includeInWorkflow: true);

        await _appService.SetIncludeInWorkflow(new SetClearanceRequestTypeIncludeInWorkflowInput
        {
            Id = id,
            IncludeInWorkflow = false
        });

        (await _appService.GetIdsIncludedInWorkflow()).Items.ShouldNotContain(id);
    }
}
