using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.MultiTenancy;
using Abp.UI;
using HCMv3.Authorization.Users;
using HCMv3.EffectiveDated;
using HCMv3.EffectiveDated.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.EffectiveDated;

/// <summary>
/// Filling a new date-effective record from the one in force on the date the user typed: adding a salary effective
/// 01/01/2027 starts from the 01/01/2026 values, and a back-dated 01/07/2025 starts from the 01/01/2025 ones.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EffectiveDatedPrefill_Tests : AppTestBase
{
    private const int TenantId = 1;
    private static readonly DateTime OpenEnded = new(9999, 12, 31);

    private readonly IEffectiveDatedPrefillAppService _service;
    private readonly int _employeeId;

    public EffectiveDatedPrefill_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _service = Resolve<IEffectiveDatedPrefillAppService>();

        _employeeId = CreateEmployee("ED-001");
        SeedBasicPay(_employeeId, new DateTime(2025, 1, 1), 1000);
        SeedBasicPay(_employeeId, new DateTime(2026, 1, 1), 1500);
    }

    [Fact]
    public async Task Should_Take_The_Values_Of_The_Latest_Record_For_A_Future_Date()
    {
        var result = await PrefillAsync(new DateTime(2027, 1, 1));

        result.Found.ShouldBeTrue();
        result.SourceDate.ShouldBe(new DateTime(2026, 1, 1));
        result.Values["amount"].ShouldBe(1500d);
    }

    [Fact]
    public async Task Should_Take_The_Values_In_Force_On_A_Back_Dated_Date()
    {
        // The record covering 01/07/2025 is the one starting 01/01/2025 -- not the newest one.
        var result = await PrefillAsync(new DateTime(2025, 7, 1));

        result.SourceDate.ShouldBe(new DateTime(2025, 1, 1));
        result.Values["amount"].ShouldBe(1000d);
    }

    [Fact]
    public async Task Should_Take_The_Record_Starting_On_That_Very_Day()
    {
        var result = await PrefillAsync(new DateTime(2026, 1, 1));

        result.SourceDate.ShouldBe(new DateTime(2026, 1, 1));
    }

    [Fact]
    public async Task Should_Find_Nothing_Before_The_History_Starts()
    {
        var result = await PrefillAsync(new DateTime(2024, 6, 1));

        result.Found.ShouldBeFalse();
        result.Values.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Not_Read_Another_Employees_History()
    {
        var other = CreateEmployee("ED-002");

        var result = await PrefillAsync(new DateTime(2027, 1, 1), other);

        result.Found.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Separate_Histories_That_Are_Split_By_A_Second_Key()
    {
        // A wage account is a history per employee AND wage type: asking for one must not return the other's values.
        var wageTypeA = CreateWageType("WT-A");
        var wageTypeB = CreateWageType("WT-B");
        SeedWageAccount(wageTypeA, new DateTime(2025, 1, 1), 40);
        SeedWageAccount(wageTypeB, new DateTime(2025, 1, 1), 60);

        var result = await _service.GetPrefill(new EffectiveDatedPrefillInput
        {
            RecordType = "EmployeeWageAccount",
            EffectiveDate = new DateTime(2026, 1, 1),
            Keys = new List<EffectiveDatedKeyDto>
            {
                new() { Name = "EmployeeId", Value = _employeeId },
                new() { Name = "WageTypeId", Value = wageTypeB },
            },
        });

        result.Found.ShouldBeTrue();
        result.Values["costPercentage"].ShouldBe(60d);
    }

    [Fact]
    public async Task Should_Leave_Out_The_Id_The_Dates_And_The_Audit_Fields()
    {
        var result = await PrefillAsync(new DateTime(2027, 1, 1));

        // The new record is a new record, and takes the date the user typed.
        result.Values.Keys.ShouldNotContain("id");
        result.Values.Keys.ShouldNotContain("startDate");
        result.Values.Keys.ShouldNotContain("endDate");
        result.Values.Keys.ShouldNotContain("creationTime");
        result.Values.Keys.ShouldNotContain("tenantId");

        // What it does carry: the values worth starting from.
        result.Values.Keys.ShouldContain("amount");
        result.Values.Keys.ShouldContain("employeeId");
    }

    [Fact]
    public async Task Should_Refuse_A_Record_Type_It_Does_Not_Know()
    {
        await Should.ThrowAsync<UserFriendlyException>(() => _service.GetPrefill(new EffectiveDatedPrefillInput
        {
            RecordType = "NotARecordType",
            EffectiveDate = new DateTime(2027, 1, 1),
            Keys = new List<EffectiveDatedKeyDto> { new() { Name = "EmployeeId", Value = _employeeId } },
        }));
    }

    [Fact]
    public async Task Should_Refuse_When_A_Key_Is_Missing()
    {
        // Without the employee it would read somebody's history -- whose is anybody's guess.
        await Should.ThrowAsync<UserFriendlyException>(() => _service.GetPrefill(new EffectiveDatedPrefillInput
        {
            RecordType = "EmployeeBasicPay",
            EffectiveDate = new DateTime(2027, 1, 1),
            Keys = new List<EffectiveDatedKeyDto>(),
        }));
    }

    [Fact]
    public async Task Should_Refuse_A_User_Who_May_Not_See_The_Records()
    {
        var userName = "edp" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var user = new User
        {
            TenantId = TenantId,
            UserName = userName,
            Name = "No",
            Surname = "Permissions",
            EmailAddress = userName + "@gbskw.com",
            IsEmailConfirmed = true,
            Password = "AM4OLBpptxBYmM79lGOX9egzZk3vIQU3d/gFCJzaBjAPXzYIK3tQ2N7X4fcrHtElTw=="
        };
        user.SetNormalizedNames();
        UsingDbContext(context => context.Users.Add(user));

        LoginAsTenant(AbpTenantBase.DefaultTenantName, userName);

        await Should.ThrowAsync<AbpAuthorizationException>(() => PrefillAsync(new DateTime(2027, 1, 1)));
    }

    // ------------------------------------------------------------------ helpers

    private Task<EffectiveDatedPrefillDto> PrefillAsync(DateTime effectiveDate, int? employeeId = null) =>
        _service.GetPrefill(new EffectiveDatedPrefillInput
        {
            RecordType = "EmployeeBasicPay",
            EffectiveDate = effectiveDate,
            Keys = new List<EffectiveDatedKeyDto>
            {
                new() { Name = "EmployeeId", Value = employeeId ?? _employeeId },
            },
        });

    private void SeedBasicPay(int employeeId, DateTime startDate, double amount) =>
        UsingDbContext(c => c.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay
        {
            TenantId = TenantId, EmployeeId = employeeId, StartDate = startDate, EndDate = OpenEnded, Amount = amount
        }));

    private void SeedWageAccount(int wageTypeId, DateTime startDate, double costPercentage) =>
        UsingDbContext(c => c.Set<HCMv3.PayrollManagement.EmployeeWageAccount>().Add(new HCMv3.PayrollManagement.EmployeeWageAccount
        {
            TenantId = TenantId,
            EmployeeId = _employeeId,
            WageTypeId = wageTypeId,
            StartDate = startDate,
            EndDate = OpenEnded,
            // The test database enforces foreign keys, and a wage account must point at a real account.
            AccountInfoId = AccountInfoId(),
            CostPercentage = costPercentage,
        }));

    private int? _accountInfoId;

    private int AccountInfoId()
    {
        if (_accountInfoId == null)
        {
            HCMv3.PayrollManagement.AccountInfo account = null;

            UsingDbContext(c =>
            {
                var accountType = c.Set<HCMv3.PayrollManagement.AccountType>().Add(new HCMv3.PayrollManagement.AccountType
                {
                    TenantId = TenantId, Code = "AT", NameEn = "Ledger", NameAr = "Ledger"
                }).Entity;
                c.SaveChanges();

                account = c.Set<HCMv3.PayrollManagement.AccountInfo>().Add(new HCMv3.PayrollManagement.AccountInfo
                {
                    TenantId = TenantId, Code = "AC", NameEn = "Account", NameAr = "Account", AccountTypeId = accountType.Id
                }).Entity;
            });

            // Read after the block: the id is only assigned once the context has saved.
            _accountInfoId = account.Id;
        }

        return _accountInfoId.Value;
    }

    private int CreateWageType(string code)
    {
        HCMv3.PayrollManagement.WageType row = null;
        UsingDbContext(c => row = c.Set<HCMv3.PayrollManagement.WageType>().Add(new HCMv3.PayrollManagement.WageType
        {
            TenantId = TenantId, Code = code, NameEn = code, NameAr = code
        }).Entity);
        return row.Id;
    }

    private int CreateEmployee(string code)
    {
        Employee employee = null;
        UsingDbContext(c =>
        {
            var nationality = c.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId, Code = Guid.NewGuid().ToString("N")[..8], NameEn = "N", NameAr = "N"
            }).Entity;
            c.SaveChanges();

            employee = c.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = code,
                FullNameAr = code,
                BirthDate = new DateTime(1990, 1, 1),
                Gender = Gender.Male,
                NationalityId = nationality.Id,
                JoiningDate = new DateTime(2020, 1, 1),
            }).Entity;
        });
        return employee.Id;
    }
}
