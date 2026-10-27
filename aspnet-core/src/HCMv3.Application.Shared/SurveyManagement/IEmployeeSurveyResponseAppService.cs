using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.SurveyManagement.Dto;

namespace HCMv3.SurveyManagement;

/// <summary>
/// The "take a survey" screen, ported from legacy
/// <c>HCM10.Web/UserControls/SurveyManagement/uscEmployeeSurvey.ascx.cs</c>.
///
/// <para>This is a genuine gap over the existing SurveyManagement CRUD services: <c>EmployeeSurveys</c>
/// /<c>EmployeeSurveyDetails</c> expose row-by-row maintenance of stored responses, whereas this service
/// renders a whole questionnaire for one (employee, survey) pair with existing answers merged in, and
/// upserts every answer in one call.</para>
/// </summary>
public interface IEmployeeSurveyResponseAppService : IApplicationService
{
    /// <summary>Legacy <c>BindSurvey</c> (lines 174-196): surveys whose date range spans "now".</summary>
    Task<List<ActiveSurveyLookupDto>> GetActiveSurveys();

    /// <summary>Legacy <c>GetData</c> (lines 197-272).</summary>
    Task<EmployeeSurveyQuestionnaireDto> GetEmployeeSurveyQuestionnaire(GetEmployeeSurveyQuestionnaireInput input);

    /// <summary>Legacy <c>SubmitSurveyReponse</c> (lines 273-343).</summary>
    Task SubmitEmployeeSurvey(SubmitEmployeeSurveyInput input);
}
