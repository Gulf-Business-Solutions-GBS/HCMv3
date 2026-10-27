using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// The legacy Full &amp; Final filter restored with the rejoin port (legacy <c>EmployeeFnFPayment.cs</c> lines 714 and
/// 1077): only a rejoin that KEPT the employee code restarts service on the old record (<c>startExit</c>).
///
/// <para>The scenario: joined 01/01/2015, first F&amp;F on 31/12/2019, rejoined 01/01/2020, leaving again on
/// 30/06/2024. If the rejoin restarts service, the 2019 F&amp;F lies before it and a second F&amp;F is allowed. If it
/// does not, the 2019 F&amp;F still counts and the second one is refused.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class FullAndFinalSettlementService_Rejoin_Tests : FullAndFinalTestBase
{
    private static readonly DateTime Joined = new DateTime(2015, 1, 1);
    private static readonly DateTime FirstCompensation = new DateTime(2019, 12, 31);
    private static readonly DateTime Rejoined = new DateTime(2020, 1, 1);
    private static readonly DateTime LastDay = new DateTime(2024, 6, 30);

    private readonly IFullAndFinalSettlementService _service;

    public FullAndFinalSettlementService_Rejoin_Tests()
    {
        _service = Resolve<IFullAndFinalSettlementService>();
    }

    [Fact]
    public async Task Should_Restart_Service_At_A_Rejoin_That_Kept_The_Employee_Code()
    {
        var employeeId = SeedEmployeeWhoLeftAndRejoined(createNewEmployeeCode: false);

        await Should.NotThrowAsync(() => _service.ValidateSaveAsync(Values(employeeId), null, null));
    }

    [Fact]
    public async Task Should_Not_Restart_Service_At_A_Rejoin_That_Created_A_New_Employee()
    {
        var employeeId = SeedEmployeeWhoLeftAndRejoined(createNewEmployeeCode: true);

        var ex = await Should.ThrowAsync<UserFriendlyException>(
            () => _service.ValidateSaveAsync(Values(employeeId), null, null));

        ex.Message.ShouldStartWith("The Full & Final payment has already been processed");
    }

    /// <summary>The same filter applies when an existing F&amp;F is edited (legacy <c>ValidatePreviousEntry</c>, line 1077).</summary>
    [Fact]
    public async Task Should_Apply_The_Filter_To_The_Entry_Being_Edited_Too()
    {
        var keptId = SeedEmployeeWhoLeftAndRejoined(createNewEmployeeCode: false);
        var newCodeId = SeedEmployeeWhoLeftAndRejoined(createNewEmployeeCode: true);

        await Should.NotThrowAsync(() => _service.ValidateDeleteAsync(Values(keptId), 999));
        var ex = await Should.ThrowAsync<UserFriendlyException>(() => _service.ValidateDeleteAsync(Values(newCodeId), 999));

        ex.Message.ShouldStartWith("The Full & Final payment has already been processed");
    }

    /// <summary>
    /// Legacy's "Rejoin made after the compensation date" check (line 722) has NO new-code filter: any later rejoin
    /// freezes the F&amp;F. Kept.
    /// </summary>
    [Fact]
    public async Task Should_Still_Freeze_The_Full_And_Final_After_Any_Later_Rejoin()
    {
        var seeded = SeedLeaver(Joined, LastDay, SeparationType.Termination);
        UsingDbContext(c => c.Set<EmployeeRejoin>().Add(new EmployeeRejoin
        {
            TenantId = TenantId, EmployeeId = seeded.EmployeeId, RejoinDate = new DateTime(2024, 9, 1), CreateNewEmployeeCode = true
        }));

        var ex = await Should.ThrowAsync<UserFriendlyException>(
            () => _service.ValidateSaveAsync(Values(seeded.EmployeeId), null, null));

        ex.Message.ShouldStartWith("The Rejoin has already been processed for this employee.");
    }

    private int SeedEmployeeWhoLeftAndRejoined(bool createNewEmployeeCode)
    {
        var seeded = SeedLeaver(Joined, LastDay, SeparationType.Termination);

        UsingDbContext(context =>
        {
            context.Set<EmployeeFnFPayment>().Add(new EmployeeFnFPayment
            {
                TenantId = TenantId, EmployeeId = seeded.EmployeeId, CompensationDate = FirstCompensation
            });
            context.Set<EmployeeRejoin>().Add(new EmployeeRejoin
            {
                TenantId = TenantId,
                EmployeeId = seeded.EmployeeId,
                RejoinDate = Rejoined,
                CreateNewEmployeeCode = createNewEmployeeCode
            });
        });

        return seeded.EmployeeId;
    }

    private static FullAndFinalValues Values(int employeeId)
    {
        return new FullAndFinalValues { EmployeeId = employeeId, CompensationDate = LastDay };
    }
}
