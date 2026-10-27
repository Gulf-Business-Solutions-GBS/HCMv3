using System;
using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.HealthAndSafetyManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class FirstAidBoxDetailsAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private FirstAidBoxDetailsAppService CreateAppService()
    {
        return CreateService(() => new FirstAidBoxDetailsAppService(FakeRepository.For<FirstAidBoxDetail>(Context)));
    }

    private int CreateFirstAidBox()
    {
        var box = new FirstAidBox
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "BOX-1",
            LocationId = CreateWorkLocation(),
            AllocationDate = new DateTime(2026, 1, 1),
            ResponsiblePersonId = CreateEmployee()
        };
        Context.FirstAidBoxes.Add(box);
        Context.SaveChanges();
        return box.Id;
    }

    private int CreateMedicine()
    {
        var entity = new FirstAidMedicine { TenantId = AbpSession.TenantId ?? 1, NameEn = "Bandage", NameAr = "Bandage" };
        Context.FirstAidMedicines.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_FirstAidBoxDetail()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditFirstAidBoxDetailDto
        {
            FirstAidBoxId = CreateFirstAidBox(),
            MedicineId = CreateMedicine(),
            ExpiryDate = new DateTime(2027, 1, 1),
            AllocationDate = new DateTime(2026, 1, 1),
            AllocatedById = CreateEmployee()
        });

        var result = await service.GetAll(new GetAllFirstAidBoxDetailsInput());
        result.TotalCount.ShouldBe(1);
    }
}
