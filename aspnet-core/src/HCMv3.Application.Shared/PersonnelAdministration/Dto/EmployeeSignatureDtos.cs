using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeSignatureDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public byte[] Signature { get; set; }
}

public class CreateOrEditEmployeeSignatureDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public byte[] Signature { get; set; }
}

public class GetEmployeeSignatureForEditOutput
{
    public CreateOrEditEmployeeSignatureDto EmployeeSignature { get; set; }
}

public class GetAllEmployeeSignaturesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id ASC";
        }
    }
}
