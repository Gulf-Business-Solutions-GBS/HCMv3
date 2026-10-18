using Abp.Application.Services.Dto;

namespace HCMv3.TrainingManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TrainingCourse"/>.
/// </summary>
public class TrainingCourseDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public TrainingDeliveryMethod DeliveryMethod { get; set; }

    public double Fees { get; set; }

    public bool Active { get; set; }

    public int? InstitutionId { get; set; }

    public string InstitutionName { get; set; }

    public TrainingCourseSource Source { get; set; }
}
