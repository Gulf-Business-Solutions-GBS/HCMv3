using System.Linq;
using HCMv3.PayrollManagement.Costing;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Hand-written mapping between the costing DOMAIN models (<c>HCMv3.PayrollManagement.Costing</c>) and the
/// costing DTOs.
///
/// <para>Mapperly is not used here on purpose: the sources are plain in-memory calculation results, not
/// entities, and <see cref="JournalVoucherPreviewDto.TotalDebit"/> / <see cref="JournalVoucherPreviewDto.IsBalanced"/>
/// are COMPUTED properties on the domain side (<see cref="JournalVoucherResult"/>) with no settable
/// counterpart -- exactly the "calculated property" case that the Mapperly guidance says to map by hand.</para>
/// </summary>
internal static class CostingMapper
{
    public static CostingRunInput ToDomain(CostingRunInputDto dto)
    {
        return new CostingRunInput
        {
            PayrollAreaName = dto.PayrollAreaName,
            EmployeeCode = dto.EmployeeCode,

            // The period is normalised to end-of-month exactly as legacy did before it is used as a
            // register match key (legacy uscCostingFile.ascx.cs line 442 / 803).
            PayInDate = CostEngineService.NormalizeToEndOfMonth(dto.PayInDate),
            PostingDate = dto.PostingDate,
            PayrollRunType = dto.PayrollRunType
        };
    }

    public static JournalVoucherLineDto ToDto(JournalVoucherLine line)
    {
        return new JournalVoucherLineDto
        {
            CompanyAccounts = line.CompanyAccounts,
            JournalBatchNumber = line.JournalBatchNumber,
            RecId = line.RecId,
            LineNumber = line.LineNumber,
            LineNumberDate = line.LineNumberDate,
            LineNumberAccountType = line.LineNumberAccountType,
            LedgerDimensionMainAccount = line.LedgerDimensionMainAccount,
            LedgerDimensionMainAccountName = line.LedgerDimensionMainAccountName,
            DefaultDimensionLocation = line.DefaultDimensionLocation,
            DefaultDimensionEmployees = line.DefaultDimensionEmployees,
            LedgerDimensionLocation = line.LedgerDimensionLocation,
            LedgerDimensionBrand = line.LedgerDimensionBrand,
            LedgerDimensionDepartment = line.LedgerDimensionDepartment,
            LedgerDimensionEmployees = line.LedgerDimensionEmployees,
            Currency = line.Currency,
            Debit = line.Debit,
            Credit = line.Credit,
            Description = line.Description,
            OffsetLedgerDimensionMainAccount = line.OffsetLedgerDimensionMainAccount,
            BillingType = line.BillingType,
            IsMerged = line.IsMerged,
            TransDate = line.TransDate,
            EmpCode = line.EmpCode
        };
    }

    public static JournalVoucherPreviewDto ToDto(JournalVoucherResult result)
    {
        return new JournalVoucherPreviewDto
        {
            Lines = result.Lines.Select(ToDto).ToList(),
            TotalDebit = result.TotalDebit,
            TotalCredit = result.TotalCredit,
            IsBalanced = result.IsBalanced
        };
    }

    public static CostingRunResultDto ToDto(CostingRunResult result)
    {
        return new CostingRunResultDto
        {
            TotalEmployees = result.TotalEmployees,
            SuccessCount = result.SuccessCount,
            FailedCount = result.FailedCount,
            Aborted = result.Aborted,
            Entries = result.Entries.Select(e => new CostingRunLogEntryDto
            {
                EmployeeId = e.EmployeeId,
                EmployeeCode = e.EmployeeCode,
                Status = e.Status,
                Message = e.Message
            }).ToList()
        };
    }
}
