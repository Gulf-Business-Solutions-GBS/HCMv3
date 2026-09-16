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
        return CreateService(() => new WageTypeLeavesAppService(
            FakeRepository.For<WageTypeLeave>(Context),
            FakeRepository.For<HCMv3.PayrollManagement.AbsenceType>(Context)));
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

    [Fact]
    public async Task Should_Filter_Leaves_By_Absence_Type_Name()
    {
        var wageTypeId = CreateWageType("WTL3");
        var service = CreateAppService();
        foreach (var code in new[] { "SICK", "ANNUAL" })
        {
            await service.CreateOrEdit(new CreateOrEditWageTypeLeaveDto { WageTypeId = wageTypeId, AbsenceTypeId = CreateAbsenceType(code) });
        }

        var result = await service.GetAll(new GetAllWageTypeLeavesInput { WageTypeId = wageTypeId, Filter = "ANN" });

        result.TotalCount.ShouldBe(1);
        result.Items[0].AbsenceTypeName.ShouldBe("ANNUAL");
    }

    [Fact]
    public async Task Should_Add_Every_Leave_Type_Not_Yet_Linked()
    {
        var wageTypeId = CreateWageType("WTL4");
        var linked = CreateAbsenceType("ATL4A");
        CreateAbsenceType("ATL4B");
        CreateAbsenceType("ATL4C");
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditWageTypeLeaveDto { WageTypeId = wageTypeId, AbsenceTypeId = linked });

        var added = await service.AddAllLeaveTypes(new Abp.Application.Services.Dto.EntityDto(wageTypeId));

        added.ShouldBe(2);
        (await service.GetAll(new GetAllWageTypeLeavesInput { WageTypeId = wageTypeId })).TotalCount.ShouldBe(3);
        (await service.AddAllLeaveTypes(new Abp.Application.Services.Dto.EntityDto(wageTypeId))).ShouldBe(0);
    }
}
