using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;
using HCMv3.Common;

namespace HCMv3.Organizations.Dto;

public class GetOrganizationUnitRolesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    [Range(1, long.MaxValue)]
    public long Id { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrEmpty(Sorting))
        {
            Sorting = "role.DisplayName, role.Name";
        }

        Sorting = DtoSortingHelper.ReplaceSorting(Sorting, s =>
        {
            if (s.Contains("displayName"))
            {
                s = s.Replace("displayName", "role.displayName");
            }

            if (s.Contains("addedTime"))
            {
                s = s.Replace("addedTime", "ouRole.creationTime");
            }

            return s;
        });
    }
}

