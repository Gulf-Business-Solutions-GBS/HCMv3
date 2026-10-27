using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.TrainingReport;
using HCMv3.GridReports.TrainingReport.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.TrainingManagement;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class EmployeeTrainingReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeTrainingReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeTrainingReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeTrainingReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F" + code,
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "Full " + code,
                FullNameAr = "عربي " + code,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id,
                Status = status
            }).Entity;
        });

        return employee;
    }

    private TrainingCourse CreateTrainingCourse(string code)
    {
        TrainingCourse course = null;
        UsingDbContext(context =>
        {
            course = context.Set<TrainingCourse>().Add(new TrainingCourse
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar",
                Active = true
            }).Entity;
        });
        return course;
    }

    private TrainingSchedule CreateTrainingSchedule(TrainingCourse course)
    {
        TrainingSchedule schedule = null;
        UsingDbContext(context =>
        {
            schedule = context.Set<TrainingSchedule>().Add(new TrainingSchedule
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "S",
                NameAr = "S",
                TrainingCourseId = course.Id,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 1, 5)
            }).Entity;
        });
        return schedule;
    }

    private void CreateEmployeeTraining(Employee employee, TrainingCourse course, TrainingSchedule schedule, double score)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeTraining>().Add(new EmployeeTraining
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                TrainingCourseId = course.Id,
                TrainingScheduleId = schedule.Id,
                CompletionStatus = TrainingCompletionStatus.Complete,
                Score = score
            });
        });
    }

    private static GetEmployeeTrainingReportInput Input(string employeeCode = null, string trainingCourseCode = null)
    {
        return new GetEmployeeTrainingReportInput
        {
            EmployeeCode = employeeCode,
            TrainingCourseCode = trainingCourseCode,
            MaxResultCount = 100,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Filter_By_Training_Course_Code()
    {
        var courseA = CreateTrainingCourse("TC-A");
        var courseB = CreateTrainingCourse("TC-B");
        var scheduleA = CreateTrainingSchedule(courseA);
        var scheduleB = CreateTrainingSchedule(courseB);
        var empA = CreateEmployee("TRN-A");
        var empB = CreateEmployee("TRN-B");

        CreateEmployeeTraining(empA, courseA, scheduleA, 80);
        CreateEmployeeTraining(empB, courseB, scheduleB, 90);

        var result = await _appService.GetEmployeeTrainingReport(Input(trainingCourseCode: "TC-A"));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("TRN-A");
        codes.ShouldNotContain("TRN-B");
    }

    [Fact]
    public async Task Should_Filter_By_Comma_Separated_Employee_Codes()
    {
        var course = CreateTrainingCourse("TC-CODE");
        var schedule = CreateTrainingSchedule(course);
        var first = CreateEmployee("TRC-A");
        var second = CreateEmployee("TRC-B");
        var third = CreateEmployee("TRC-C");

        CreateEmployeeTraining(first, course, schedule, 70);
        CreateEmployeeTraining(second, course, schedule, 75);
        CreateEmployeeTraining(third, course, schedule, 85);

        var result = await _appService.GetEmployeeTrainingReport(Input(employeeCode: "TRC-A,TRC-C"));

        var codes = result.Items.Select(i => i.EmployeeCode).OrderBy(c => c).ToList();
        codes.ShouldBe(new List<string> { "TRC-A", "TRC-C" });
    }

    /// <summary>
    /// LEGACY QUIRK pinning test: FromDate/ToDate are accepted but never actually filter the row set (see
    /// GetEmployeeTrainingReportInput remarks) -- a window that excludes "today" (when the training record
    /// was created) must still return the row.
    /// </summary>
    [Fact]
    public async Task Should_Reproduce_The_Legacy_Unfiltered_Date_Range_Behavior()
    {
        var course = CreateTrainingCourse("TC-DATE");
        var schedule = CreateTrainingSchedule(course);
        var employee = CreateEmployee("TRN-DATE");
        CreateEmployeeTraining(employee, course, schedule, 60);

        var input = Input(employeeCode: "TRN-DATE");
        input.FromDate = new DateTime(2000, 1, 1);
        input.ToDate = new DateTime(2000, 1, 2);

        var result = await _appService.GetEmployeeTrainingReport(input);

        result.Items.Select(i => i.EmployeeCode).ShouldContain("TRN-DATE");
    }

    [Fact]
    public async Task Should_Resolve_Every_Export_Header_To_Localized_Text_Not_A_Missing_Key_Placeholder()
    {
        var course = CreateTrainingCourse("TC-HDR");
        var schedule = CreateTrainingSchedule(course);
        var employee = CreateEmployee("TRN-HDR");
        CreateEmployeeTraining(employee, course, schedule, 65);

        var file = await _appService.GetEmployeeTrainingReportToExcel(Input(employeeCode: "TRN-HDR"));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        foreach (var header in exported.First().Keys)
        {
            header.ShouldNotStartWith("[");
        }
    }
}
