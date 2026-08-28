using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.Authorization.Users;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeUserLinkAppService_Tests : AppTestBase
{
    private readonly IEmployeeUserLinkAppService _employeeUserLinkAppService;

    public EmployeeUserLinkAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _employeeUserLinkAppService = Resolve<IEmployeeUserLinkAppService>();
    }

    private int CreateNationality()
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        return nationality.Id;
    }

    private Employee CreateEmployee(string code, string loginId = null, long? userId = null)
    {
        var nationalityId = CreateNationality();

        Employee employee = null;
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
                LoginID = loginId,
                UserId = userId,
                NationalityId = nationalityId
            }).Entity;
        });

        return employee;
    }

    /// <summary>
    /// Drops the LoginID unique index so a test can seed the duplicate-LoginID state that only exists in
    /// data pre-dating that index.
    ///
    /// This is not a workaround for an awkward test -- it reproduces the real sequence. AutoLinkByLoginId
    /// and GetDuplicateLoginIds exist precisely for the window before
    /// <c>Added_Employee_LoginID_Unique_Index</c> is applied: the index cannot go on until the duplicates
    /// are cleaned, and these are the tools that find and clear them. Once it is applied the ambiguous state
    /// is unreachable, which is the whole point.
    /// </summary>
    private void DropLoginIdUniqueIndex()
    {
        UsingDbContext(context =>
            context.Database.ExecuteSqlRaw("DROP INDEX IF EXISTS \"IX_Employees_TenantId_LoginID\""));
    }

    private long CreateUser(string userName)
    {
        User user = null;
        UsingDbContext(context =>
        {
            user = context.Users.Add(new User
            {
                TenantId = AbpSession.TenantId ?? 1,
                UserName = userName,
                EmailAddress = userName + "@test.local",
                Name = "Test",
                Surname = "User",
                IsActive = true,
                IsEmailConfirmed = true,
                Password = "hashed"
            }).Entity;

            user.SetNormalizedNames();
            context.SaveChanges();
        });

        return user.Id;
    }

    [Fact]
    public async Task Should_Link_User_To_Employee()
    {
        var employee = CreateEmployee("LNK100");
        var userId = CreateUser("lnk100");

        await _employeeUserLinkAppService.LinkUser(new LinkEmployeeUserInput
        {
            EmployeeId = employee.Id,
            UserId = userId
        });

        var stored = UsingDbContext(context => context.Set<Employee>().Single(e => e.Id == employee.Id));
        stored.UserId.ShouldBe(userId);
    }

    [Fact]
    public async Task Should_Reject_Linking_A_User_That_Belongs_To_Another_Employee()
    {
        var userId = CreateUser("lnk200");
        CreateEmployee("LNK200A", userId: userId);
        var other = CreateEmployee("LNK200B");

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _employeeUserLinkAppService.LinkUser(new LinkEmployeeUserInput
            {
                EmployeeId = other.Id,
                UserId = userId
            }));

        exception.Message.ShouldContain("LNK200A");
    }

    [Fact]
    public async Task Should_Allow_Relinking_The_Same_User_To_The_Same_Employee()
    {
        var userId = CreateUser("lnk250");
        var employee = CreateEmployee("LNK250", userId: userId);

        // The "already linked" guard must exclude the employee being edited, or a no-op save from the edit
        // form would reject itself.
        await _employeeUserLinkAppService.LinkUser(new LinkEmployeeUserInput
        {
            EmployeeId = employee.Id,
            UserId = userId
        });

        var stored = UsingDbContext(context => context.Set<Employee>().Single(e => e.Id == employee.Id));
        stored.UserId.ShouldBe(userId);
    }

    [Fact]
    public async Task Should_Throw_When_Linking_A_User_That_Does_Not_Exist()
    {
        var employee = CreateEmployee("LNK300");

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _employeeUserLinkAppService.LinkUser(new LinkEmployeeUserInput
            {
                EmployeeId = employee.Id,
                UserId = 999999
            }));
    }

    [Fact]
    public async Task Should_Unlink_User()
    {
        var userId = CreateUser("lnk400");
        var employee = CreateEmployee("LNK400", userId: userId);

        await _employeeUserLinkAppService.UnlinkUser(new EntityDto(employee.Id));

        var stored = UsingDbContext(context => context.Set<Employee>().Single(e => e.Id == employee.Id));
        stored.UserId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Return_Linked_User_Details()
    {
        var userId = CreateUser("lnk500");
        var employee = CreateEmployee("LNK500", loginId: "lnk500", userId: userId);

        var dto = await _employeeUserLinkAppService.GetEmployeeUserLink(new EntityDto(employee.Id));

        dto.EmployeeCode.ShouldBe("LNK500");
        dto.LoginID.ShouldBe("lnk500");
        dto.UserId.ShouldBe(userId);
        dto.UserExists.ShouldBeTrue();
        dto.UserName.ShouldBe("lnk500");
        dto.UserIsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Report_UserExists_False_When_The_Link_Dangles()
    {
        // Employee.UserId is deliberately not a database FK, so a link can outlive its user.
        var employee = CreateEmployee("LNK550", userId: 999999);

        var dto = await _employeeUserLinkAppService.GetEmployeeUserLink(new EntityDto(employee.Id));

        dto.UserId.ShouldBe(999999);
        dto.UserExists.ShouldBeFalse();
        dto.UserName.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Exclude_Already_Linked_Users_From_The_Picker()
    {
        var takenUserId = CreateUser("lnk600taken");
        CreateEmployee("LNK600", userId: takenUserId);
        CreateUser("lnk600free");

        var result = await _employeeUserLinkAppService.GetLinkableUsers(new GetLinkableUsersInput
        {
            Filter = "lnk600",
            OnlyUnlinked = true
        });

        result.Items.Select(u => u.UserName).ShouldContain("lnk600free");
        result.Items.Select(u => u.UserName).ShouldNotContain("lnk600taken");
    }

    [Fact]
    public async Task Should_Keep_The_Edited_Employees_Own_User_Visible_In_The_Picker()
    {
        var takenUserId = CreateUser("lnk650taken");
        var employee = CreateEmployee("LNK650", userId: takenUserId);

        var result = await _employeeUserLinkAppService.GetLinkableUsers(new GetLinkableUsersInput
        {
            Filter = "lnk650",
            OnlyUnlinked = true,
            ForEmployeeId = employee.Id
        });

        var own = result.Items.SingleOrDefault(u => u.UserName == "lnk650taken");
        own.ShouldNotBeNull();
        own.LinkedEmployeeId.ShouldBe(employee.Id);
        own.LinkedEmployeeCode.ShouldBe("LNK650");
    }

    [Fact]
    public async Task Should_Report_Linked_Employee_When_Not_Filtering_Unlinked()
    {
        var takenUserId = CreateUser("lnk700taken");
        var employee = CreateEmployee("LNK700", userId: takenUserId);

        var result = await _employeeUserLinkAppService.GetLinkableUsers(new GetLinkableUsersInput
        {
            Filter = "lnk700",
            OnlyUnlinked = false
        });

        var taken = result.Items.Single(u => u.UserName == "lnk700taken");
        taken.LinkedEmployeeId.ShouldBe(employee.Id);
    }

    [Fact]
    public async Task Should_AutoLink_Employees_Whose_LoginID_Uniquely_Matches_A_UserName()
    {
        var userId = CreateUser("auto100");
        var employee = CreateEmployee("AUTO100", loginId: "auto100");

        var result = await _employeeUserLinkAppService.AutoLinkByLoginId();

        result.LinkedCount.ShouldBeGreaterThanOrEqualTo(1);

        var stored = UsingDbContext(context => context.Set<Employee>().Single(e => e.Id == employee.Id));
        stored.UserId.ShouldBe(userId);
    }

    [Fact]
    public async Task Should_Not_AutoLink_An_Ambiguous_LoginID()
    {
        DropLoginIdUniqueIndex();

        CreateUser("auto200");
        var first = CreateEmployee("AUTO200A", loginId: "auto200");
        var second = CreateEmployee("AUTO200B", loginId: "auto200");

        var result = await _employeeUserLinkAppService.AutoLinkByLoginId();

        result.AmbiguousLoginIds.ShouldContain("auto200");
        result.AmbiguousLoginIdCount.ShouldBeGreaterThanOrEqualTo(2);

        UsingDbContext(context =>
        {
            context.Set<Employee>().Single(e => e.Id == first.Id).UserId.ShouldBeNull();
            context.Set<Employee>().Single(e => e.Id == second.Id).UserId.ShouldBeNull();
        });
    }

    [Fact]
    public async Task Should_Not_AutoLink_A_User_Already_Linked_Elsewhere()
    {
        var userId = CreateUser("auto300");
        CreateEmployee("AUTO300A", userId: userId);
        var unlinked = CreateEmployee("AUTO300B", loginId: "auto300");

        var result = await _employeeUserLinkAppService.AutoLinkByLoginId();

        result.UserAlreadyLinkedCount.ShouldBeGreaterThanOrEqualTo(1);

        var stored = UsingDbContext(context => context.Set<Employee>().Single(e => e.Id == unlinked.Id));
        stored.UserId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Count_Employees_Whose_LoginID_Matches_No_User()
    {
        var employee = CreateEmployee("AUTO400", loginId: "nosuchuser400");

        var result = await _employeeUserLinkAppService.AutoLinkByLoginId();

        result.NoMatchingUserCount.ShouldBeGreaterThanOrEqualTo(1);

        var stored = UsingDbContext(context => context.Set<Employee>().Single(e => e.Id == employee.Id));
        stored.UserId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Report_Existing_Duplicate_LoginIds()
    {
        DropLoginIdUniqueIndex();

        var linkedUserId = CreateUser("dup100");
        var kept = CreateEmployee("DUP100A", loginId: "dup100", userId: linkedUserId);
        var stale = CreateEmployee("DUP100B", loginId: "dup100");

        var duplicates = await _employeeUserLinkAppService.GetDuplicateLoginIds();

        var group = duplicates.Single(d => d.LoginID == "dup100");
        group.Employees.Count.ShouldBe(2);

        // The employee already holding the explicit user link is listed first -- that is the one to keep.
        group.Employees.First().EmployeeId.ShouldBe(kept.Id);
        group.Employees.First().UserId.ShouldBe(linkedUserId);
        group.Employees.Last().EmployeeId.ShouldBe(stale.Id);
    }

    [Fact]
    public async Task Should_Group_Duplicate_LoginIds_Case_Insensitively()
    {
        DropLoginIdUniqueIndex();

        // SQL Server's default collation is case-insensitive, so these two already resolve to the same
        // login at sign-in. The report groups them the same way regardless of the test database's collation.
        CreateEmployee("DUP200A", loginId: "dup200");
        CreateEmployee("DUP200B", loginId: "DUP200");

        var duplicates = await _employeeUserLinkAppService.GetDuplicateLoginIds();

        duplicates.Count(d => string.Equals(d.LoginID, "dup200", StringComparison.OrdinalIgnoreCase))
            .ShouldBe(1);
    }

    [Fact]
    public async Task Should_Report_No_Duplicates_For_Clean_Data()
    {
        CreateEmployee("DUP300A", loginId: "dup300a");
        CreateEmployee("DUP300B", loginId: "dup300b");
        CreateEmployee("DUP300C");

        var duplicates = await _employeeUserLinkAppService.GetDuplicateLoginIds();

        duplicates.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Not_Overwrite_An_Existing_Link_When_AutoLink_Is_Re_Run()
    {
        var userId = CreateUser("auto500");
        var otherUserId = CreateUser("auto500other");
        var employee = CreateEmployee("AUTO500", loginId: "auto500other", userId: userId);

        await _employeeUserLinkAppService.AutoLinkByLoginId();

        var stored = UsingDbContext(context => context.Set<Employee>().Single(e => e.Id == employee.Id));
        stored.UserId.ShouldBe(userId);
        stored.UserId.ShouldNotBe(otherUserId);
    }
}
