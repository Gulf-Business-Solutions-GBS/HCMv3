using System;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Runtime.Validation;

namespace HCMv3.Authorization.Users.Dto;

public class GetLoginAttemptsInput : PagedAndSortedResultRequestDto, IGetLoginAttemptsInput, IShouldNormalize
{
    public string Filter { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public AbpLoginResultType? Result { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrEmpty(Sorting))
        {
            Sorting = "CreationTime DESC";
        }

        Filter = Filter?.Trim();
    }
}

