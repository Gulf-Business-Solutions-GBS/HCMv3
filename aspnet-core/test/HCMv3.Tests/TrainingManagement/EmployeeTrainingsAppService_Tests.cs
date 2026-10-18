using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.TrainingManagement;
using HCMv3.TrainingManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TrainingManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeTrainingsAppService_Tests : TrainingManagementAppServiceTestBase
{
    private EmployeeTrainingsAppService CreateAppService()
    {
        return CreateService(() => new EmployeeTrainingsAppService(
            FakeRepository.For<EmployeeTraining>(Context),
            FakeRepository.For<EmployeeCertification>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_EmployeeTraining()
    {
        var employeeId = CreateEmployee();
        var courseId = CreateTrainingCourse();
        var scheduleId = CreateTrainingSchedule(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10), courseId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeTrainingDto
        {
            EmployeeId = employeeId,
            TrainingCourseId = courseId,
            TrainingScheduleId = scheduleId,
            CompletionStatus = TrainingCompletionStatus.Pending,
            Score = 0
        });

        var result = await service.GetAll(new GetAllEmployeeTrainingsInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].TrainingScheduleId.ShouldBe(scheduleId);
        result.Items[0].CompletionStatus.ShouldBe(TrainingCompletionStatus.Pending);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Employee_Course_Schedule_Combination()
    {
        var employeeId = CreateEmployee();
        var courseId = CreateTrainingCourse();
        var scheduleId = CreateTrainingSchedule(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10), courseId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeTrainingDto
        {
            EmployeeId = employeeId,
            TrainingCourseId = courseId,
            TrainingScheduleId = scheduleId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmployeeTrainingDto
            {
                EmployeeId = employeeId,
                TrainingCourseId = courseId,
                TrainingScheduleId = scheduleId
            }));
    }

    [Fact]
    public async Task Should_Save_Training_Certification_Link_And_Show_It_In_List_And_Edit()
    {
        var employeeId = CreateEmployee();
        var courseId = CreateTrainingCourse();
        var scheduleId = CreateTrainingSchedule(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10), courseId);
        var certificationId = CreateEmployeeCertification(employeeId, CertificationType.Training, "First Aid");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeTrainingDto
        {
            EmployeeId = employeeId,
            TrainingCourseId = courseId,
            TrainingScheduleId = scheduleId,
            EmployeeCertificationId = certificationId
        });

        var list = await service.GetAll(new GetAllEmployeeTrainingsInput { EmployeeId = employeeId });
        list.Items[0].EmployeeCertificationId.ShouldBe(certificationId);
        list.Items[0].EmployeeCertificationName.ShouldBe("First Aid");

        var forEdit = await service.GetEmployeeTrainingForEdit(new EntityDto(list.Items[0].Id));
        forEdit.EmployeeTraining.EmployeeCertificationId.ShouldBe(certificationId);
    }

    [Fact]
    public async Task Should_Reject_A_Professional_Certification()
    {
        var employeeId = CreateEmployee();
        var courseId = CreateTrainingCourse();
        var scheduleId = CreateTrainingSchedule(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10), courseId);
        var certificationId = CreateEmployeeCertification(employeeId, CertificationType.Professional);
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmployeeTrainingDto
            {
                EmployeeId = employeeId,
                TrainingCourseId = courseId,
                TrainingScheduleId = scheduleId,
                EmployeeCertificationId = certificationId
            }));
    }

    [Fact]
    public async Task Should_Reject_Another_Employees_Certification()
    {
        var employeeId = CreateEmployee();
        var otherEmployeeId = CreateEmployee();
        var courseId = CreateTrainingCourse();
        var scheduleId = CreateTrainingSchedule(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10), courseId);
        var certificationId = CreateEmployeeCertification(otherEmployeeId, CertificationType.Training);
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmployeeTrainingDto
            {
                EmployeeId = employeeId,
                TrainingCourseId = courseId,
                TrainingScheduleId = scheduleId,
                EmployeeCertificationId = certificationId
            }));
    }

    [Fact]
    public async Task Should_Offer_Only_The_Employees_Training_Certifications()
    {
        var employeeId = CreateEmployee();
        var otherEmployeeId = CreateEmployee();
        var training = CreateEmployeeCertification(employeeId, CertificationType.Training, "Training Cert");
        CreateEmployeeCertification(employeeId, CertificationType.Professional, "Professional Cert");
        CreateEmployeeCertification(otherEmployeeId, CertificationType.Training, "Other Cert");
        var service = CreateAppService();

        var result = await service.GetTrainingCertifications(new GetEmployeeTrainingCertificationsInput { EmployeeId = employeeId });

        result.Items.Select(i => i.Id).ShouldBe(new[] { training });
        result.Items[0].DisplayName.ShouldBe("Training Cert");
    }

    [Fact]
    public async Task Should_Keep_The_Current_Certification_In_The_Picker_Even_If_Not_Training()
    {
        var employeeId = CreateEmployee();
        var professional = CreateEmployeeCertification(employeeId, CertificationType.Professional, "Professional Cert");
        var service = CreateAppService();

        var result = await service.GetTrainingCertifications(new GetEmployeeTrainingCertificationsInput
        {
            EmployeeId = employeeId,
            CurrentCertificationId = professional
        });

        result.Items.Select(i => i.Id).ShouldContain(professional);
    }

    [Fact]
    public async Task Should_Fall_Back_To_Course_When_Certificate_Name_Is_Blank()
    {
        var employeeId = CreateEmployee();
        CreateEmployeeCertification(employeeId, CertificationType.Training, certificate: null, course: "Safety Course");
        var service = CreateAppService();

        var result = await service.GetTrainingCertifications(new GetEmployeeTrainingCertificationsInput { EmployeeId = employeeId });

        result.Items[0].DisplayName.ShouldBe("Safety Course");
    }
}
