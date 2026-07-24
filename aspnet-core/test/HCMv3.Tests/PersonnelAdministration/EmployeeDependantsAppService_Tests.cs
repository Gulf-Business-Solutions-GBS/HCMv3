using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeDependantsAppService_Tests : AppTestBase
{
    private readonly IEmployeeDependantsAppService _appService;

    public EmployeeDependantsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeDependantsAppService>();
    }

    private Employee CreateTestEmployee(string code)
    {
        Nationality nationality = null;
        Employee employee = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });
        return employee;
    }

    private DependantType CreateTestDependantType(string code)
    {
        DependantType dependantType = null;
        UsingDbContext(context =>
        {
            dependantType = context.Set<DependantType>().Add(new DependantType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return dependantType;
    }

    private TicketType CreateTestTicketType(string code)
    {
        TicketType ticketType = null;
        UsingDbContext(context =>
        {
            ticketType = context.Set<TicketType>().Add(new TicketType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return ticketType;
    }

    [Fact]
    public async Task Should_Create_Dependant()
    {
        //Arrange
        var employee = CreateTestEmployee("EDP100");
        var dependantType = CreateTestDependantType("DT100");
        var ticketType = CreateTestTicketType("DTT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeDependantDto
        {
            EmployeeId = employee.Id,
            NameEn = "Child One",
            NameAr = "الطفل الأول",
            DependantTypeId = dependantType.Id,
            BirthDate = new DateTime(2015, 1, 1),
            TicketTypeId = ticketType.Id,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(9999, 12, 31)
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeDependantsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Child One");
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_Dependant_For_Same_TicketType()
    {
        //Arrange
        var employee = CreateTestEmployee("EDP200");
        var dependantType = CreateTestDependantType("DT200");
        var ticketType = CreateTestTicketType("DTT200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeDependantDto
        {
            EmployeeId = employee.Id,
            NameEn = "Child One",
            NameAr = "الطفل الأول",
            DependantTypeId = dependantType.Id,
            BirthDate = new DateTime(2015, 1, 1),
            TicketTypeId = ticketType.Id,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(9999, 12, 31)
        });

        //Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeDependantDto
            {
                EmployeeId = employee.Id,
                NameEn = "Child Two",
                NameAr = "الطفل الثاني",
                DependantTypeId = dependantType.Id,
                BirthDate = new DateTime(2016, 1, 1),
                TicketTypeId = ticketType.Id,
                StartDate = new DateTime(2024, 6, 1),
                EndDate = new DateTime(9999, 12, 31)
            }));
    }
}
