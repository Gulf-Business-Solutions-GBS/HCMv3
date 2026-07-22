using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing organization bank info records.
/// </summary>
public class GetAllOrganizationBankInfosInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Free-text filter matched against AccountNo / SC_Code.
    /// </summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id ASC";
        }
    }
}
