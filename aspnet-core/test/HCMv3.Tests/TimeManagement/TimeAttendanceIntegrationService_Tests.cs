using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using ClosedXML.Excel;
using HCMv3.GeneralMasterInfo;
using HCMv3.Integrations.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class TimeAttendanceIntegrationService_Tests : AppTestBase
{
    /// <summary>Routes fake HTTP requests to canned responses, keyed by a substring of the request URL.</summary>
    private class RoutingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public RoutingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }

    private class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public FakeHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name) => new HttpClient(_handler);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json)
        };
    }

    /// <summary>
    /// <see cref="TimeAttendanceIntegrationService"/> is constructed directly (not resolved from the
    /// container) so a fake <see cref="IHttpClientFactory"/> can be substituted -- meaning the class-level
    /// <c>[UnitOfWork]</c> attribute's Castle-proxy interceptor never applies. Each repository-touching call
    /// is therefore wrapped in an explicit ambient <see cref="IUnitOfWorkManager"/> scope, mirroring what the
    /// interceptor would have done automatically had the service been resolved via DI.
    /// </summary>
    private async Task<T> RunInUnitOfWorkAsync<T>(Func<Task<T>> action)
    {
        using var uow = Resolve<IUnitOfWorkManager>().Begin();
        var result = await action();
        await uow.CompleteAsync();
        return result;
    }

    private TimeAttendanceIntegrationService CreateService(HttpMessageHandler handler = null)
    {
        return new TimeAttendanceIntegrationService(
            Resolve<IRepository<TimeAttAPI>>(),
            Resolve<IRepository<TimeEvent>>(),
            Resolve<IRepository<Employee>>(),
            Resolve<IRepository<TransType>>(),
            Resolve<IRepository<AccessUnit>>(),
            Resolve<ISysSettingsAccessor>(),
            new FakeHttpClientFactory(handler ?? new RoutingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound))),
            AbpSession);
    }

    private int CreateNationality()
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                NameEn = "Local",
                NameAr = "محلي"
            }).Entity;
        });
        return nationality.Id;
    }

    private int CreateEmployee(string accessId)
    {
        Employee employee = null;
        var nationalityId = CreateNationality();
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                AccessID = accessId,
                NationalityId = nationalityId
            }).Entity;
        });
        return employee.Id;
    }

    private int CreateTransType(string code)
    {
        TransType transType = null;
        UsingDbContext(context =>
        {
            transType = context.Set<TransType>().Add(new TransType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return transType.Id;
    }

    private void CreateTimeAttApi(string punchInCode = "I", string punchOutCode = "O", string password = "pwd")
    {
        UsingDbContext(context =>
        {
            context.Set<TimeAttAPI>().Add(new TimeAttAPI
            {
                TenantId = AbpSession.TenantId ?? 1,
                TokenUrl = "http://fake-vendor/api/token/",
                TransactionsUrl = "http://fake-vendor/api/transactions/?",
                DevicesUrl = "http://fake-vendor/api/devices/?page=1",
                NoOfRecords = 100,
                PunchInCode = punchInCode,
                PunchOutCode = punchOutCode,
                BreakInCode = "",
                BreakOutCode = "",
                Username = "user",
                Email = "user@example.com",
                Password = password,
                Company = "co"
            });
        });
    }

    private void EnableApiIntegration()
    {
        UsingDbContext(context =>
        {
            context.Set<ManageTimeSettings>().Add(new ManageTimeSettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                EnableTimeAttIntegrationAPI = true,
                EarliestEvaluationDate = new DateTime(2020, 1, 1)
            });
        });
    }

    [Fact]
    public async Task TransferTransactionFromApiAsync_Should_Return_Zero_When_Integration_Disabled()
    {
        var service = CreateService();

        var result = await RunInUnitOfWorkAsync(() => service.TransferTransactionFromApiAsync());

        result.ShouldBe(0);
    }

    [Fact]
    public async Task TransferTransactionFromApiForMissingDataAsync_Should_Return_Zero_When_No_Prior_TimeEvent_Exists()
    {
        EnableApiIntegration();
        var service = CreateService();

        var result = await RunInUnitOfWorkAsync(() => service.TransferTransactionFromApiForMissingDataAsync(
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), "", ""));

        result.ShouldBe(0);
    }

    [Fact]
    public async Task InsertTransactionFromApiAsync_Should_Return_Zero_When_No_TimeAttApi_Configured()
    {
        var service = CreateService();

        var result = await RunInUnitOfWorkAsync(() => service.InsertTransactionFromApiAsync(
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 1), "", ""));

        result.ShouldBe(0);
    }

    [Fact]
    public async Task GetTokenAsync_Should_Return_Token_On_Success()
    {
        var handler = new RoutingHttpMessageHandler(request =>
            JsonResponse(HttpStatusCode.OK, "{\"token\":\"tok-123\"}"));
        var service = CreateService(handler);

        var timeAttApi = new TimeAttAPI { TokenUrl = "http://fake-vendor/api/token/" };
        var token = await service.GetTokenAsync(timeAttApi);

        token.ShouldBe("tok-123");
    }

    [Fact]
    public async Task GetTokenAsync_Should_Return_Null_On_Http_Failure()
    {
        var handler = new RoutingHttpMessageHandler(request =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var service = CreateService(handler);

        var timeAttApi = new TimeAttAPI { TokenUrl = "http://fake-vendor/api/token/" };
        var token = await service.GetTokenAsync(timeAttApi);

        token.ShouldBeNull();
    }

    /// <summary>
    /// Records the password in the token request, then fails it so the sync stops right after sign-in.
    /// </summary>
    private static RoutingHttpMessageHandler CapturePasswordHandler(Action<string> onPassword)
    {
        return new RoutingHttpMessageHandler(request =>
        {
            if (request.RequestUri!.ToString().Contains("/token/"))
            {
                var body = request.Content!.ReadAsStringAsync().Result;
                onPassword(JObject.Parse(body).Value<string>("password"));
            }

            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        });
    }

    [Fact]
    public async Task GetTokenAsync_Should_Send_Plain_Password_When_Stored_Encrypted()
    {
        string sent = null;
        var service = CreateService(CapturePasswordHandler(p => sent = p));

        await service.GetTokenAsync(new TimeAttAPI
        {
            TokenUrl = "http://fake-vendor/api/token/",
            Password = IntegrationSecret.Protect("pwd")
        });

        sent.ShouldBe("pwd");
    }

    [Fact]
    public async Task InsertTransactionFromApiAsync_Should_Send_Plain_Password_When_Stored_Encrypted()
    {
        CreateTimeAttApi(password: IntegrationSecret.Protect("pwd"));
        string sent = null;
        var service = CreateService(CapturePasswordHandler(p => sent = p));

        await RunInUnitOfWorkAsync(() => service.InsertTransactionFromApiAsync(
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), "", ""));

        sent.ShouldBe("pwd");
    }

    [Fact]
    public async Task InsertTransactionFromApiAsync_Should_Send_Legacy_Plain_Text_Password_Unchanged()
    {
        CreateTimeAttApi(password: "pwd");
        string sent = null;
        var service = CreateService(CapturePasswordHandler(p => sent = p));

        await RunInUnitOfWorkAsync(() => service.InsertTransactionFromApiAsync(
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), "", ""));

        sent.ShouldBe("pwd");
    }

    [Fact]
    public async Task GetCheckInOutDataListAsync_Should_Follow_Next_Pagination_Link()
    {
        var handler = new RoutingHttpMessageHandler(request =>
        {
            var url = request.RequestUri!.ToString();
            if (url.Contains("page=2"))
            {
                return JsonResponse(HttpStatusCode.OK,
                    "{\"count\":2,\"next\":null,\"previous\":null,\"msg\":\"\",\"code\":0,\"data\":[]}");
            }

            return JsonResponse(HttpStatusCode.OK,
                "{\"count\":2,\"next\":\"http://fake-vendor/api/transactions/?page=2\",\"previous\":null,\"msg\":\"\",\"code\":0,\"data\":[]}");
        });
        var service = CreateService(handler);

        var pages = await service.GetCheckInOutDataListAsync("tok", "http://fake-vendor/api/transactions/?page=1");

        pages.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetDevicesDataAsync_Should_Deserialize_Devices_Page()
    {
        var handler = new RoutingHttpMessageHandler(request => JsonResponse(HttpStatusCode.OK,
            "{\"count\":1,\"next\":null,\"previous\":null,\"msg\":\"\",\"code\":0,\"data\":[{\"id\":1,\"sn\":\"SN1\",\"ip_address\":\"10.0.0.5\",\"alias\":\"Gate1\"}]}"));
        var service = CreateService(handler);

        var devices = await service.GetDevicesDataAsync("tok", "http://fake-vendor/api/devices/?page=1");

        devices.ShouldNotBeNull();
        devices.data.Count.ShouldBe(1);
        devices.data[0].sn.ShouldBe("SN1");
        devices.data[0].ip_address.ShouldBe("10.0.0.5");
    }

    [Fact]
    public async Task InsertTransactionFromApiAsync_Should_Create_TimeEvent_From_Vendor_Api_Data()
    {
        CreateTimeAttApi();
        var employeeId = CreateEmployee("E001");
        var transTypeInId = CreateTransType("I");

        var handler = new RoutingHttpMessageHandler(request =>
        {
            var url = request.RequestUri!.ToString();
            if (url.Contains("/token/"))
            {
                return JsonResponse(HttpStatusCode.OK, "{\"token\":\"tok-abc\"}");
            }

            if (url.Contains("/devices/"))
            {
                return JsonResponse(HttpStatusCode.OK,
                    "{\"count\":1,\"next\":null,\"previous\":null,\"msg\":\"\",\"code\":0," +
                    "\"data\":[{\"id\":1,\"sn\":\"SN1\",\"ip_address\":\"10.0.0.5\",\"alias\":\"Gate1\"}]}");
            }

            if (url.Contains("/transactions/"))
            {
                return JsonResponse(HttpStatusCode.OK,
                    "{\"count\":1,\"next\":null,\"previous\":null,\"msg\":\"\",\"code\":0," +
                    "\"data\":[{\"id\":555,\"emp_code\":\"E001\",\"punch_time\":\"2026-01-01 08:00:00\"," +
                    "\"punch_state\":\"I\",\"terminal_sn\":\"SN1\",\"terminal_alias\":\"Gate1\"}]}");
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var service = CreateService(handler);

        var insertedCount = await RunInUnitOfWorkAsync(() => service.InsertTransactionFromApiAsync(
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 1), "", ""));

        insertedCount.ShouldBe(1);

        UsingDbContext(context =>
        {
            var timeEvent = context.Set<TimeEvent>().Single(e => e.RecordSource == 555);
            timeEvent.EmployeeId.ShouldBe(employeeId);
            timeEvent.TransTypeId.ShouldBe(transTypeInId);
            timeEvent.TransSource.ShouldBe(TransSource.Machine);
            timeEvent.IPAddress.ShouldBe("10.0.0.5");

            var accessUnit = context.Set<AccessUnit>().Single(a => a.IPAddress == "10.0.0.5");
            accessUnit.NameEn.ShouldBe("Gate1");
        });

        // Re-running with the same RecordSource must not create a duplicate.
        var secondRun = await RunInUnitOfWorkAsync(() => service.InsertTransactionFromApiAsync(
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 1), "", ""));

        secondRun.ShouldBe(0);
        UsingDbContext(context =>
        {
            context.Set<TimeEvent>().Count(e => e.RecordSource == 555).ShouldBe(1);
        });
    }

    // ----------------------------------------------------------------------------------------------
    // Follow-up correction wave: DumpAttendanceDataAsync / TransferTransaction(ByParam)Async /
    // FetchMissingTimeAttendanceDataAsync (OleDb "AccessControl" integration), ImportTransactionsAsync
    // (ClosedXML Excel import), ImportTransactionFromMySqlAsync (MySqlConnector integration).
    //
    // These methods require a real, caller-supplied external database connection to do their actual I/O, so
    // the tests below only exercise the parts that do NOT require a live external MySQL/AccessControl
    // database connection: connection-string validation, ManageTimeSettings gating (which all no-op / return
    // before ever touching the external connection), and -- for the Excel importer, which has no live-database
    // dependency at all -- full round-trip parsing logic against a real ClosedXML-written .xlsx file.
    // ----------------------------------------------------------------------------------------------

    [Fact]
    public async Task DumpAttendanceDataAsync_Should_Throw_When_ConnectionString_Blank()
    {
        var service = CreateService();

        await Should.ThrowAsync<ArgumentException>(() => service.DumpAttendanceDataAsync(""));
    }

    [Fact]
    public async Task DumpAttendanceDataAsync_Should_NoOp_When_Setting_Disabled()
    {
        // No ManageTimeSettings row exists yet (EnableMiddleTierTimeEvents defaults to disabled) -- the method
        // must return without ever attempting to open the (fake, unreachable) OleDb connection.
        var service = CreateService();

        await RunInUnitOfWorkAsync(async () =>
        {
            await service.DumpAttendanceDataAsync("Provider=SQLOLEDB;Data Source=unreachable-host;");
            return true;
        });
    }

    [Fact]
    public async Task TransferTransactionAsync_Should_Throw_When_ConnectionString_Blank()
    {
        var service = CreateService();

        await Should.ThrowAsync<ArgumentException>(() => service.TransferTransactionAsync(" "));
    }

    [Fact]
    public async Task TransferTransactionByParamAsync_Should_Return_Zero_When_Integration_Disabled()
    {
        // No ManageTimeSettings row exists yet (EnableTimeAttendanceIntegration defaults to disabled) -- must
        // return 0 without ever attempting to open the (fake, unreachable) OleDb connection.
        var service = CreateService();

        var result = await RunInUnitOfWorkAsync(() => service.TransferTransactionByParamAsync(
            "Provider=SQLOLEDB;Data Source=unreachable-host;", "", ""));

        result.ShouldBe(0);
    }

    [Fact]
    public async Task FetchMissingTimeAttendanceDataAsync_Should_NoOp_When_Both_Filters_Blank()
    {
        EnableApiIntegration();
        UsingDbContext(context =>
        {
            var settings = context.Set<ManageTimeSettings>().Single();
            settings.EnableTimeAttendanceIntegration = true;
        });

        var service = CreateService();

        // Both accessIdList and ipAddress are blank -- must return without ever attempting to open the (fake,
        // unreachable) OleDb connection, matching legacy's own early-out.
        await RunInUnitOfWorkAsync(async () =>
        {
            await service.FetchMissingTimeAttendanceDataAsync(
                "Provider=SQLOLEDB;Data Source=unreachable-host;", DateTime.MinValue, DateTime.MinValue, "", "");
            return true;
        });
    }

    [Fact]
    public async Task FetchMissingTimeAttendanceDataAsync_Should_Throw_When_ConnectionString_Blank()
    {
        var service = CreateService();

        await Should.ThrowAsync<ArgumentException>(() => service.FetchMissingTimeAttendanceDataAsync(
            null, DateTime.MinValue, DateTime.MinValue, "123", ""));
    }

    [Fact]
    public async Task ImportTransactionsAsync_Should_Return_Zero_When_ImportPath_Not_Configured()
    {
        var service = CreateService();

        var result = await RunInUnitOfWorkAsync(() => service.ImportTransactionsAsync());

        result.ShouldBe(0);
    }

    [Fact]
    public async Task ImportTransactionsAsync_Should_Import_Rows_From_Excel_File()
    {
        var employeeId = CreateEmployee("E100");
        var transTypeInId = CreateTransType("1");

        var importDir = Path.Combine(Path.GetTempPath(), "HCMv3TaiTests_" + Guid.NewGuid());
        Directory.CreateDirectory(importDir);
        try
        {
            var filePath = Path.Combine(importDir, "punches.xlsx");
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Sheet1");

                // Header row (row 1) -- skipped by the importer, content irrelevant.
                worksheet.Cell(1, 1).Value = "Header";

                // Data row (row 2): DevExpress column index -> Excel column (devIdx + 1).
                worksheet.Cell(2, 2).Value = "EMPCODE1"; // devIdx 1: EmpCode (unused for lookup)
                worksheet.Cell(2, 3).Value = "E100"; // devIdx 2: PIN
                worksheet.Cell(2, 4).Value = "IN"; // devIdx 3: TransType text
                worksheet.Cell(2, 5).Value = "IN"; // devIdx 4: legacy dead "ActTransType" column
                worksheet.Cell(2, 6).Value = "2026-02-01"; // devIdx 5: TransDate
                worksheet.Cell(2, 7).Value = "2026-02-01 08:00:00"; // devIdx 6: TransTime
                worksheet.Cell(2, 8).Value = "9001"; // devIdx 7: RecordSource

                workbook.SaveAs(filePath);
            }

            UsingDbContext(context =>
            {
                context.Set<ManageTimeSettings>().Add(new ManageTimeSettings
                {
                    TenantId = AbpSession.TenantId ?? 1,
                    TimeEventImportPath = importDir
                });
            });

            var service = CreateService();

            var insertedCount = await RunInUnitOfWorkAsync(() => service.ImportTransactionsAsync());

            insertedCount.ShouldBe(1);

            UsingDbContext(context =>
            {
                var timeEvent = context.Set<TimeEvent>().Single(e => e.RecordSource == 9001);
                timeEvent.EmployeeId.ShouldBe(employeeId);
                timeEvent.TransTypeId.ShouldBe(transTypeInId);
                timeEvent.TransSource.ShouldBe(TransSource.System);
            });

            // Re-running against the same file must not create a duplicate (same RecordSource).
            var secondRun = await RunInUnitOfWorkAsync(() => service.ImportTransactionsAsync());

            secondRun.ShouldBe(0);
            UsingDbContext(context =>
            {
                context.Set<TimeEvent>().Count(e => e.RecordSource == 9001).ShouldBe(1);
            });
        }
        finally
        {
            Directory.Delete(importDir, recursive: true);
        }
    }

    [Fact]
    public async Task ImportTransactionFromMySqlAsync_Should_Throw_When_ConnectionString_Blank()
    {
        var service = CreateService();

        await Should.ThrowAsync<ArgumentException>(() => service.ImportTransactionFromMySqlAsync(""));
    }

    [Fact]
    public async Task ImportTransactionFromMySqlAsync_Should_Return_Zero_When_Integration_Disabled()
    {
        // No ManageTimeSettings row exists yet (EnableMySqlIntegration defaults to disabled) -- must return 0
        // without ever attempting to open the (fake, unreachable) MySQL connection.
        var service = CreateService();

        var result = await RunInUnitOfWorkAsync(() => service.ImportTransactionFromMySqlAsync(
            "Server=unreachable-host;Database=attendance;Uid=x;Pwd=x;"));

        result.ShouldBe(0);
    }
}
