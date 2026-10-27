using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Authorization;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Runtime.Session;
using Abp.UI;
using ClosedXML.Excel;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSettings.Services;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Basic-pay (salary) Excel import -- the file side of legacy <c>ImportExcelController</c>'s
/// <c>EmployeeBasicPay</c> branch (lines 3978-4224). Reads the first worksheet, walks the rows, runs each row
/// through <see cref="IEmployeeBasicPayImportService"/> in its own unit of work and builds legacy's
/// <c>ImportStatus</c> result.
///
/// <para>The method's own unit of work is switched off: every row opens a <c>RequiresNew</c> one, so a row that
/// fails (a payroll lock, a retro rule, a zero amount) rolls back only itself, as legacy's per-row
/// <c>CommitChanges</c>/<c>Rollback</c> did. An outer transaction holding the settings read open would also
/// make the rows wait on it.</para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays_Create)]
public class EmployeeBasicPayImportAppService : HCMv3AppServiceBase, IEmployeeBasicPayImportAppService
{
    private readonly IEmployeeBasicPayImportService _importService;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public EmployeeBasicPayImportAppService(
        IEmployeeBasicPayImportService importService,
        ISysSettingsAccessor sysSettingsAccessor)
    {
        _importService = importService;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays_Create)]
    [UnitOfWork(IsDisabled = true)]
    public async Task<ImportEmployeeBasicPaysOutput> ImportEmployeeBasicPays(ImportEmployeeBasicPaysInput input)
    {
        var tenantId = AbpSession.GetTenantId();
        var content = DecodeBase64(input.FileBase64);
        var sheet = ReadSheet(content);

        // Legacy reads the setting once, before the first row.
        bool carryAllowanceOnSalaryImport;
        using (var uow = UnitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
        {
            carryAllowanceOnSalaryImport = (await _sysSettingsAccessor.GetPayrollSettingsAsync())?.CarryAllowanceOnSalaryImport ?? false;
            await uow.CompleteAsync();
        }

        var output = new ImportEmployeeBasicPaysOutput { TotalRecords = sheet.Rows.Count };
        var employeeCodeMissing = sheet.MissingColumns.Contains(EmployeeBasicPayImportRow.EmployeeCodeColumn);
        var firstMissingColumn = sheet.MissingColumns.FirstOrDefault();

        foreach (var (rowNumber, row) in sheet.Rows)
        {
            try
            {
                // Legacy read "Employee Code" first: without that column every row fails on it.
                if (employeeCodeMissing)
                {
                    throw new UserFriendlyException(L("BasicPayImportColumnMissing", EmployeeBasicPayImportRow.EmployeeCodeColumn));
                }

                // A blank code is skipped before any other column is touched -- counted in the total only.
                if (row.EmployeeCode.IsNullOrWhiteSpace())
                {
                    continue;
                }

                if (firstMissingColumn != null)
                {
                    throw new UserFriendlyException(L("BasicPayImportColumnMissing", firstMissingColumn));
                }

                using (var uow = UnitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
                {
                    var result = await _importService.ImportRowAsync(tenantId, row, carryAllowanceOnSalaryImport);
                    switch (result.Status)
                    {
                        case EmployeeBasicPayImportRowStatus.Invalid:
                            AddError(output, rowNumber, result.Message);
                            continue;
                        case EmployeeBasicPayImportRowStatus.Skipped:
                            continue;
                    }

                    await uow.CompleteAsync();
                }

                output.SuccessfulRecords++;
            }
            catch (Exception ex)
            {
                if (ex is not UserFriendlyException)
                {
                    Logger.Error($"Basic pay import, Excel row {rowNumber}", ex);
                }

                AddError(output, rowNumber, ex.Message);
            }
        }

        output.Status = output.TotalRecords == output.SuccessfulRecords
            ? L("BasicPayImportAllRowsImported")
            : output.Errors.Count > 0
                ? string.Join(Environment.NewLine, output.Errors)
                : L("BasicPayImportCompleted");

        return output;
    }

    private void AddError(ImportEmployeeBasicPaysOutput output, int rowNumber, string message)
    {
        output.ErrorRecords++;
        output.Errors.Add(L("BulkImportRowError", rowNumber, message));
    }

    #region << Workbook reading >>

    private sealed class ImportSheet
    {
        public List<(int RowNumber, EmployeeBasicPayImportRow Row)> Rows { get; } = new();

        /// <summary>Legacy columns absent from the header row, in the order legacy touched them.</summary>
        public List<string> MissingColumns { get; } = new();
    }

    /// <summary>
    /// First worksheet only; row 1 is the header. Every row from 2 to the last row holding a value is a data
    /// row, so its Excel row number is legacy's <c>DataTable index + 2</c>.
    /// </summary>
    private ImportSheet ReadSheet(byte[] content)
    {
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(new MemoryStream(content));
        }
        catch (Exception ex)
        {
            throw new UserFriendlyException(L("BulkImportWorkbookUnreadable", ex.Message));
        }

        using (workbook)
        {
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                throw new UserFriendlyException(L("BulkImportWorkbookHasNoSheet"));
            }

            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastColumn = worksheet.LastColumnUsed(XLCellsUsedOptions.Contents)?.ColumnNumber() ?? 0;
            for (var c = 1; c <= lastColumn; c++)
            {
                var header = ReadText(worksheet.Cell(1, c));
                if (header.Length > 0 && !columns.ContainsKey(header))
                {
                    columns[header] = c;
                }
            }

            var sheet = new ImportSheet();
            sheet.MissingColumns.AddRange(EmployeeBasicPayImportRow.Columns.Where(name => !columns.ContainsKey(name)));

            var lastRow = worksheet.LastRowUsed(XLCellsUsedOptions.Contents)?.RowNumber() ?? 1;
            for (var r = 2; r <= lastRow; r++)
            {
                IXLCell Cell(string name) => columns.TryGetValue(name, out var c) ? worksheet.Cell(r, c) : null;

                var effectiveDate = Cell(EmployeeBasicPayImportRow.EffectiveDateColumn);
                var amount = Cell(EmployeeBasicPayImportRow.AmountColumn);

                sheet.Rows.Add((r, new EmployeeBasicPayImportRow
                {
                    EmployeeCode = ReadText(Cell(EmployeeBasicPayImportRow.EmployeeCodeColumn)),
                    EffectiveDateValue = ReadDate(effectiveDate),
                    EffectiveDateText = ReadText(effectiveDate),
                    PayType = ReadText(Cell(EmployeeBasicPayImportRow.PayTypeColumn)),
                    PayInterval = ReadText(Cell(EmployeeBasicPayImportRow.PayIntervalColumn)),
                    WageType = ReadText(Cell(EmployeeBasicPayImportRow.WageTypeColumn)),
                    AmountValue = ReadNumber(amount),
                    AmountText = ReadText(amount),
                    Currency = ReadText(Cell(EmployeeBasicPayImportRow.CurrencyColumn))
                }));
            }

            return sheet;
        }
    }

    /// <summary>Legacy turned a cell it could not convert into an empty value; so does this.</summary>
    private static string ReadText(IXLCell cell)
    {
        if (cell == null)
        {
            return "";
        }

        try
        {
            return cell.GetString()?.Trim() ?? "";
        }
        catch (Exception)
        {
            return "";
        }
    }

    private static DateTime? ReadDate(IXLCell cell)
    {
        try
        {
            return cell != null && cell.DataType == XLDataType.DateTime ? cell.GetDateTime() : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static double? ReadNumber(IXLCell cell)
    {
        try
        {
            return cell != null && cell.DataType == XLDataType.Number ? cell.GetDouble() : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private byte[] DecodeBase64(string value)
    {
        if (value.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("BulkImportFileNotSelected"));
        }

        // Browsers commonly hand back a "data:...;base64," prefixed string from FileReader.readAsDataURL.
        var commaIndex = value.IndexOf(',');
        if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex > 0)
        {
            value = value.Substring(commaIndex + 1);
        }

        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            throw new UserFriendlyException(L("BulkImportFileNotSelected"));
        }
    }

    #endregion
}
