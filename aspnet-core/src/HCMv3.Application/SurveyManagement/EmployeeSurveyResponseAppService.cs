using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SurveyManagement.Dto;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.SurveyManagement;

/// <inheritdoc cref="IEmployeeSurveyResponseAppService"/>
/// <remarks>
/// <para><b>NOT ported (WebForms presentation only):</b> the entire <c>EditorTemplate : ITemplate</c> class
/// (legacy lines 542-777) that built an <c>ASPxTextBox</c>/<c>ASPxRadioButtonList</c>/<c>ASPxCheckBoxList</c>
/// per grid row, plus the four dead editor factories (ComboBox / DateEdit / TimeEdit / SpinEdit) that could
/// never be reached because their <c>QuestionType</c> members were commented out in the legacy enum and are
/// likewise absent from this port's <see cref="QuestionType"/>. Editor selection is Angular's job; the
/// <see cref="QuestionType"/> travels on each question row.</para>
///
/// <para><b>NOT ported:</b> legacy stored the working <c>DataTable</c> in <c>Session["Data"]</c> and
/// re-read the grid's live editor controls on submit (lines 96-118). A REST API is stateless -- the client
/// posts the answers it holds.</para>
/// </remarks>
public class EmployeeSurveyResponseAppService : HCMv3AppServiceBase, IEmployeeSurveyResponseAppService
{
    private readonly IRepository<Survey> _surveyRepository;
    private readonly IRepository<SurveyDetail> _surveyDetailRepository;
    private readonly IRepository<EmployeeSurvey> _employeeSurveyRepository;
    private readonly IRepository<EmployeeSurveyDetail> _employeeSurveyDetailRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IEmployeeSessionService _employeeSessionService;

    public EmployeeSurveyResponseAppService(
        IRepository<Survey> surveyRepository,
        IRepository<SurveyDetail> surveyDetailRepository,
        IRepository<EmployeeSurvey> employeeSurveyRepository,
        IRepository<EmployeeSurveyDetail> employeeSurveyDetailRepository,
        IRepository<Employee> employeeRepository,
        IEmployeeSessionService employeeSessionService)
    {
        _surveyRepository = surveyRepository;
        _surveyDetailRepository = surveyDetailRepository;
        _employeeSurveyRepository = employeeSurveyRepository;
        _employeeSurveyDetailRepository = employeeSurveyDetailRepository;
        _employeeRepository = employeeRepository;
        _employeeSessionService = employeeSessionService;
    }

    /// <summary>
    /// Legacy <c>BindSurvey</c> (lines 178-179): <c>StartDate &lt;= now &amp;&amp; EndDate &gt;= now</c>.
    /// Legacy compared against <c>DateTime.Now</c> (with its time component); preserved via
    /// <see cref="Clock.Now"/>.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_SurveyManagement_EmployeeSurveys)]
    public async Task<List<ActiveSurveyLookupDto>> GetActiveSurveys()
    {
        var now = Clock.Now;

        return await Task.FromResult(_surveyRepository.GetAll()
            .Where(r => r.StartDate <= now && r.EndDate >= now)
            .OrderBy(r => r.NameEn)
            .Select(r => new ActiveSurveyLookupDto
            {
                Id = r.Id,
                Code = r.Code,
                NameEn = r.NameEn,
                NameAr = r.NameAr,
                StartDate = r.StartDate,
                EndDate = r.EndDate
            })
            .ToList());
    }

