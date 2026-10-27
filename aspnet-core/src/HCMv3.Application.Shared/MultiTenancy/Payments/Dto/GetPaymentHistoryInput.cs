using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;
using HCMv3.Common;

namespace HCMv3.MultiTenancy.Payments.Dto;

public class GetPaymentHistoryInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public void Normalize()
    {
        if (string.IsNullOrEmpty(Sorting))
        {
            Sorting = "CreationTime";
        }

        Sorting = DtoSortingHelper.ReplaceSorting(Sorting, s =>
        {
            return s.Replace("editionDisplayName", "Edition.DisplayName");
        });
    }
}

