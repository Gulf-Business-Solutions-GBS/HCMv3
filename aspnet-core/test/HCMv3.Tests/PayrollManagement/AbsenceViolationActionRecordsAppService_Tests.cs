using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class AbsenceViolationActionRecordsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private AbsenceViolationActionRecordsAppService CreateAppService()
    {
        return CreateService(() => new AbsenceViolationActionRecordsAppService(FakeRepository.For<AbsenceViolationActionRecord>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_AbsenceViolationActionRecord()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsenceViolationActionRecordDto
        {
            ActionLogType = ActionLogType.Leave,
            LogStatus = LogStatus.Open,
            EntityObjectId = 42
        });

        var result = await service.GetAll(new GetAllAbsenceViolationActionRecordsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].ActionLogType.ShouldBe(ActionLogType.Leave);
        result.Items[0].EntityObjectId.ShouldBe(42);
    }

    [Fact]
    public void ActionLogType_Members_Should_Have_Distinct_Values()
    {
        // BUG FIX parity: the legacy enum had Leave = 0, TimeEvent = 0 (copy-paste bug).
        ((int)ActionLogType.Leave).ShouldNotBe((int)ActionLogType.TimeEvent);
        ((int)ActionLogType.TimeEvent).ShouldBe(1);
    }
}
