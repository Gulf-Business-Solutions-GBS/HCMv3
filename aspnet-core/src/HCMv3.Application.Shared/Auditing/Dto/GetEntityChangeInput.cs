using System;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using HCMv3.Common;

namespace HCMv3.Auditing.Dto;

public class GetEntityChangeInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string UserName { get; set; }

    public string EntityTypeFullName { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "ChangeTime DESC";
        }

        Sorting = DtoSortingHelper.ReplaceSorting(Sorting, s =>
        {

            if (s.IndexOf("UserName", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "User." + s;
            }

            return "EntityChange." + s;
        });
    }
}

public class GetEntityTypeChangeInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public string EntityTypeFullName { get; set; }

    public string EntityId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "ChangeTime DESC";
        }

        Sorting = DtoSortingHelper.ReplaceSorting(Sorting, s =>
        {
            if (s.IndexOf("UserName", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                s = "User." + s;
            }
            else
            {
                s = "EntityChange." + s;
            }

            return s;
        });
    }
}

