using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class WageTypeExceptionsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private WageTypeExceptionsAppService CreateAppService()
    {
        return CreateService(() => new WageTypeExceptionsAppService(FakeRepository.For<WageTypeException>(Context)));
    }

    private int CreateWageType(string code)
    {
        var entity = new WageType
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            PayrollOrder = 1
        };
        Context.WageTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_WageTypeException_With_Distinct_Ids()
    {
        var wageTypeId = CreateWageType("WTE1");
        var exceptWageTypeId = CreateWageType("WTE2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWageTypeExceptionDto
        {
            WageTypeId = wageTypeId,
            ExceptWageTypeId = exceptWageTypeId
        });

        var result = await service.GetAll(new GetAllWageTypeExceptionsInput { WageTypeId = wageTypeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].WageTypeId.ShouldBe(wageTypeId);
        result.Items[0].ExceptWageTypeId.ShouldBe(exceptWageTypeId);
        result.Items[0].WageTypeId.ShouldNotBe(result.Items[0].ExceptWageTypeId);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Pair()
    {
        var wageTypeId = CreateWageType("WTE3");
        var exceptWageTypeId = CreateWageType("WTE4");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWageTypeExceptionDto
        {
            WageTypeId = wageTypeId,
            ExceptWageTypeId = exceptWageTypeId
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditWageTypeExceptionDto
            {
                WageTypeId = wageTypeId,
                ExceptWageTypeId = exceptWageTypeId
            }));
    }

    [Fact]
    public async Task Should_Filter_Exceptions_By_Excepted_Wage_Type()
    {
        var wageTypeId = CreateWageType("WTE5");
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditWageTypeExceptionDto { WageTypeId = wageTypeId, ExceptWageTypeId = CreateWageType("HOUSING") });
        await service.CreateOrEdit(new CreateOrEditWageTypeExceptionDto { WageTypeId = wageTypeId, ExceptWageTypeId = CreateWageType("TRANSPORT") });

        var result = await service.GetAll(new GetAllWageTypeExceptionsInput { WageTypeId = wageTypeId, Filter = "TRANS" });

        result.TotalCount.ShouldBe(1);
    }
}
