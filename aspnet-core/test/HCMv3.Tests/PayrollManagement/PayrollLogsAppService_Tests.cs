using System;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class PayrollLogsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private PayrollLogsAppService CreateAppService()
    {
        return CreateService(() => new PayrollLogsAppService(FakeRepository.For<PayrollLog>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_PayrollLog()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPayrollLogDto
        {
            Message = "Payroll run started",
            RunTimeStamp = new DateTime(2025, 1, 1, 8, 0, 0),
            EmployeeId = null,
            Status = 1,
            InPeriod = new DateTime(2025, 1, 1),
            StopFlag = false,
            NoOfEmployee = 100
        });

        var result = await service.GetAll(new GetAllPayrollLogsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Message.ShouldBe("Payroll run started");
        result.Items[0].NoOfEmployee.ShouldBe(100);
    }
}
