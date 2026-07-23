using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing employees.
/// </summary>
public class GetAllEmployeesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Free-text filter matched against Code / FullNameEn / FullNameAr / Email / CivilID.
    /// </summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Code ASC";
        }
    }
}
