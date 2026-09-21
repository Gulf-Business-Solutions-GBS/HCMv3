namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Output of <c>GetTimeAttAPIForEdit</c>.
/// </summary>
public class GetTimeAttAPIForEditOutput
{
    public CreateOrEditTimeAttAPIDto TimeAttAPI { get; set; }

    /// <summary>
    /// True when a password is already saved. The password itself is never returned, so the form uses
    /// this to show "leave blank to keep" instead of demanding a new one.
    /// </summary>
    public bool IsPasswordSet { get; set; }
}
