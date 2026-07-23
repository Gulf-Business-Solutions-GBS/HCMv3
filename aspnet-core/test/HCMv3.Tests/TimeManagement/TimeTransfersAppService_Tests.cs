using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class TimeTransfersAppService_Tests : TimeManagementAppServiceTestBase
{
    private TimeTransfersAppService CreateAppService()
    {
        return CreateService(() => new TimeTransfersAppService(FakeRepository.For<TimeTransfer>(Context)));
    }

    private int CreateNationality()
    {
        var entity = new HCMv3.GeneralMasterInfo.Nationality
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "Local",
            NameAr = "محلي"
        };
        Context.Nationalities.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateEmployee(string firstName)
    {
        var entity = new Employee
        {
            TenantId = AbpSession.TenantId ?? 1,
            FirstNameEn = firstName,
            LastNameEn = "Doe",
            FirstNameAr = firstName,
            LastNameAr = "Doe",
            FullNameEn = firstName,
            FullNameAr = firstName,
            NationalityId = CreateNationality()
        };
        Context.Employees.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateTimeTransferType(string code)
    {
        var entity = new TimeTransferType
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.TimeTransferTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_TimeTransfer_And_Round_TransferCount()
    {
        var employeeId = CreateEmployee("John");
        var transferTypeId = CreateTimeTransferType("LIEU");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTimeTransferDto
        {
            EmployeeId = employeeId,
            TransferDate = new DateTime(2026, 1, 1),
            TransferTypeId = transferTypeId,
            TransferCount = 1.23456,
            Comments = "Test"
        });

        var result = await service.GetAll(new GetAllTimeTransfersInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].TransferCount.ShouldBe(1.235);
    }

    [Fact]
    public async Task Should_Throw_When_TransferType_Is_Blank()
    {
        var employeeId = CreateEmployee("Jane");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditTimeTransferDto
            {
                EmployeeId = employeeId,
                TransferDate = new DateTime(2026, 1, 1),
                TransferTypeId = 0,
                TransferCount = 1
            }));
    }
}
