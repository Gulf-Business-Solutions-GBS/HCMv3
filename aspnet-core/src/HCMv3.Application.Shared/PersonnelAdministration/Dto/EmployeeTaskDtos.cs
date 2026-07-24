using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeTaskDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int TaskTypeId { get; set; }
    public string TaskTypeNameEn { get; set; }
    public DateTime TaskDate { get; set; }
    public int TaskStatus { get; set; }
    public DateTime ReminderDate { get; set; }
    public int MonitoringType { get; set; }
    public int MonitoringCount { get; set; }
    public int MonitoringUnit { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeTaskDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int TaskTypeId { get; set; }

    [Required]
    public DateTime TaskDate { get; set; }

    public int TaskStatus { get; set; }

    public DateTime ReminderDate { get; set; }

    public int MonitoringType { get; set; }

    public int MonitoringCount { get; set; }

    public int MonitoringUnit { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

public class GetEmployeeTaskForEditOutput
{
    public CreateOrEditEmployeeTaskDto EmployeeTask { get; set; }
}

public class GetAllEmployeeTasksInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "TaskDate DESC";
        }
    }
}
