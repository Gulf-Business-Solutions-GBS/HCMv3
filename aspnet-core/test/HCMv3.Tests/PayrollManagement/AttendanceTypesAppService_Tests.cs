using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class AttendanceTypesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private AttendanceTypesAppService CreateAppService()
    {
        return CreateService(() => new AttendanceTypesAppService(FakeRepository.For<AttendanceType>(Context)));
    }

    private int CreateAbsenceType(string code)
    {
        var entity = new AbsenceType
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
    public async Task Should_Create_AttendanceType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAttendanceTypeDto
        {
            Code = "ATT1",
            NameEn = "Overtime",
            NameAr = "عمل إضافي"
        });

        var result = await service.GetAll(new GetAllAttendanceTypesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Overtime");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAttendanceTypeDto
        {
            Code = "ATT2",
            NameEn = "Field Work",
            NameAr = "عمل ميداني"
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAttendanceTypeDto
            {
                Code = "ATT2",
                NameEn = "Remote Work",
                NameAr = "عمل عن بعد"
            }));
    }

    [Fact]
    public async Task Should_Throw_When_HaveQuotaAccrual_And_LinkLeaveQuota_Missing()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAttendanceTypeDto
            {
                Code = "ATT3",
                NameEn = "Accrual Type",
                NameAr = "نوع الاستحقاق",
                HaveQuotaAccrual = true,
                LinkLeaveQuotaId = null
            }));
    }

    [Fact]
    public async Task Should_Create_When_HaveQuotaAccrual_And_LinkLeaveQuota_Provided()
    {
        var absenceTypeId = CreateAbsenceType("AT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAttendanceTypeDto
        {
            Code = "ATT4",
            NameEn = "Accrual Type 2",
            NameAr = "نوع الاستحقاق 2",
            HaveQuotaAccrual = true,
            LinkLeaveQuotaId = absenceTypeId
        });

        var result = await service.GetAll(new GetAllAttendanceTypesInput());
        result.TotalCount.ShouldBe(1);
    }
}
