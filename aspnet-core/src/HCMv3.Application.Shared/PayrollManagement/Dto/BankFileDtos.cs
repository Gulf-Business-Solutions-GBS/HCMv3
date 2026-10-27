using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Input for the bank salary-transfer file generator. Mirrors the legacy screen's filter panel
/// (the live <c>HCM10.Web/UserControls/PayrollManagement/uscBankFile.ascx</c>) one-for-one.
/// </summary>
public class GenerateBankFileInput
{
    /// <summary>Optional Payroll Area filter (legacy <c>cmbPayrollArea</c>).</summary>
    public int? PayrollAreaId { get; set; }

    /// <summary>Optional Sponsor filter (legacy <c>cmbSponsor</c>).</summary>
    public int? SponsorId { get; set; }

    /// <summary>
    /// Optional Sponsor Bank whose account number seeds the CBK-T7 header
    /// (legacy <c>cmbSponsorBank</c>, parsed there from a "CODE (ACCOUNT)" display string -- here it is a
    /// proper id, which removes the legacy string-parsing fragility).
    /// </summary>
    public int? SponsorBankId { get; set; }

    /// <summary>Optional MOSAL ID filter (legacy <c>cmbMosalId</c>).</summary>
    public long? MosalId { get; set; }

    /// <summary>Optional comma-separated employee codes (legacy <c>txtEmpCode</c>).</summary>
    public string EmployeeCodes { get; set; }

    /// <summary>Optional Organization Unit filter (legacy <c>cmbOrganizationUnit</c>).</summary>
    public long? OrganizationUnitId { get; set; }

    /// <summary>
    /// Payment Type radio (legacy <c>rdPaymentAll</c>/<c>rdPaymentBank</c>/<c>rdPaymentCash</c>): null (or NA) = ALL,
    /// which still needs an employee-bank record in force on the end date; Bank / Cash also need that payment type.
    /// </summary>
    public PersonnelAdministration.PaymentType? PaymentType { get; set; }

    /// <summary>Optional Employee Bank filter (legacy <c>cmbEmpBank</c>).</summary>
    public int? EmployeeBankId { get; set; }

    /// <summary>Payment (payer) bank -- REQUIRED; selects the file layout (legacy <c>ctlPaymentBank</c>).</summary>
    [Required]
    public int PaymentBankId { get; set; }

    /// <summary>
    /// "R" = Regular only, "O" = OffCycle only, anything else = Regular + OffCycle on/after the cutoff day
    /// (legacy <c>cmbPayrollRunType</c> and <c>GetCutoffDate()</c> at <c>uscBankFile2.ascx.cs:2787</c>).
    /// </summary>
    [Required]
    [StringLength(1)]
    public string PayrollRunType { get; set; }

    [Range(1900, 2999)]
    public int Year { get; set; }

    [Range(1, 12)]
    public int Month { get; set; }

    /// <summary>
    /// Current / Other (legacy <c>rdCurrent</c>/<c>rdOtherPeriod</c>). When set -- the Bank File screen always sets
    /// it -- the server works the dates out itself from the payroll area's control record (Current) or from
    /// <see cref="Month"/>/<see cref="Year"/> through the payroll period (Other), and <see cref="StartDate"/>/
    /// <see cref="EndDate"/> are IGNORED: a date sent from a browser can arrive a day early on a server running on
    /// UTC. When null (server-side callers such as Go-Live Setup), the dates below are used as given.
    /// </summary>
    public BankFilePeriodType? PeriodType { get; set; }

    /// <summary>Period start (legacy <c>txtStartDate</c>). Retained for parity; the layouts key off EndDate.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Period end (legacy <c>txtEndDate</c>) -- every header/footer date token derives from this.</summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Produce one file per payee bank and return them zipped (legacy <c>chkGenerateAllBankFiles</c>).
    /// Legacy collapses back to a single file when only one payee bank is involved.
    /// </summary>
    public bool SplitBankFiles { get; set; }
}

/// <summary>Generated file, returned inline base64 (same pattern as <c>ReportPdfOutput</c>).</summary>
public class BankFileOutput
{
    public string FileName { get; set; }

    public string Base64Content { get; set; }

    /// <summary>text/plain, application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, or application/zip.</summary>
    public string ContentType { get; set; }

    /// <summary>Resolved format code(s), e.g. "CBK-T7". Comma-separated when split across payee banks.</summary>
    public string FormatCode { get; set; }

    /// <summary>Number of payee detail rows across all produced files.</summary>
    public int RecordCount { get; set; }

    /// <summary>Sum of the transferred net amounts across all produced files.</summary>
    public double TotalAmount { get; set; }

    /// <summary>The run's row in the bank-file log (View Log), which also holds a stored copy of the file.</summary>
    public int? ExecutionLogId { get; set; }

    /// <summary>
    /// Salary registers locked by this run because "Lock salary register on payroll exit" is on (legacy
    /// <c>LockSalaryRegisters</c>). Registers that were already locked are not counted.
    /// </summary>
    public int RegistersLocked { get; set; }

    /// <summary>The period the file was made for, dd/MM/yyyy.</summary>
    public string StartDateText { get; set; }

    /// <summary>The period the file was made for, dd/MM/yyyy.</summary>
    public string EndDateText { get; set; }
}

/// <summary>Simple id/text pair for the screen's filter combos.</summary>
public class BankFileLookupItemDto
{
    public int Id { get; set; }

    public string DisplayText { get; set; }

    /// <summary>Entity code, where the entity has one (used to show "NBK" alongside the name).</summary>
    public string Code { get; set; }
}

/// <summary>Sponsor bank combo item -- legacy renders these as "BANKCODE (ACCOUNTNUMBER)".</summary>
public class BankFileSponsorBankDto : BankFileLookupItemDto
{
    public string AccountNumber { get; set; }

    /// <summary>The sponsor the account belongs to; the screen lists only the chosen sponsor's accounts.</summary>
    public int SponsorId { get; set; }
}

/// <summary>A row of the <c>BankFile</c> lookup table (the seeded format catalogue).</summary>
public class BankFileFormatDto
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    /// <summary>Payer bank code the format belongs to, e.g. "CBK" for "CBK-T7".</summary>
    public string PayerBankCode { get; set; }

    /// <summary>True when the format emits fixed-width text rather than a spreadsheet grid.</summary>
    public bool IsTextFormat { get; set; }

    /// <summary>False when the <c>BankFile</c> lookup row has not been seeded in this tenant yet.</summary>
    public bool IsSeeded { get; set; }
}

/// <summary>Everything the screen needs to populate its filter panel.</summary>
public class BankFileLookupsOutput
{
    public List<BankFileLookupItemDto> PayrollAreas { get; set; } = new();

    public List<BankFileLookupItemDto> Sponsors { get; set; } = new();

    public List<BankFileSponsorBankDto> SponsorBanks { get; set; } = new();

    public List<BankFileLookupItemDto> PaymentBanks { get; set; } = new();

    public List<BankFileLookupItemDto> EmployeeBanks { get; set; } = new();

    /// <summary>Distinct sponsor MOSAL IDs (legacy <c>BindMosalId</c>).</summary>
    public List<long> MosalIds { get; set; } = new();

    public List<BankFileFormatDto> Formats { get; set; } = new();
}
