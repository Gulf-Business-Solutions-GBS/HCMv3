using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class WageTypeLeavesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private WageTypeLeavesAppService CreateAppService()
    {
        return CreateService(() => new WageTypeLeavesAppService(FakeRepository.For<WageTypeLeave>(Context)));
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

    private int CreateAbsenceType(string code)
    {
        var entity = new HCMv3.PayrollManagement.AbsenceType
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.AbsenceTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_WageTypeLeave()
    {
        var wageTypeId = CreateWageType("WTL1");
        var absenceTypeId = CreateAbsenceType("ATL1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWageTypeLeaveDto
        {
            WageTypeId = wageTypeId,
            AbsenceTypeId = absenceTypeId
        });

        var result = await service.GetAll(new GetAllWageTypeLeavesInput { WageTypeId = wageTypeId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Pair()
    {
        var wageTypeId = CreateWageType("WTL2");
        var absenceTypeId = CreateAbsenceType("ATL2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWageTypeLeaveDto
        {
            WageTypeId = wageTypeId,
            AbsenceTypeId = absenceTypeId
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditWageTypeLeaveDto
            {
                WageTypeId = wageTypeId,
                AbsenceTypeId = absenceTypeId
            }));
    }
}
