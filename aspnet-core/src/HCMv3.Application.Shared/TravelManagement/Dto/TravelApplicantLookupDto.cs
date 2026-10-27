using Abp.Application.Services.Dto;

namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// One job applicant in the Employee/Applicant choice on the travel and travel feedback forms. Served by
/// <c>ITravelsAppService.GetApplicantsForLookup</c> so the travel desk does not need the Recruitment
/// permissions to fill in the drop-down.
/// </summary>
public class TravelApplicantLookupDto : EntityDto
{
    public string Code { get; set; }

    public string FullNameEn { get; set; }

    public string FullNameAr { get; set; }
}
