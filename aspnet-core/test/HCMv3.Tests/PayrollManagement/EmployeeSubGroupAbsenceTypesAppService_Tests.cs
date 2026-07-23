using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeSubGroupAbsenceTypesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private EmployeeSubGroupAbsenceTypesAppService CreateAppService()
    {
        return CreateService(() => new EmployeeSubGroupAbsenceTypesAppService(FakeRepository.For<EmployeeSubGroupAbsenceType>(Context)));
    }

    private int CreateEmployeeSubGroup(string code)
    {
        var group = new EmployeeGroup { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.EmployeeGroups.Add(group);
        Context.SaveChanges();

        var subGroup = new EmployeeSubGroup
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeGroupId = group.Id,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.EmployeeSubGroups.Add(subGroup);
        Context.SaveChanges();
        return subGroup.Id;
    }

    private int CreateAbsenceType(string code)
    {
        var entity = new AbsenceType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.AbsenceTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_EmployeeSubGroupAbsenceType()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG1");
        var absenceTypeId = CreateAbsenceType("AT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupAbsenceTypeDto
        {
            EmployeeSubGroupId = subGroupId,
            AbsenceTypeId = absenceTypeId
        });

        var result = await service.GetAll(new GetAllEmployeeSubGroupAbsenceTypesInput());
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG2");
        var absenceTypeId = CreateAbsenceType("AT2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupAbsenceTypeDto
        {
            EmployeeSubGroupId = subGroupId,
            AbsenceTypeId = absenceTypeId
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupAbsenceTypeDto
            {
                EmployeeSubGroupId = subGroupId,
                AbsenceTypeId = absenceTypeId
            }));
    }
}
