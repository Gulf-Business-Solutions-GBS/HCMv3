using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeDisciplinaryDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public long? OrganizationUnitId { get; set; }
    public int? WorkLocationId { get; set; }
    public string WorkLocationNameEn { get; set; }
    public DateTime DisciplinaryDate { get; set; }
    public DateTime DisciplinaryTime { get; set; }
    public int DisciplinaryTypeId { get; set; }
    public string DisciplinaryTypeNameEn { get; set; }
    public int? ReportedById { get; set; }
    public string ReportedByFullNameEn { get; set; }
    public bool WLSendToEmp { get; set; }
    public bool WLRecvFromEmp { get; set; }
    public string Comments { get; set; }
    public Severity Severity { get; set; }
    public int? DisciplinaryDeductionTypeId { get; set; }
    public string DisciplinaryDeductionTypeNameEn { get; set; }
}

public class CreateOrEditEmployeeDisciplinaryDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public long? OrganizationUnitId { get; set; }

    public int? WorkLocationId { get; set; }

    [Required]
    public DateTime DisciplinaryDate { get; set; }

    [Required]
    public DateTime DisciplinaryTime { get; set; }

    [Required]
    public int DisciplinaryTypeId { get; set; }

    public int? ReportedById { get; set; }

    public bool WLSendToEmp { get; set; }

    public bool WLRecvFromEmp { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }

    public Severity Severity { get; set; }

    public int? DisciplinaryDeductionTypeId { get; set; }
}

public class GetEmployeeDisciplinaryForEditOutput
{
    public CreateOrEditEmployeeDisciplinaryDto EmployeeDisciplinary { get; set; }
}

public class GetAllEmployeeDisciplinariesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "DisciplinaryDate DESC";
        }
    }
}
