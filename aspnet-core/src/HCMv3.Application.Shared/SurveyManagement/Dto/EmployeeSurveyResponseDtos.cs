using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.SurveyManagement.Dto;

/// <summary>A survey currently open for responses (legacy <c>uscEmployeeSurvey.BindSurvey</c>, lines 174-196).</summary>
public class ActiveSurveyLookupDto
{
    public int Id { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}

/// <summary>Input for loading a questionnaire (legacy <c>GetData</c>, lines 197-272).</summary>
public class GetEmployeeSurveyQuestionnaireInput
{
    [Required]
    public int SurveyId { get; set; }

    /// <summary>
    /// Legacy's employee combo. When omitted the current user's own employee record is used -- legacy
    /// pre-selected it AND DISABLED the combo whenever the logged-in user mapped to an Employee via
    /// <c>LoginID</c> (lines 150-167).
    /// </summary>
    public int? EmployeeId { get; set; }
}

/// <summary>One question row of a questionnaire, mirroring legacy's <c>DataTable</c> columns.</summary>
public class EmployeeSurveyQuestionDto
{
    /// <summary>Legacy column "No" -- 1-based row number in <c>OrderNo</c> order.</summary>
    public int No { get; set; }

    /// <summary>Legacy column "SurveyDetail_OID".</summary>
    public int SurveyDetailId { get; set; }

    /// <summary>Legacy column "Question".</summary>
    public string Question { get; set; }

    /// <summary>
    /// Legacy column "Options" -- the raw option string. NOTE the legacy inconsistency, preserved: the
    /// editors split it on <b>';'</b> (semicolon) while a multi-select ANSWER is joined/split on
    /// <b>','</b> (comma). Splitting is left to the client so both separators stay verbatim.
    /// </summary>
    public string Options { get; set; }

    /// <summary>Legacy column "Editor": TextBox / RadioList / CheckboxList.</summary>
    public QuestionType QuestionType { get; set; }

    /// <summary>Legacy column "Answer" -- the employee's stored answer, or "" when unanswered.</summary>
    public string Answer { get; set; }
}

/// <summary>Output of <c>GetEmployeeSurveyQuestionnaire</c>.</summary>
public class EmployeeSurveyQuestionnaireDto
{
    public int SurveyId { get; set; }

    public int EmployeeId { get; set; }

    public string SurveyNameEn { get; set; }

    public string EmployeeCodeWithName { get; set; }

    /// <summary>True when this employee already has an <c>EmployeeSurvey</c> for this survey.</summary>
    public bool AlreadySubmitted { get; set; }

    public List<EmployeeSurveyQuestionDto> Questions { get; set; } = new List<EmployeeSurveyQuestionDto>();
}

/// <summary>One answer being submitted.</summary>
public class EmployeeSurveyAnswerInput
{
    [Required]
    public int SurveyDetailId { get; set; }

    /// <summary>
    /// For a CheckboxList legacy joined the selected values with a comma (lines 105-111); the client must
    /// do the same so stored answers stay compatible.
    /// </summary>
    [StringLength(4000)]
    public string Answer { get; set; }
}

/// <summary>Input for <c>SubmitEmployeeSurvey</c> (legacy <c>SubmitSurveyReponse</c>, lines 273-343).</summary>
public class SubmitEmployeeSurveyInput
{
    [Required]
    public int SurveyId { get; set; }

    public int? EmployeeId { get; set; }

    public List<EmployeeSurveyAnswerInput> Answers { get; set; } = new List<EmployeeSurveyAnswerInput>();
}
