namespace HCMv3.SurveyManagement;

/// <summary>
/// Rendering type for a <c>SurveyDetail</c> question (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SurveyManagement.QuestionType</c>). The legacy enum also declared
/// commented-out <c>DropdownList</c>/<c>Date</c>/<c>Time</c>/<c>Number</c> members that were never enabled --
/// they are intentionally NOT ported since they were dead/inactive in the source.
/// </summary>
public enum QuestionType
{
    TextBox = 0,
    RadioList = 1,
    CheckboxList = 2
}
