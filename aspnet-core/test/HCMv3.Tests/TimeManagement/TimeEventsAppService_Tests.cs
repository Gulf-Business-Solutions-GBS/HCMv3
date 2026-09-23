using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class TimeEventsAppService_Tests : TimeManagementAppServiceTestBase
{
    private readonly IBinaryObjectManager _binaryObjects = Substitute.For<IBinaryObjectManager>();
    private readonly HCMv3.SystemSettings.Services.ISysSettingsAccessor _sysSettingsAccessor =
        Substitute.For<HCMv3.SystemSettings.Services.ISysSettingsAccessor>();

    private TimeEventsAppService CreateAppService(bool allowDuplicateAcrossSources = false)
    {
        _sysSettingsAccessor.GetTimeSettingsAsync().Returns(Task.FromResult(new HCMv3.SystemSettings.ManageTimeSettings
        {
            TenantId = AbpSession.TenantId ?? 1,
            AllowDuplicateInTimeEvent = allowDuplicateAcrossSources
        }));

        return CreateService(() => new TimeEventsAppService(
            FakeRepository.For<TimeEvent>(Context), _binaryObjects, _sysSettingsAccessor));
    }

    /// <summary>
    /// Legacy #4164: with <c>ManageTimeSettings.AllowDuplicateInTimeEvent</c> on, the same punch from a
    /// DIFFERENT source is not a duplicate; from the same source it still is.
    /// </summary>
    [Fact]
    public async Task With_AllowDuplicateInTimeEvent_On_Should_Accept_The_Same_Punch_From_Another_Source_Only()
    {
        var employeeId = CreateEmployee("Dup");
        var transTypeId = CreateTransType("DUP");
        var service = CreateAppService(allowDuplicateAcrossSources: true);

        var transDate = new DateTime(2026, 2, 1);
        var transTime = new DateTime(2026, 2, 1, 8, 0, 0);

        await service.CreateOrEdit(new CreateOrEditTimeEventDto
        {
            EmployeeId = employeeId,
            TransTypeId = transTypeId,
            TransDate = transDate,
            TransTime = transTime,
            TransSource = TransSource.Manual
        });

        // Same punch, other source: allowed.
        await service.CreateOrEdit(new CreateOrEditTimeEventDto
        {
            EmployeeId = employeeId,
            TransTypeId = transTypeId,
            TransDate = transDate,
            TransTime = transTime,
            TransSource = TransSource.Machine,
            RecordSource = 777
        });

        // Same punch, same source: still a duplicate.
        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditTimeEventDto
            {
                EmployeeId = employeeId,
                TransTypeId = transTypeId,
                TransDate = transDate,
                TransTime = transTime,
                TransSource = TransSource.Manual
            }));
    }

    private int CreateNationality()
    {
        var entity = new HCMv3.GeneralMasterInfo.Nationality
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "Local",
            NameAr = "محلي"
        };
        Context.Nationalities.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateEmployee(string firstName)
    {
        var entity = new Employee
        {
            TenantId = AbpSession.TenantId ?? 1,
            FirstNameEn = firstName,
            LastNameEn = "Doe",
            FirstNameAr = firstName,
            LastNameAr = "Doe",
            FullNameEn = firstName,
            FullNameAr = firstName,
            NationalityId = CreateNationality()
        };
        Context.Employees.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateTransType(string code)
    {
        var entity = new TransType
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.TransTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_TimeEvent()
    {
        var employeeId = CreateEmployee("John");
        var transTypeId = CreateTransType("IN");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTimeEventDto
        {
            EmployeeId = employeeId,
            TransTypeId = transTypeId,
            TransDate = new DateTime(2026, 1, 1),
            TransTime = new DateTime(2026, 1, 1, 8, 0, 0),
            TransSource = TransSource.Manual
        });

        var result = await service.GetAll(new GetAllTimeEventsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
        result.Items[0].TransTypeId.ShouldBe(transTypeId);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Employee_TransType_TransDate_TransTime()
    {
        var employeeId = CreateEmployee("Jane");
        var transTypeId = CreateTransType("OUT");
        var service = CreateAppService();

        var transDate = new DateTime(2026, 1, 1);
        var transTime = new DateTime(2026, 1, 1, 17, 0, 0);

        await service.CreateOrEdit(new CreateOrEditTimeEventDto
        {
            EmployeeId = employeeId,
            TransTypeId = transTypeId,
            TransDate = transDate,
            TransTime = transTime,
            TransSource = TransSource.Manual
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditTimeEventDto
            {
                EmployeeId = employeeId,
                TransTypeId = transTypeId,
                TransDate = transDate,
                TransTime = transTime,
                TransSource = TransSource.Manual
            }));
    }

    [Fact]
    public async Task Should_Throw_When_Machine_Sourced_RecordSource_Is_Blank()
    {
        var employeeId = CreateEmployee("Alex");
        var transTypeId = CreateTransType("IN2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditTimeEventDto
            {
                EmployeeId = employeeId,
                TransTypeId = transTypeId,
                TransDate = new DateTime(2026, 1, 2),
                TransTime = new DateTime(2026, 1, 2, 8, 0, 0),
                TransSource = TransSource.Machine,
                RecordSource = 0
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Machine_RecordSource()
    {
        var employeeId = CreateEmployee("Sam");
        var transTypeId = CreateTransType("IN3");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTimeEventDto
        {
            EmployeeId = employeeId,
            TransTypeId = transTypeId,
            TransDate = new DateTime(2026, 1, 3),
            TransTime = new DateTime(2026, 1, 3, 8, 0, 0),
            TransSource = TransSource.Machine,
            RecordSource = 12345
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditTimeEventDto
            {
                EmployeeId = employeeId,
                TransTypeId = transTypeId,
                TransDate = new DateTime(2026, 1, 3),
                TransTime = new DateTime(2026, 1, 3, 9, 0, 0),
                TransSource = TransSource.Machine,
                RecordSource = 12345
            }));
    }

    private int CreateGeoPunch(string geoSelfiePath)
    {
        var entity = new TimeEvent
        {
            TenantId = 1,
            EmployeeId = CreateEmployee("Geo"),
            TransTypeId = CreateTransType("GEO" + Guid.NewGuid().ToString("N")[..4]),
            TransDate = new DateTime(2026, 1, 4),
            TransTime = new DateTime(2026, 1, 4, 8, 0, 0),
            TransSource = TransSource.ESS,
            GeoSelfiePath = geoSelfiePath
        };
        Context.TimeEvents.Add(entity);
        Context.SaveChanges();

        return entity.Id;
    }

    [Fact]
    public async Task Should_Return_The_Geo_Punch_Selfie_As_A_Data_Url()
    {
        var selfieId = Guid.NewGuid();
        var eventId = CreateGeoPunch(selfieId.ToString());
        _binaryObjects.GetOrNullAsync(selfieId)
            .Returns(new BinaryObject(1, new byte[] { 1, 2, 3 }, "GeoPunchSelfie|image/png") { Id = selfieId });

        var selfie = await CreateAppService().GetTimeEventSelfie(new Abp.Application.Services.Dto.EntityDto(eventId));

        selfie.DataUrl.ShouldBe("data:image/png;base64,AQID");
    }

    [Fact]
    public async Task Should_Return_No_Selfie_For_A_Punch_Without_One()
    {
        var eventId = CreateGeoPunch(null);

        var selfie = await CreateAppService().GetTimeEventSelfie(new Abp.Application.Services.Dto.EntityDto(eventId));

        selfie.DataUrl.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Return_No_Selfie_When_The_File_Is_Gone()
    {
        var eventId = CreateGeoPunch(Guid.NewGuid().ToString());

        var selfie = await CreateAppService().GetTimeEventSelfie(new Abp.Application.Services.Dto.EntityDto(eventId));

        selfie.DataUrl.ShouldBeNull();
    }
}
