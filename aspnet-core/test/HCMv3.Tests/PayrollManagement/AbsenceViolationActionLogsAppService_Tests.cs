using System;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class AbsenceViolationActionLogsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private AbsenceViolationActionLogsAppService CreateAppService()
    {
        return CreateService(() => new AbsenceViolationActionLogsAppService(FakeRepository.For<AbsenceViolationActionLog>(Context)));
    }

    private int CreateAbsenceViolationActionRecord()
    {
        var entity = new AbsenceViolationActionRecord
        {
            TenantId = AbpSession.TenantId ?? 1,
            ActionLogType = ActionLogType.Leave,
            LogStatus = LogStatus.Open,
            EntityObjectId = 1
        };
        Context.AbsenceViolationActionRecords.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_AbsenceViolationActionLog()
    {
        var recordId = CreateAbsenceViolationActionRecord();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsenceViolationActionLogDto
        {
            AbsenceViolationActionRecordId = recordId,
            Step = 1,
            Action = AbsenceViolationAction.Notification,
            ActionDate = new DateTime(2026, 1, 1),
            ObjectOId = 99
        });

        var result = await service.GetAll(new GetAllAbsenceViolationActionLogsInput { AbsenceViolationActionRecordId = recordId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Action.ShouldBe(AbsenceViolationAction.Notification);
        result.Items[0].ObjectOId.ShouldBe(99);
    }
}
