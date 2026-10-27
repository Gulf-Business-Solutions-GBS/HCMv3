using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IMyBankAppService"/>
///
/// <remarks>
/// The highest-stakes self-service type: this row decides where a salary is paid. Two things follow from
/// that and are deliberate here.
///
/// <para>
/// ACCOUNT NUMBERS ARE MASKED ON THE WAY OUT. The list returns only the last four characters. An employee
/// listing their own details does not need the full number read back -- they have it -- and a list endpoint
/// is the easiest place for one to leak into a log, a screenshot or a support ticket.
/// </para>
/// <para>
/// The new row is created hidden and stays hidden until approved, so an unapproved change never reaches a
/// pay run. That is the whole reason this type is worth routing rather than editing directly.
/// </para>
/// </remarks>
[AbpAuthorize(AppPermissions.Ess_MyBank)]
public class MyBankAppService : MyTransactionAppServiceBase<EmployeeBank>, IMyBankAppService
{
    public MyBankAppService(
        IRepository<EmployeeBank> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
    }

    protected override string RequestType => BankWorkflowTransactionHandler.BankRequestType;

    protected override string RequestNumberPrefix => "BANK";

    [HttpPost]
    public async Task<PagedResultDto<MyBankRequestDto>> GetMyBanks(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var today = DateTime.Today;

            var query = MyRowsQuery(employeeId)
                .Include(b => b.Bank)
                .WhereIf(!input.IncludeHistory, b => b.EndDate >= today || b.WorkflowStatus == RequestStatus.Open);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            return new PagedResultDto<MyBankRequestDto>(totalCount, items.Select(ToDto).ToList());
        }
    }

    public Task<MyRequestSubmissionDto> RequestBankChange(RequestBankChangeInput input)
    {
        // One or the other, not necessarily both: some countries pay on an account number, others on IBAN.
        // The handler enforces this too; refused here so the employee is told before the row is created.
        if (input.AccountNum.IsNullOrWhiteSpace() && input.IBAN.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("AccountNumberOrIbanIsRequired"));
        }

        return CreateAndSubmitAsync(
            employeeId => new EmployeeBank
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId,
                BankId = input.BankId,
                AccountNum = input.AccountNum?.Trim(),
                IBAN = input.IBAN?.Trim(),
                BankBranch = input.BankBranch,
                StartDate = input.StartDate.Date,

                // Open-ended until superseded. A change with an end date would silently stop paying the
                // employee on that day, which nobody asked for.
                EndDate = new DateTime(9999, 12, 31)
            },
            input.BankId);
    }

    public Task CancelMyBank(EntityDto input) => CancelAsync(input.Id);

    private static MyBankRequestDto ToDto(EmployeeBank entity)
    {
        return new MyBankRequestDto
        {
            Id = entity.Id,
            BankId = entity.BankId,
            BankNameEn = entity.Bank?.NameEn,
            BankNameAr = entity.Bank?.NameAr,
            AccountNumMasked = MaskAllButLastFour(entity.AccountNum),
            IbanMasked = MaskAllButLastFour(entity.IBAN),
            BankBranch = entity.BankBranch,
            StartDate = entity.StartDate,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity)
        };
    }

    /// <summary>
    /// Last four characters only. Enough to confirm a change against a payslip; not enough to pay anyone.
    /// </summary>
    private static string MaskAllButLastFour(string value)
    {
        if (value.IsNullOrWhiteSpace())
        {
            return null;
        }

        var trimmed = value.Trim();

        return trimmed.Length <= 4
            ? new string('*', trimmed.Length)
            : new string('*', trimmed.Length - 4) + trimmed.Substring(trimmed.Length - 4);
    }
}
