using HCMv3.Authorization.Users;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Authorization.Users;

public class SysUserNamesTests
{
    [Theory]
    [InlineData("SysAdmin")]
    [InlineData("SysDataAdmin")]
    [InlineData("SysPayrollAdmin")]
    [InlineData("SysTimeAdmin")]
    [InlineData("SysJobAdmin")]
    public void Should_Recognize_Known_System_Usernames(string userName)
    {
        SysUserNames.IsSysUser(userName).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("SuperAdmin")]
    [InlineData("john.doe")]
    [InlineData("")]
    [InlineData(null)]
    public void Should_Not_Recognize_NonSystem_Usernames(string userName)
    {
        SysUserNames.IsSysUser(userName).ShouldBeFalse();
    }

    [Fact]
    public void SysUserList_Should_Contain_Exactly_The_Five_Known_System_Usernames()
    {
        var list = SysUserNames.SysUserList();

        list.ShouldBe(new[]
        {
            SysUserNames.SysAdmin,
            SysUserNames.SysPayrollAdmin,
            SysUserNames.SysTimeAdmin,
            SysUserNames.SysDataAdmin,
            SysUserNames.SysJobAdmin
        });
    }
}
