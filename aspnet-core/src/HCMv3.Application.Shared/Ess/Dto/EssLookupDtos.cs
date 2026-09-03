using Abp.Application.Services.Dto;

namespace HCMv3.Ess.Dto;

/// <summary>
/// A colleague the employee may hand their approval duties to, as the delegation picker shows them.
///
/// <para>
/// Deliberately NOT <c>EmployeeDto</c>. That DTO carries birth dates, contact details, nationality and
/// identifiers -- an HR record. Choosing who covers for you next week needs a name and a staff number and
/// nothing else, so this is the whole of what an employee is allowed to read about their colleagues
/// through self-service.
/// </para>
/// </summary>
public class EssColleagueLookupDto : EntityDto<int>
{
    /// <summary>Staff number. Shown alongside the name because names repeat and staff numbers do not.</summary>
    public string Code { get; set; }

    public string FullNameEn { get; set; }

    public string FullNameAr { get; set; }
}
