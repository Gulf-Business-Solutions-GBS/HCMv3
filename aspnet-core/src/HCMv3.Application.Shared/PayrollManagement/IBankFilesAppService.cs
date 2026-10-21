using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Bank salary-transfer file generation, ported from the LIVE legacy screen
/// <c>HCM10.Web/UserControls/PayrollManagement/uscBankFile.ascx(.cs)</c>.
///
/// <para><c>uscBankFile2.ascx.cs</c>, which the formats were first ported from, carries the SAME 28 format
/// branches line for line; what only the live screen has is the Current/Other period choice, the ALL/BANK/CASH
/// payment-type radios and the "View Log" popup with its stored copy of every file. All three are here.</para>
/// </summary>
public interface IBankFilesAppService : IApplicationService
{
    /// <summary>Filter-panel lookups plus the format catalogue.</summary>
    Task<BankFileLookupsOutput> GetBankFileLookups();

    /// <summary>
    /// The sponsor's own bank accounts, shown as "CODE (ACCOUNT)" -- legacy <c>BindSponsorBank</c>, which
    /// re-filled the Sponsor (A/C) list whenever the sponsor changed.
    /// </summary>
    Task<List<BankFileSponsorBankDto>> GetBankFileSponsorBanks(EntityDto input);

    /// <summary>
    /// The start and end dates for the chosen payroll area and Current/Other period (legacy
    /// <c>ASPxCallbackPanelPeriod_Callback</c>). Never throws for a missing choice: it says why in
    /// <see cref="BankFilePeriodDto.Message"/>, as legacy simply left the boxes blank.
    /// </summary>
    Task<BankFilePeriodDto> GetBankFilePeriod(GetBankFilePeriodInput input);

    /// <summary>
    /// Generates the file(s) and returns them base64-encoded. A zipped copy is kept in the tenant's file store
    /// against the run's log row, and the run's salary registers are locked when the company locks on payroll
    /// exit -- both as legacy did.
    /// </summary>
    Task<BankFileOutput> GenerateBankFile(GenerateBankFileInput input);

    /// <summary>
    /// Ensures every one of the 28 legacy format codes exists as a <c>BankFile</c> lookup row for the
    /// current tenant. HCMv3 equivalent of legacy <c>SaveBankFileFormats()</c>
    /// (<c>uscBankFile.ascx.cs:2663</c>), which ran implicitly on every page load; here it is explicit.
    /// </summary>
    Task SeedBankFileFormats();

    /// <summary>Legacy "Bank File Generation Log" popup: newest first, with All / Successful / Failed counts.</summary>
    Task<BankFileLogsOutput> GetBankFileLogs(GetBankFileLogsInput input);

    /// <summary>Legacy "Export Log": the log rows the filter shows, as an Excel workbook.</summary>
    Task<FileDto> ExportBankFileLogs(GetBankFileLogsInput input);

    /// <summary>The stored copy of the file(s) one run produced (a .zip, as legacy stored it).</summary>
    Task<FileDto> GetBankFileLogDownload(EntityDto input);
}
