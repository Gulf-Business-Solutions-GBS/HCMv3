using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class AbsenceTypeExceptionsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private AbsenceTypeExceptionsAppService CreateAppService()
    {
        return CreateService(() => new AbsenceTypeExceptionsAppService(FakeRepository.For<AbsenceTypeException>(Context)));
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
    public async Task Should_Create_AbsenceTypeException()
    {
        var absenceTypeId = CreateAbsenceType("AT1");
        var exceptAbsenceTypeId = CreateAbsenceType("AT2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsenceTypeExceptionDto
        {
            AbsenceTypeId = absenceTypeId,
            ExceptAbsenceTypeId = exceptAbsenceTypeId
        });

        var result = await service.GetAll(new GetAllAbsenceTypeExceptionsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].AbsenceTypeId.ShouldBe(absenceTypeId);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Pair()
    {
        var absenceTypeId = CreateAbsenceType("AT3");
        var exceptAbsenceTypeId = CreateAbsenceType("AT4");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsenceTypeExceptionDto
        {
            AbsenceTypeId = absenceTypeId,
            ExceptAbsenceTypeId = exceptAbsenceTypeId
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAbsenceTypeExceptionDto
            {
                AbsenceTypeId = absenceTypeId,
                ExceptAbsenceTypeId = exceptAbsenceTypeId
            }));
    }

    [Fact]
    public async Task Should_Throw_When_AbsenceType_Excepts_Itself()
    {
        var absenceTypeId = CreateAbsenceType("AT5");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAbsenceTypeExceptionDto
            {
                AbsenceTypeId = absenceTypeId,
                ExceptAbsenceTypeId = absenceTypeId
            }));
    }
}
