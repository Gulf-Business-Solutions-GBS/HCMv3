using System.Collections.Generic;
using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;

namespace HCMv3.Authorization.Users.Dto;

public class GetUsersInput : PagedAndSortedResultRequestDto, IShouldNormalize, IGetUsersInput
{
    public string Filter { get; set; }

    public List<string> Permissions { get; set; }

    public int? Role { get; set; }

    public bool OnlyLockedUsers { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrEmpty(Sorting))
        {
            Sorting = "Name,Surname";
        }

        Filter = Filter?.Trim();
    }
}

