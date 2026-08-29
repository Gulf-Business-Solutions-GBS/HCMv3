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

namespace HCMv3.GoLiveSetup.Apply;

/// <summary>
/// Provisions where each salary is paid, from step S3.3.
///
/// <para>Runs after the employee stage: every row keys on an employee code that must already resolve.
/// A row for an unknown employee is reported rather than skipped, because silently dropping it is how
/// somebody ends up unpaid on the first run.</para>
/// </summary>
public class BankApplyStage : ApplyStageBase, ITransientDependency
{
    public const string StepId = "S3.3";

    /// <summary>The port's open-ended end date, matching every other dated record here.</summary>
    private static readonly DateTime OpenEnded = new(9999, 12, 31);

    private readonly GoLiveDraftReader _reader;
    private readonly IEmployeeBanksAppService _banks;
    private readonly IRepository<Employee> _employees;
    private readonly IRepository<EmployeeBank> _employeeBanks;
    private readonly IRepository<Bank> _bankLookup;

    public BankApplyStage(
        GoLiveDraftReader reader,
        IEmployeeBanksAppService banks,
        IRepository<Employee> employees,
        IRepository<EmployeeBank> employeeBanks,
        IRepository<Bank> bankLookup)
    {
        _reader = reader;
        _banks = banks;
        _employees = employees;
        _employeeBanks = employeeBanks;
        _bankLookup = bankLookup;
    }

    public async Task<GoLiveApplyStageDto> RunAsync(bool apply)
    {
        var stage = new GoLiveApplyStageDto { StepId = StepId, Target = nameof(EmployeeBank) };

        var rows = await _reader.ReadRowsAsync(StepId);
        if (rows.Count == 0)
        {
            return stage;
        }

        var employeeIds = await LoadEmployeeIdsAsync(_employees, _reader);

        var banks = (await _bankLookup.GetAllListAsync())
            .Where(b => !string.IsNullOrWhiteSpace(b.NameEn))
            .GroupBy(b => b.NameEn.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        // One bank record per employee is what a go-live import means; a second run updates it rather
        // than stacking a duplicate alongside.
        var existing = (await _employeeBanks.GetAllListAsync())
            .GroupBy(b => b.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First().Id);

        var joiningDates = (await _employees.GetAllListAsync())
            .ToDictionary(e => e.Id, e => e.JoiningDate);

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

            try
            {
                var paidBy = GoLiveDraftReader.Text(row, "paidBy");
                var isCash = string.Equals(paidBy, "Cash", StringComparison.OrdinalIgnoreCase);
                var iban = GoLiveDraftReader.Text(row, "iban");
                var bankName = GoLiveDraftReader.Text(row, "bank");

                int? bankId = null;
                if (!isCash)
                {
                    if (bankName == null)
                    {
                        throw new ApplyRowException("Bank name is required when the salary is paid by transfer.");
                    }

                    if (!banks.TryGetValue(bankName, out var resolved))
                    {
                        throw new ApplyRowException($"Bank '{bankName}' is not in the system.");
                    }

                    bankId = resolved;

                    if (iban == null)
                    {
                        throw new ApplyRowException("IBAN is required when the salary is paid by transfer.");
                    }
                }

                var dto = new CreateOrEditEmployeeBankDto
                {
                    EmployeeId = employeeId,
                    BankId = bankId,
                    PaymentType = isCash ? PaymentType.Cash : PaymentType.Bank,
                    IBAN = iban,
                    AccountNum = GoLiveDraftReader.Text(row, "accountNum"),
                    // Dated from the employee's joining date so the record covers their whole history,
                    // which is what the payroll engine's period lookups expect.
                    StartDate = joiningDates.TryGetValue(employeeId, out var joined) ? joined : DateTime.UtcNow.Date,
                    EndDate = OpenEnded,
                };

                var isNew = !existing.TryGetValue(employeeId, out var existingId);
                if (!isNew)
                {
                    dto.Id = existingId;
                }

                if (apply)
                {
                    await _banks.CreateOrEdit(dto);
                }

                if (isNew)
                {
                    stage.Created++;
                }
                else
                {
                    stage.Updated++;
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
