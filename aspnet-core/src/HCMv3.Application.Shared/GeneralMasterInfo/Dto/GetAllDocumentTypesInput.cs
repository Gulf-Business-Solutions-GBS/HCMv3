using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing document types.
/// </summary>
public class GetAllDocumentTypesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Free-text filter matched against Code / NameEn / NameAr.
    /// </summary>
    public string Filter { get; set; }

    /// <summary>
    /// When true, returns only the types the company issues as letters. This is what the letter-request
    /// screen calls to fill its dropdown -- the HCMv3 replacement for legacy's
    /// <c>XPQuery&lt;DocumentType&gt;().Where(r =&gt; r.AllowUpdate == false)</c>.
    /// </summary>
    public bool OnlyLetterTypes { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Code ASC";
        }
    }
}