    /// <summary>
    /// Legacy <c>GetData</c> (lines 197-272): one row per <c>SurveyDetail</c> in <c>OrderNo</c> order, with
    /// the employee's stored answer merged in (or "" when there is none).
    ///
    /// <para><c>[HttpPost]</c> per this repo's NSwag rule -- without it
    /// <c>GetEmployeeSurveyQuestionnaireInput</c> would never be generated into the client proxies.</para>
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_SurveyManagement_EmployeeSurveys)]
    public async Task<EmployeeSurveyQuestionnaireDto> GetEmployeeSurveyQuestionnaire(
        GetEmployeeSurveyQuestionnaireInput input)
    {
        var employee = await ResolveEmployeeAsync(input.EmployeeId);

        var survey = await _surveyRepository.FirstOrDefaultAsync(input.SurveyId);
        if (survey == null)
        {
            throw new UserFriendlyException(L("SurveyNotFound"));
        }

        var employeeSurvey = _employeeSurveyRepository.GetAll()
            .FirstOrDefault(r => r.EmployeeId == employee.Id && r.SurveyId == survey.Id);

        var storedAnswers = employeeSurvey == null
            ? new Dictionary<int, string>()
            : _employeeSurveyDetailRepository.GetAll()
                .Where(r => r.EmployeeSurveyId == employeeSurvey.Id)
                .Select(r => new { r.QuestionId, r.Answer })
                .ToList()
                .GroupBy(r => r.QuestionId)
                // Legacy used FirstOrDefault per question (line 242); a duplicate row keeps the first.
                .ToDictionary(g => g.Key, g => g.First().Answer);

        var surveyDetails = _surveyDetailRepository.GetAll()
            .Where(r => r.SurveyId == survey.Id)
            .OrderBy(r => r.OrderNo)
            .ToList();

        var result = new EmployeeSurveyQuestionnaireDto
        {
            SurveyId = survey.Id,
            EmployeeId = employee.Id,
            SurveyNameEn = survey.NameEn,
            // Legacy's employee combo TextField was "CodeWithFullNameEn" (line 145); this port has no such
            // computed property on Employee, so the same string is composed here.
            EmployeeCodeWithName = employee.Code + " - " + employee.FullNameEn,
            AlreadySubmitted = employeeSurvey != null
        };

        var no = 0;
        foreach (var surveyDetail in surveyDetails)
        {
            no++;

            string answer;
            if (!storedAnswers.TryGetValue(surveyDetail.Id, out answer))
            {
                answer = string.Empty;
            }

            result.Questions.Add(new EmployeeSurveyQuestionDto
            {
                No = no,
                SurveyDetailId = surveyDetail.Id,
                Question = surveyDetail.Question,
                Options = surveyDetail.Options,
                QuestionType = surveyDetail.QuestionType,
                Answer = answer ?? string.Empty
            });
        }

        return result;
    }

    /// <summary>
    /// Legacy <c>SubmitSurveyReponse</c> (lines 273-343): creates the <c>EmployeeSurvey</c> header on first
    /// submit, then upserts one <c>EmployeeSurveyDetail</c> per question.
    ///
    /// <para><b>Parity notes.</b> (1) Legacy answers are stored verbatim, including empty strings -- an
    /// unanswered question still gets a row; preserved. (2) Legacy validated the posted
    /// <c>SurveyDetail</c> against the survey's OWN detail collection (line 289) and would NRE on a foreign
    /// id; this port rejects it with a <c>UserFriendlyException</c> instead. (3) Legacy's
    /// <c>SysCreator</c>/<c>SysModifier</c> stamping is superseded by ABP's audit columns.</para>
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_SurveyManagement_EmployeeSurveys)]
    public async Task SubmitEmployeeSurvey(SubmitEmployeeSurveyInput input)
    {
        var employee = await ResolveEmployeeAsync(input.EmployeeId);

        var survey = await _surveyRepository.FirstOrDefaultAsync(input.SurveyId);
        if (survey == null)
        {
            throw new UserFriendlyException(L("SurveyNotFound"));
        }

        // Legacy lines 96 / 123-127: nothing to save is a no-op with a message, not an error.
        if (input.Answers == null || input.Answers.Count == 0)
        {
            return;
        }

        var validQuestionIds = _surveyDetailRepository.GetAll()
            .Where(r => r.SurveyId == survey.Id)
            .Select(r => r.Id)
            .ToList();

        var employeeSurvey = _employeeSurveyRepository.GetAll()
            .FirstOrDefault(r => r.EmployeeId == employee.Id && r.SurveyId == survey.Id);

        if (employeeSurvey == null)
        {
            employeeSurvey = new EmployeeSurvey
            {
                TenantId = AbpSession.GetTenantId(),
                EmployeeId = employee.Id,
                SurveyId = survey.Id
            };

            // InsertAndGetIdAsync, never InsertAsync: every detail row below needs this id.
            employeeSurvey.Id = await _employeeSurveyRepository.InsertAndGetIdAsync(employeeSurvey);
        }

        var existingDetails = _employeeSurveyDetailRepository.GetAll()
            .Where(r => r.EmployeeSurveyId == employeeSurvey.Id)
            .ToList();

        foreach (var answerInput in input.Answers)
        {
            if (!validQuestionIds.Contains(answerInput.SurveyDetailId))
            {
                throw new UserFriendlyException(L("SurveyQuestionDoesNotBelongToSurvey"));
            }

            var detail = existingDetails.FirstOrDefault(r => r.QuestionId == answerInput.SurveyDetailId);

            if (detail == null)
            {
                await _employeeSurveyDetailRepository.InsertAsync(new EmployeeSurveyDetail
                {
                    TenantId = AbpSession.GetTenantId(),
                    EmployeeSurveyId = employeeSurvey.Id,
                    QuestionId = answerInput.SurveyDetailId,
                    Answer = answerInput.Answer ?? string.Empty
                });
            }
            else
            {
                detail.Answer = answerInput.Answer ?? string.Empty;
                await _employeeSurveyDetailRepository.UpdateAsync(detail);
            }
        }
    }

    /// <summary>
    /// Legacy <c>BindEmployee</c> (lines 139-173): the employee combo defaults to the logged-in user's own
    /// employee record and is DISABLED when one is found. Reproduced server-side: an explicit
    /// <c>EmployeeId</c> is honoured, otherwise the session's employee is used.
    /// </summary>
    private async Task<Employee> ResolveEmployeeAsync(int? employeeId)
    {
        if (employeeId.HasValue && employeeId.Value > 0)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(employeeId.Value);
            if (employee == null)
            {
                throw new UserFriendlyException(L("EmployeeNotFound"));
            }

            return employee;
        }

        var currentEmployee = await _employeeSessionService.GetCurrentEmployeeAsync();
        if (currentEmployee == null)
        {
            throw new UserFriendlyException(L("CurrentUserIsNotLinkedToAnEmployee"));
        }

        return currentEmployee;
    }
}
