using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class AbsencePaymentsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private AbsencePaymentsAppService CreateAppService()
    {
        return CreateService(() => new AbsencePaymentsAppService(FakeRepository.For<AbsencePayment>(Context)));
    }

    private int CreateAbsenceType(string code)
    {
        var entity = new AbsenceType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.AbsenceTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateWageType(string code)
    {
        var entity = new WageType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code, PayrollOrder = 1 };
        Context.WageTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_AbsencePayment()
    {
        var absenceTypeId = CreateAbsenceType("AP_AT1");
        var wageTypeId = CreateWageType("AP_WT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsencePaymentDto
        {
            AbsenceTypeId = absenceTypeId,
            WageTypeId = wageTypeId
        });

        var result = await service.GetAll(new GetAllAbsencePaymentsInput { AbsenceTypeId = absenceTypeId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_AbsenceType_WageType_Pair()
    {
        var absenceTypeId = CreateAbsenceType("AP_AT2");
        var wageTypeId = CreateWageType("AP_WT2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsencePaymentDto
        {
            AbsenceTypeId = absenceTypeId,
            WageTypeId = wageTypeId
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAbsencePaymentDto
            {
                AbsenceTypeId = absenceTypeId,
                WageTypeId = wageTypeId
            }));
    }

    [Fact]
    public async Task Should_Filter_Payments_By_Wage_Type()
    {
        var absenceTypeId = CreateAbsenceType("AP_AT3");
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditAbsencePaymentDto { AbsenceTypeId = absenceTypeId, WageTypeId = CreateWageType("LEAVEPAY3") });
        await service.CreateOrEdit(new CreateOrEditAbsencePaymentDto { AbsenceTypeId = absenceTypeId, WageTypeId = CreateWageType("TICKET3") });

        var result = await service.GetAll(new GetAllAbsencePaymentsInput { AbsenceTypeId = absenceTypeId, Filter = "TICK" });

        result.TotalCount.ShouldBe(1);
    }
}
