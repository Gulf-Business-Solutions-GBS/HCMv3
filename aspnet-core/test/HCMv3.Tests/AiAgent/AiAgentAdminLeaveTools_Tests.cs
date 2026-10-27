using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.AiAgent.Dto;
using HCMv3.Authorization;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AiAgent;

/// <summary>
/// "What is the leave balance of 1002" from an administrator -- answered "I don't have access" on the server,
/// because the only leave tools were the self-service ones, which show the signed-in user's own data.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AiAgentAdminLeaveTools_Tests : AiAgentTestBase
{
    private string LastToolResult() => Model.Requests.Last().Messages.Last().Content;

    private async Task AskForBalance(string code, string asOf = null)
    {
        var arguments = asOf == null ? $@"{{""employeeCode"":""{code}""}}" : $@"{{""employeeCode"":""{code}"",""asOfDate"":""{asOf}""}}";
        Model.CallsTool("get_employee_leave_balances", arguments).RepliesWith("Here it is.");
        await Service.SendMessage(new SendAiMessageInput { Message = "what is the leave balance of " + code });
    }

    [Fact]
    public async Task An_Administrator_Should_See_Another_Employees_Leave_Balance()
    {
        var employee = CreateEmployee("1002");
        var annual = CreateAbsenceType("Annual Leave", "إجازة سنوية");
        CreateQuota(employee, annual, accrued: 30, used: 5);

        await AskForBalance("1002");

        var result = LastToolResult();
        result.ShouldContain(@"""code"":""1002""");
        result.ShouldContain(@"""leaveTypeEn"":""Annual Leave""");
        result.ShouldContain(@"""leaveTypeAr"":""إجازة سنوية""");
        result.ShouldContain(@"""accrued"":30");
        result.ShouldContain(@"""used"":5");
        result.ShouldContain(@"""balance"":25");
    }

    [Fact]
    public async Task It_Should_Say_When_The_Employee_Has_No_Entitlement()
    {
        CreateEmployee("1003");

        await AskForBalance("1003");

        LastToolResult().ShouldContain("no leave entitlement set up");
    }

    [Fact]
    public async Task It_Should_Ask_When_The_Code_Does_Not_Exist()
    {
        await AskForBalance("9999");

        LastToolResult().ShouldContain("employee_not_found");
    }

    [Fact]
    public async Task It_Should_Use_The_Period_Covering_The_Date_Asked_About()
    {
        var employee = CreateEmployee("1004");
        var annual = CreateAbsenceType("Annual Leave", "إجازة سنوية");
        CreateQuota(employee, annual, accrued: 30); // covers today, one year either side

        await AskForBalance("1004", DateTime.Today.AddYears(5).ToString("yyyy-MM-dd"));

        LastToolResult().ShouldContain("No entitlement period covers that date");
    }

    [Fact]
    public async Task The_Tool_Should_Be_Offered_To_An_Administrator_As_An_Administration_Tool()
    {
        Model.RepliesWith("hi");

        await Service.SendMessage(new SendAiMessageInput { Message = "hi" });

        Model.Requests.Single().Tools.Select(t => t.Name).ShouldContain("get_employee_leave_balances");
    }

    [Fact]
    public async Task An_Employee_Should_Not_Be_Offered_Other_Peoples_Leave()
    {
        AbpSession.UserId = CreateUser(AppPermissions.Ess, AppPermissions.Ess_AiAgent, AppPermissions.Ess_MyLeave);
        Model.RepliesWith("hi");

        await Service.SendMessage(new SendAiMessageInput { Message = "what is the leave balance of 1002" });

        Model.Requests.Single().Tools.Select(t => t.Name).ShouldNotContain("get_employee_leave_balances");
    }
}
