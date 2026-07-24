using System;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeDependantExceptionDto : EntityDto<int>
{
    public int EmployeeDependantId { get; set; }
    public string EmployeeDependantNameEn { get; set; }
    public ExceptionBenefitType ExceptionBenefitType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public double Amount { get; set; }
}

public class CreateOrEditEmployeeDependantExceptionDto
{
    public int? Id { get; set; }

    public int EmployeeDependantId { get; set; }

    public ExceptionBenefitType ExceptionBenefitType { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double Amount { get; set; }
}

public class GetEmployeeDependantExceptionForEditOutput
{
    public CreateOrEditEmployeeDependantExceptionDto EmployeeDependantException { get; set; }
}

public class GetAllEmployeeDependantExceptionsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeDependantId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}
