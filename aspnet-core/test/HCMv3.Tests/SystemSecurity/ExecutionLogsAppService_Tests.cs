using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class ExecutionLogsAppService_Tests : SystemSecurityAppServiceTestBase
{
    private ExecutionLogsAppService CreateAppService()
    {
        return CreateService(() => new ExecutionLogsAppService(FakeRepository.For<ExecutionLog>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ExecutionLog()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditExecutionLogDto
        {
            UserName = "admin",
            ActionDateTime = new DateTime(2026, 1, 1),
            Action = ExecutionLogAction.RUN,
            Module = ExecutionLogModule.PayrollEngine,
            ExecutionStatus = ExecutionLogStatus.SUCCESS,
            TotalRecords = 100,
            Successfull = 100,
            Failed = 0
        });

        var result = await service.GetAll(new GetAllExecutionLogsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Module.ShouldBe(ExecutionLogModule.PayrollEngine);
        result.Items[0].ExecutionStatus.ShouldBe(ExecutionLogStatus.SUCCESS);
    }

    [Fact]
    public async Task Should_Filter_By_Module()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditExecutionLogDto
        {
            ActionDateTime = DateTime.Now,
            Action = ExecutionLogAction.RUN,
            Module = ExecutionLogModule.PayrollEngine,
            ExecutionStatus = ExecutionLogStatus.SUCCESS
        });
        await service.CreateOrEdit(new CreateOrEditExecutionLogDto
        {
            ActionDateTime = DateTime.Now,
            Action = ExecutionLogAction.RUN,
            Module = ExecutionLogModule.TimeEngine,
            ExecutionStatus = ExecutionLogStatus.SUCCESS
        });

        var result = await service.GetAll(new GetAllExecutionLogsInput { Module = ExecutionLogModule.TimeEngine });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Module.ShouldBe(ExecutionLogModule.TimeEngine);
    }

    [Fact]
    public async Task Should_Delete_ExecutionLog()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditExecutionLogDto
        {
            ActionDateTime = DateTime.Now,
            Action = ExecutionLogAction.RUN,
            Module = ExecutionLogModule.PayrollEngine,
            ExecutionStatus = ExecutionLogStatus.SUCCESS
        });
        var created = await service.GetAll(new GetAllExecutionLogsInput());
        var id = created.Items[0].Id;

        await service.Delete(new EntityDto(id));

        var result = await service.GetAll(new GetAllExecutionLogsInput());
        result.TotalCount.ShouldBe(0);
    }
}
