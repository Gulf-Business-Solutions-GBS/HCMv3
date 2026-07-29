using Microsoft.AspNetCore.Mvc;
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
using HCMv3.PayrollManagement.Costing;
using HCMv3.PayrollManagement.Dto;
using HCMv3.ProjectManagement;
using HCMv3.SystemSettings.Services;

namespace HCMv3.PayrollManagement;

/// <inheritdoc cref="IPostJvAppService"/>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_PostJv)]
public class PostJvAppService : HCMv3AppServiceBase, IPostJvAppService
{
    private readonly IJournalVoucherBuilder _journalVoucherBuilder;
    private readonly IFinancePostingClient _financePostingClient;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<ProjectLog> _projectLogRepository;
    private readonly ICostingFileAppService _costingFileAppService;

    public PostJvAppService(
        IJournalVoucherBuilder journalVoucherBuilder,
        IFinancePostingClient financePostingClient,
        ISysSettingsAccessor sysSettingsAccessor,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<ProjectLog> projectLogRepository,
        ICostingFileAppService costingFileAppService)
    {
        _journalVoucherBuilder = journalVoucherBuilder;
        _financePostingClient = financePostingClient;
        _sysSettingsAccessor = sysSettingsAccessor;
        _payrollAreaRepository = payrollAreaRepository;
        _projectLogRepository = projectLogRepository;
        _costingFileAppService = costingFileAppService;
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PostJv_Post)]
    public async Task<PostJvResultDto> PostJournalVoucher(CostingRunInputDto input)
    {
        var payrollArea = _payrollAreaRepository.GetAll()
            .FirstOrDefault(a => a.Name == input.PayrollAreaName);

        // Legacy lines 784-788: both FI links are hard preconditions checked BEFORE anything is built.
        if (payrollArea == null)
        {
            throw new UserFriendlyException(L("PayrollAreaNotFound"));
        }

        if (payrollArea.FICompanyId == null)
        {
            throw new UserFriendlyException(L("PayrollFICompanyNull"));
        }

        if (payrollArea.FIBankId == null)
        {
            throw new UserFriendlyException(L("PayrollFIBankNull"));
        }

        var result = await _journalVoucherBuilder.BuildAsync(CostingMapper.ToDomain(input));

        var response = new PostJvResultDto();
        var messages = new List<string>();

        // Legacy lines 818-827: when SeparateProvisionPosting is on the line set is split into two
        // journals by the BillingType column; otherwise everything goes into the single salary journal.
        // A missing settings row (fresh tenant) means the feature is off -- ISysSettingsAccessor returns
        // null in that case by design.
        var fiSettings = await _sysSettingsAccessor.GetFIPostingSettingsAsync();
        var separateProvisionPosting = fiSettings?.SeparateProvisionPosting ?? false;

        List<JournalVoucherLine> salaryLines;
        List<JournalVoucherLine> provisionLines = null;

        if (separateProvisionPosting)
        {
            salaryLines = result.Lines
                .Where(l => l.BillingType != JournalVoucherBuilder.ProvisionBillingTypeLabel)
                .ToList();
            provisionLines = result.Lines
                .Where(l => l.BillingType == JournalVoucherBuilder.ProvisionBillingTypeLabel)
                .ToList();
        }
        else
        {
            salaryLines = result.Lines.ToList();
        }

        response.SalaryLineCount = salaryLines.Count;
        response.ProvisionLineCount = provisionLines?.Count ?? 0;

        // Legacy lines 831-844.
        if (salaryLines.Any())
        {
            var header = $"Employee Payroll/Salary For {input.PayInDate:MMM-yyyy}";
            var batchNumber = await _financePostingClient.PostJournalAsync(payrollArea, salaryLines, header);
            response.JournalBatchNumbers.Add(batchNumber);
            messages.Add(L("PayrollSalaryJvPostedSuccessfully"));
        }
        else
        {
            messages.Add(L("NoDataFoundToPostPayrollSalaryJv"));
        }

        // Legacy lines 846-858.
        if (provisionLines != null)
        {
            if (provisionLines.Any())
            {
                var header = $"Employee Provision For {input.PayInDate:MMM-yyyy}";
                var batchNumber = await _financePostingClient.PostJournalAsync(payrollArea, provisionLines, header);
                response.JournalBatchNumbers.Add(batchNumber);
                messages.Add(L("ProvisionJvPostedSuccessfully"));
            }
            else
            {
                messages.Add(L("NoDataFoundToPostProvisionJv"));
            }
        }

        response.Success = response.JournalBatchNumbers.Any();
        response.Message = string.Join(Environment.NewLine, messages);

        // Legacy lines 860-869: the post-JV outcome is persisted as an EMPLOYEE-LESS ProjectLog row
        // (`pl1.Employee` is never assigned, so it stays null; Status is 0 even when nothing was posted,
        // because legacy only reaches this point without an exception). Wave P8 made
        // ProjectLog.EmployeeId nullable, so this row is now written instead of only being returned in
        // PostJvResultDto.Message.
        await _projectLogRepository.InsertAsync(new ProjectLog
        {
            TenantId = AbpSession.GetTenantId(),
            EmployeeId = null,
            InPeriod = input.PayInDate,
            Message = response.Message,
            Status = 0,
            RunTimeStamp = Clock.Now,
            NoOfEmployee = 0
        });

        return response;
    }

    // [HttpPost] required: ABP maps Get* to HTTP GET, so NSwag would flatten CostingRunInputDto into
    // query-string params and never generate the DTO class, breaking the Angular client.
    [HttpPost]
    public Task<JournalVoucherPreviewDto> GetJournalVoucherLines(CostingRunInputDto input)
    {
        return _costingFileAppService.GetJournalVoucherLines(input);
    }

    public Task<JournalVoucherValidationResultDto> ValidateJournalVoucher(CostingRunInputDto input)
    {
        return _costingFileAppService.ValidateJournalVoucher(input);
    }

    public Task<CostingProcessStatusDto> GetProcessStatus()
    {
        return Task.FromResult(CostingFileAppService.BuildProcessStatus(_projectLogRepository));
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PostJv_Post)]
    public Task ClearProcessStatus()
    {
        return CostingFileAppService.ClearProjectLogAsync(_projectLogRepository);
    }
}
