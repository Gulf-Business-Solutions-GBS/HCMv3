using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.SystemSecurity.Dto;

public class GetAllEmailTokensInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public string Filter { get; set; }

    public int? EmailTemplateId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "TokenName";
        }
    }
}
