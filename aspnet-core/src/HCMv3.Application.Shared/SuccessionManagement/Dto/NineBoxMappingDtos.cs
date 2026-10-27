using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.SuccessionManagement.Dto;

public class NineBoxMappingDto : EntityDto<int>
{
    public int RatingId { get; set; }
    public string RatingNameEn { get; set; }
    public int NineBoxRatingId { get; set; }
    public string NineBoxRatingNameEn { get; set; }
}

public class CreateOrEditNineBoxMappingDto
{
    public int? Id { get; set; }

    [Required]
    public int RatingId { get; set; }

    [Required]
    public int NineBoxRatingId { get; set; }
}

public class GetNineBoxMappingForEditOutput
{
    public CreateOrEditNineBoxMappingDto NineBoxMapping { get; set; }
}

public class GetAllNineBoxMappingsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? RatingId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id ASC";
        }
    }
}
