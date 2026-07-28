using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Bank salary-transfer file generation, ported from the legacy XAF web user control
/// <c>HCM10.Web/UserControls/PayrollManagement/uscBankFile2.ascx.cs</c>.
///
/// SUPERSESSION: <c>uscBankFile.ascx.cs</c> and <c>uscBankFile2.ascx.cs</c> are the same program --
/// the former merely retains a 2,499-line, fully commented-out duplicate of the generator. Both declare and
/// dispatch the SAME 28 formats, so ONE service covers both. See <c>BankFileFormatCodes</c> for the proof.
/// </summary>
public interface IBankFilesAppService : IApplicationService
{
    /// <summary>Filter-panel lookups plus the format catalogue.</summary>
    Task<BankFileLookupsOutput> GetBankFileLookups();

    /// <summary>
    /// Generates the file(s) and returns them base64-encoded. Nothing is written to server disk --
    /// legacy wrote to <c>~/Contents/BankFile</c> and streamed via <c>Response.WriteFile</c>.
    /// </summary>
    Task<BankFileOutput> GenerateBankFile(GenerateBankFileInput input);

    /// <summary>
    /// Ensures every one of the 28 legacy format codes exists as a <c>BankFile</c> lookup row for the
    /// current tenant. HCMv3 equivalent of legacy <c>SaveBankFileFormats()</c>
    /// (<c>uscBankFile2.ascx.cs:2670</c>), which ran implicitly on every page load; here it is explicit.
    /// </summary>
    Task SeedBankFileFormats();
}
