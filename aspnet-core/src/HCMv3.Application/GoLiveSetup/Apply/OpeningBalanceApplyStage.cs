using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.GoLiveSetup.Apply;

/// <summary>
/// Provisions where each employee stands on the day of go-live, from step S3.4.
///
/// <para>Two different targets from one sheet. The leave balance becomes a <c>TimeTransfer</c> of type
/// "Annual Leave Opening Balance" -- seeded with the universal reference pack, so it resolves on any
/// tenant. An outstanding loan becomes an <c>EmployeeLoan</c>, but only when the row actually carries a
/// balance: most tenants have none, and every row failing for a missing loan type would bury the
/// failures that matter.</para>
/// </summary>
public class OpeningBalanceApplyStage : ApplyStageBase, ITransientDependency
{
    public const string StepId = "S3.4";

    /// <summary>Seeded by the universal pack. Named rather than an enum because it is a tenant lookup.</summary>
    private const string AnnualLeaveOpeningBalance = "Annual Leave Opening Balance";

    private static readonly DateTime OpenEnded = new(9999, 12, 31);

    private readonly GoLiveDraftReader _reader;
    private readonly ITimeTransfersAppService _transfers;
    private readonly IEmployeeLoansAppService _loans;
    private readonly IRepository<Employee> _employees;
    private readonly IRepository<TimeTransfer> _transferRepository;
    private readonly IRepository<TimeTransferType> _transferTypes;
    private readonly IRepository<EmployeeLoan> _loanRepository;
    private readonly IRepository<LoanType> _loanTypes;

    public OpeningBalanceApplyStage(
        GoLiveDraftReader reader,
        ITimeTransfersAppService transfers,
        IEmployeeLoansAppService loans,
        IRepository<Employee> employees,
        IRepository<TimeTransfer> transferRepository,
        IRepository<TimeTransferType> transferTypes,
        IRepository<EmployeeLoan> loanRepository,
        IRepository<LoanType> loanTypes)
    {
        _reader = reader;
        _transfers = transfers;
        _loans = loans;
        _employees = employees;
        _transferRepository = transferRepository;
        _transferTypes = transferTypes;
        _loanRepository = loanRepository;
        _loanTypes = loanTypes;
    }

    public async Task<GoLiveApplyStageDto> RunAsync(bool apply)
    {
        var stage = new GoLiveApplyStageDto { StepId = StepId, Target = "OpeningBalance" };

        var rows = await _reader.ReadRowsAsync(StepId);
        if (rows.Count == 0)
        {
            return stage;
        }

        var employeeIds = await LoadEmployeeIdsAsync(_employees, _reader);

        var leaveTypeId = (await _transferTypes.GetAllListAsync())
            .FirstOrDefault(t => string.Equals(t.NameEn?.Trim(), AnnualLeaveOpeningBalance, StringComparison.OrdinalIgnoreCase))
            ?.Id;

        // Loan types are client policy, not shipped reference data. One is unambiguous; several mean the
        // sheet cannot say which, and none means there is nothing to attach a balance to.
        var loanTypes = await _loanTypes.GetAllListAsync();

        var existingTransfers = (await _transferRepository.GetAllListAsync())
            .Where(t => leaveTypeId.HasValue && t.TransferTypeId == leaveTypeId.Value)
            .GroupBy(t => t.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First().Id);

        var existingLoans = (await _loanRepository.GetAllListAsync())
            .GroupBy(l => l.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First().Id);

        var goLiveDate = DateTime.UtcNow.Date;

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (GoLiveDraftReader.IsEmpty(row))
            {
                continue;
            }

            var rowNumber = i + 1;
            if (!TryResolveEmployee(stage, employeeIds, row, rowNumber, apply, out var code, out var employeeId))
            {
                continue;
            }

            var leaveDays = GoLiveDraftReader.Number(row, "leaveDays");
            var loanBalance = GoLiveDraftReader.Number(row, "loanBalance");
            var instalment = GoLiveDraftReader.Number(row, "loanInstalment");

            if (leaveDays == null && loanBalance == null)
            {
                // The row exists only to name an employee. Nothing to provision, and nothing wrong.
                stage.Skipped++;
                continue;
            }

            try
            {
                var touched = false;

                if (leaveDays.HasValue)
                {
                    if (!leaveTypeId.HasValue)
                    {
                        throw new ApplyRowException(
                            $"Transfer type '{AnnualLeaveOpeningBalance}' is missing. Seed the universal reference pack first.");
                    }

                    var dto = new CreateOrEditTimeTransferDto
                    {
                        EmployeeId = employeeId,
                        TransferDate = goLiveDate,
                        TransferTypeId = leaveTypeId.Value,
                        TransferCount = leaveDays.Value,
                        Comments = "Opening balance from Go-Live Setup.",
                    };

                    if (existingTransfers.TryGetValue(employeeId, out var transferId))
                    {
                        dto.Id = transferId;
                    }

                    if (apply)
                    {
                        await _transfers.CreateOrEdit(dto);
                    }

                    touched = true;
                }

                if (loanBalance.HasValue && loanBalance.Value > 0)
                {
                    if (loanTypes.Count == 0)
                    {
                        throw new ApplyRowException(
                            "An outstanding loan balance was given but the tenant has no loan type configured.");
                    }

                    if (loanTypes.Count > 1)
                    {
                        throw new ApplyRowException(
                            "The tenant has several loan types, so this sheet cannot say which the balance belongs to.");
                    }

                    var dto = new CreateOrEditEmployeeLoanDto
                    {
                        EmployeeId = employeeId,
                        LoanTypeId = loanTypes[0].Id,
                        StartDate = goLiveDate,
                        RecoveryStartDate = goLiveDate,
                        EndDate = OpenEnded,
                        // The customer gives what is still owed, not the original advance, so the actual
                        // and the outstanding amount are the same figure at go-live.
                        ActualAmount = loanBalance.Value,
                        DueAmount = loanBalance.Value,
                        FixedAmount = instalment ?? 0,
                        IncludeInLoanPay = true,
                        Comments = "Opening balance from Go-Live Setup.",
                    };

                    if (existingLoans.TryGetValue(employeeId, out var loanId))
                    {
                        dto.Id = loanId;
                    }

                    if (apply)
                    {
                        await _loans.CreateOrEdit(dto);
                    }

                    touched = true;
                }

                if (touched)
                {
                    var isNew = !existingTransfers.ContainsKey(employeeId) && !existingLoans.ContainsKey(employeeId);
                    if (isNew)
                    {
                        stage.Created++;
                    }
                    else
                    {
                        stage.Updated++;
                    }
                }
            }
            catch (UserFriendlyException ex)
            {
                Fail(stage, rowNumber, code, ex.Message);
            }
            catch (ApplyRowException ex)
            {
                Fail(stage, rowNumber, code, ex.Message);
            }
        }

        return stage;
    }
}
