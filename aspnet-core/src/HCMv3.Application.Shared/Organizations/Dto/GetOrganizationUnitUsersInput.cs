using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;
using HCMv3.Common;

namespace HCMv3.Organizations.Dto;

public class GetOrganizationUnitUsersInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    [Range(1, long.MaxValue)]
    public long Id { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrEmpty(Sorting))
        {
            Sorting = "user.Name, user.Surname";
        }

        Sorting = DtoSortingHelper.ReplaceSorting(Sorting, s =>
        {
            if (s.Contains("userName"))
            {
                s = s.Replace("userName", "user.userName");
            }

            if (s.Contains("addedTime"))
            {
                s = s.Replace("addedTime", "ouUser.creationTime");
            }

            return s;
        });
    }
}

