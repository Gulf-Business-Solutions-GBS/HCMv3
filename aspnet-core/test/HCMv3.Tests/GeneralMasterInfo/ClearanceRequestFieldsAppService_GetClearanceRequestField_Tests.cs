using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Entities;
using HCMv3.GeneralMasterInfo;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GeneralMasterInfo;

/// <summary>The clearance request field's own page reads the field, with its type's name, by id.</summary>
// ReSharper disable once InconsistentNaming
public class ClearanceRequestFieldsAppService_GetClearanceRequestField_Tests : AppTestBase
{
    private readonly IClearanceRequestFieldsAppService _appService;

    public ClearanceRequestFieldsAppService_GetClearanceRequestField_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IClearanceRequestFieldsAppService>();
    }

    [Fact]
    public async Task Should_Return_The_Field_With_Its_Type_Name()
    {
        var fieldId = 0;
        UsingDbContext(context =>
        {
            var tenantId = AbpSession.TenantId ?? 1;
            var type = context.Set<ClearanceRequestType>().Add(new ClearanceRequestType
            {
                TenantId = tenantId, Code = "CRT-EXIT", NameEn = "Exit clearance", NameAr = "إخلاء طرف"
            }).Entity;
            context.SaveChanges();
            var field = context.ClearanceRequestFields.Add(new ClearanceRequestField
            {
                TenantId = tenantId, Code = "CRF-LAP", NameEn = "Laptop returned", NameAr = "إعادة الحاسوب",
                ClearanceRequestTypeId = type.Id, IsActive = true
            }).Entity;
            context.SaveChanges();
            fieldId = field.Id;
        });

        var result = await _appService.GetClearanceRequestField(new EntityDto(fieldId));

        result.Id.ShouldBe(fieldId);
        result.Code.ShouldBe("CRF-LAP");
        result.ClearanceRequestTypeName.ShouldBe("Exit clearance");
        result.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Throw_When_The_Field_Does_Not_Exist()
    {
        await Should.ThrowAsync<EntityNotFoundException>(() => _appService.GetClearanceRequestField(new EntityDto(999999)));
    }
}
