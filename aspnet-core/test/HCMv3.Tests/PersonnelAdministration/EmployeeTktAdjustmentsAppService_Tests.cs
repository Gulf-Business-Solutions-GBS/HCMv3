using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeTktAdjustmentsAppService_Tests : AppTestBase
{
    private readonly IEmployeeTktAdjustmentsAppService _appService;

    public EmployeeTktAdjustmentsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeTktAdjustmentsAppService>();
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

    private EmployeeTicketQuota CreateQuota(int employeeId, int ticketTypeId, DateTime from, DateTime to,
        double accrued, double used = 0, double adjusted = 0)
    {
        EmployeeTicketQuota quota = null;
        UsingDbContext(context =>
        {
            quota = context.Set<EmployeeTicketQuota>().Add(new EmployeeTicketQuota
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                TicketTypeId = ticketTypeId,
                EntitlementFrom = from,
                EntitlementTo = to,
                DeductionFrom = from,
                DeductionTo = to,
                QuotaAccrued = accrued,
                QuotaUsed = used,
                QuotaAdjusted = adjusted
            }).Entity;
        });
        return quota;
    }

    private double QuotaAdjustedOf(int quotaId)
    {
        double adjusted = 0;
        UsingDbContext(context => adjusted = context.Set<EmployeeTicketQuota>().Find(quotaId).QuotaAdjusted);
        return adjusted;
    }

    private int SingleAdjustmentId(int employeeId)
    {
        int id = 0;
        UsingDbContext(context => id = context.Set<EmployeeTktAdjustment>().Single(a => a.EmployeeId == employeeId).Id);
        return id;
    }

    private static CreateOrEditEmployeeTktAdjustmentDto Input(int employeeId, int ticketTypeId, DateTime date, double adjusted)
        => new()
        {
            EmployeeId = employeeId,
            TicketTypeId = ticketTypeId,
            AdjustmentType = AdjustmentType.Adjust,
            EffectiveDate = date,
            QuotaAdjusted = adjusted,
            TicketPaymentType = TicketPaymentType.PayInAdvance
        };

    [Fact]
    public async Task Should_Create_Adjustment()
    {
        //Arrange
        var employee = CreateTestEmployee("ETA100");
        var ticketType = CreateTestTicketType("TAT100");
        CreateQuota(employee.Id, ticketType.Id, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), 2);

        //Act
        await _appService.CreateOrEdit(Input(employee.Id, ticketType.Id, new DateTime(2024, 3, 1), 1.5));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeTktAdjustmentsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].QuotaAdjusted.ShouldBe(1.5);
        result.Items[0].TicketPaymentType.ShouldBe(TicketPaymentType.PayInAdvance);
    }

    [Fact]
    public async Task Should_Add_Adjusted_Count_To_Quota_On_Create()
    {
        //Arrange
        var employee = CreateTestEmployee("ETA101");
        var ticketType = CreateTestTicketType("TAT101");
        var quota = CreateQuota(employee.Id, ticketType.Id, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), 3, used: 0.5, adjusted: 0.25);

        //Act
        await _appService.CreateOrEdit(Input(employee.Id, ticketType.Id, new DateTime(2024, 6, 1), 1));

        //Assert
        QuotaAdjustedOf(quota.Id).ShouldBe(1.25);
    }

    [Fact]
    public async Task Should_Refuse_Adjustment_Above_Quota_Balance()
    {
        //Arrange: balance = 2 - (0.5 + 0.5) = 1
        var employee = CreateTestEmployee("ETA102");
        var ticketType = CreateTestTicketType("TAT102");
        var quota = CreateQuota(employee.Id, ticketType.Id, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), 2, used: 0.5, adjusted: 0.5);

        //Act
        var ex = await Should.ThrowAsync<UserFriendlyException>(
            () => _appService.CreateOrEdit(Input(employee.Id, ticketType.Id, new DateTime(2024, 6, 1), 1.5)));

        //Assert
        ex.Message.ShouldContain("1");
        QuotaAdjustedOf(quota.Id).ShouldBe(0.5);
    }

    [Fact]
    public async Task Should_Refuse_NonZero_Adjustment_Without_Quota_Covering_Date()
    {
        //Arrange: quota only covers 2024
        var employee = CreateTestEmployee("ETA103");
        var ticketType = CreateTestTicketType("TAT103");
        CreateQuota(employee.Id, ticketType.Id, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), 5);

        //Act + Assert
        await Should.ThrowAsync<UserFriendlyException>(
            () => _appService.CreateOrEdit(Input(employee.Id, ticketType.Id, new DateTime(2025, 2, 1), 1)));
    }

    [Fact]
    public async Task Should_Allow_Zero_Adjustment_Without_Quota()
    {
        //Arrange
        var employee = CreateTestEmployee("ETA104");
        var ticketType = CreateTestTicketType("TAT104");

        //Act
        await _appService.CreateOrEdit(Input(employee.Id, ticketType.Id, new DateTime(2024, 6, 1), 0));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeTktAdjustmentsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Move_Adjusted_Count_On_Edit_Within_Same_Quota()
    {
        //Arrange: balance 2, take 1.5, then edit to 2 (old 1.5 given back first, so 2 fits)
        var employee = CreateTestEmployee("ETA105");
        var ticketType = CreateTestTicketType("TAT105");
        var quota = CreateQuota(employee.Id, ticketType.Id, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), 2);
        await _appService.CreateOrEdit(Input(employee.Id, ticketType.Id, new DateTime(2024, 6, 1), 1.5));
        var id = SingleAdjustmentId(employee.Id);

        //Act
        var edit = Input(employee.Id, ticketType.Id, new DateTime(2024, 7, 1), 2);
        edit.Id = id;
        await _appService.CreateOrEdit(edit);

        //Assert
        QuotaAdjustedOf(quota.Id).ShouldBe(2);
    }

    [Fact]
    public async Task Should_Move_Adjusted_Count_Between_Quotas_On_Date_Change()
    {
        //Arrange
        var employee = CreateTestEmployee("ETA106");
        var ticketType = CreateTestTicketType("TAT106");
        var quota2024 = CreateQuota(employee.Id, ticketType.Id, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), 2);
        var quota2025 = CreateQuota(employee.Id, ticketType.Id, new DateTime(2025, 1, 1), new DateTime(2025, 12, 31), 2);
        await _appService.CreateOrEdit(Input(employee.Id, ticketType.Id, new DateTime(2024, 6, 1), 1));
        var id = SingleAdjustmentId(employee.Id);

        //Act
        var edit = Input(employee.Id, ticketType.Id, new DateTime(2025, 3, 1), 1);
        edit.Id = id;
        await _appService.CreateOrEdit(edit);

        //Assert
        QuotaAdjustedOf(quota2024.Id).ShouldBe(0);
        QuotaAdjustedOf(quota2025.Id).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Give_Adjusted_Count_Back_On_Delete()
    {
        //Arrange
        var employee = CreateTestEmployee("ETA107");
        var ticketType = CreateTestTicketType("TAT107");
        var quota = CreateQuota(employee.Id, ticketType.Id, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), 2);
        await _appService.CreateOrEdit(Input(employee.Id, ticketType.Id, new DateTime(2024, 6, 1), 1));
        var id = SingleAdjustmentId(employee.Id);

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        QuotaAdjustedOf(quota.Id).ShouldBe(0);
        (await _appService.GetAll(new GetAllEmployeeTktAdjustmentsInput { EmployeeId = employee.Id })).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_Quota_With_Balance_For_Effective_Date()
    {
        //Arrange: balance = 4 - (1 + 0.5) = 2.5
        var employee = CreateTestEmployee("ETA108");
        var ticketType = CreateTestTicketType("TAT108");
        CreateQuota(employee.Id, ticketType.Id, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), 4, used: 1, adjusted: 0.5);

        //Act
        var quota = await _appService.GetQuota(new GetEmployeeTktAdjustmentQuotaInput
        {
            EmployeeId = employee.Id,
            TicketTypeId = ticketType.Id,
            EffectiveDate = new DateTime(2024, 5, 5)
        });

        //Assert
        quota.QuotaFound.ShouldBeTrue();
        quota.QuotaAccrued.ShouldBe(4);
        quota.QuotaBalance.ShouldBe(2.5);
        quota.AvailableBalance.ShouldBe(2.5);
        quota.EntitlementFrom.ShouldBe(new DateTime(2024, 1, 1));
    }

    [Fact]
    public async Task Should_Count_Own_Value_Back_In_Available_Balance_When_Editing()
    {
        //Arrange: accrued 3, this adjustment takes 1 -> balance 2, available to it 3
        var employee = CreateTestEmployee("ETA109");
        var ticketType = CreateTestTicketType("TAT109");
        CreateQuota(employee.Id, ticketType.Id, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), 3);
        await _appService.CreateOrEdit(Input(employee.Id, ticketType.Id, new DateTime(2024, 6, 1), 1));
        var id = SingleAdjustmentId(employee.Id);

        //Act
        var quota = await _appService.GetQuota(new GetEmployeeTktAdjustmentQuotaInput
        {
            EmployeeId = employee.Id,
            TicketTypeId = ticketType.Id,
            EffectiveDate = new DateTime(2024, 6, 1),
            Id = id
        });

        //Assert
        quota.QuotaBalance.ShouldBe(2);
        quota.AvailableBalance.ShouldBe(3);
    }

    [Fact]
    public async Task Should_Report_No_Quota_When_None_Covers_Date()
    {
        //Arrange
        var employee = CreateTestEmployee("ETA110");
        var ticketType = CreateTestTicketType("TAT110");

        //Act
        var quota = await _appService.GetQuota(new GetEmployeeTktAdjustmentQuotaInput
        {
            EmployeeId = employee.Id,
            TicketTypeId = ticketType.Id,
            EffectiveDate = new DateTime(2024, 6, 1)
        });

        //Assert
        quota.QuotaFound.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Filter_List_By_Employee_Code()
    {
        //Arrange
        var employee = CreateTestEmployee("ETAFIND111");
        var ticketType = CreateTestTicketType("TAT111");
        await _appService.CreateOrEdit(Input(employee.Id, ticketType.Id, new DateTime(2024, 6, 1), 0));

        //Act
        var result = await _appService.GetAll(new GetAllEmployeeTktAdjustmentsInput { Filter = "ETAFIND111" });

        //Assert
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeCode.ShouldBe("ETAFIND111");
    }

    [Fact]
    public async Task Should_Return_Employee_On_GetForEdit()
    {
        //Arrange
        var employee = CreateTestEmployee("ETA112");
        var ticketType = CreateTestTicketType("TAT112");
        await _appService.CreateOrEdit(Input(employee.Id, ticketType.Id, new DateTime(2024, 6, 1), 0));
        var id = SingleAdjustmentId(employee.Id);

        //Act
        var output = await _appService.GetEmployeeTktAdjustmentForEdit(new EntityDto(id));

        //Assert
        output.EmployeeCode.ShouldBe("ETA112");
        output.EmployeeTktAdjustment.TicketTypeId.ShouldBe(ticketType.Id);
    }

    [Fact]
    public async Task Should_List_Employees_For_Lookup()
    {
        //Arrange
        CreateTestEmployee("ETALOOKUP113");

        //Act
        var result = await _appService.GetEmployeesForLookup(new GetEmployeeTktAdjustmentEmployeesInput { Filter = "ETALOOKUP113" });

        //Assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].Code.ShouldBe("ETALOOKUP113");
    }
}
