using System.Collections.Generic;

namespace HCMv3.PayrollManagement.BankFiles;

/// <summary>
/// Bank-transfer file format codes, ported verbatim from the legacy XAF web user control
/// <c>HCM10.Web/UserControls/PayrollManagement/uscBankFile2.ascx.cs</c> lines 37-73.
///
/// SUPERSESSION FINDING (see the wave report): <c>uscBankFile.ascx.cs</c> (5,856 lines) and
/// <c>uscBankFile2.ascx.cs</c> (3,357 lines) are byte-identical after CR normalisation EXCEPT for a single
/// inserted hunk at <c>uscBankFile.ascx.cs:2512-5010</c> which is a 2,499-line, 100%-commented-out duplicate
/// of the generator method (verified: zero non-blank, non-comment lines in that range). The two files declare
/// the SAME 28 format codes and contain the SAME 28 live <c>formatList.Contains(...)</c> branches. Therefore
/// <c>uscBankFile2</c> is simply <c>uscBankFile</c> with dead code stripped; neither carries a format the
/// other lacks, and the union of both == this list. Ported from <c>uscBankFile2</c> (the clean copy).
///
/// WHERE FORMAT DEFINITIONS LIVE: the format LAYOUTS are hardcoded C# in the legacy code-behind's
/// <c>switch (payerBankcode)</c> (uscBankFile2.ascx.cs:433-2496) -- they are NOT data-driven. The
/// <c>BankFile</c> entity is only a Code/NameEn/NameAr LOOKUP row; legacy's <c>SaveBankFileFormats()</c>
/// (uscBankFile2.ascx.cs:2670) merely seeds one lookup row per code below so an administrator can attach a
/// format to a bank via <c>Bank.BankFile</c> or <c>PayeeBankDetail.BankFile</c>. <c>BankFile</c>,
/// <c>Bank</c>, <c>PayeeBankDetail</c> and <c>EmployeeBank</c> are ALL already ported to HCMv3 --
/// NO NEW ENTITY IS REQUIRED for this feature.
///
/// Note legacy resolves the active format by <c>BankFile.NameEn</c>, not <c>BankFile.Code</c>
/// (uscBankFile2.ascx.cs:424/428) -- and its seeder sets NameEn == Code, so they coincide. Ported faithfully.
/// </summary>
public static class BankFileFormatCodes
{
    // ---- NBK (National Bank of Kuwait) ----
    /// <summary>Grid, ALL clients.</summary>
    public const string NbkG1 = "NBK-G1";
    /// <summary>Grid, LINADINA / HAMRA / KSEC.</summary>
    public const string NbkG2 = "NBK-G2";
    /// <summary>Grid, AMK. IBAN instead of AccountNo (Zainab 2023.12.11).</summary>
    public const string NbkG3 = "NBK-G3";

    // ---- CBK (Commercial Bank of Kuwait) ----
    /// <summary>Text, IMILL. HDRINT/DTLINT/TRLINT.</summary>
    public const string CbkT1 = "CBK-T1";
    /// <summary>Text, 039-NAH. HDRABG/DTLABG/TRLABG.</summary>
    public const string CbkT2 = "CBK-T2";
    /// <summary>Text, IMILL. HDRSMS/DTLSMS/TRLSMS.</summary>
    public const string CbkT3 = "CBK-T3";
    /// <summary>Text, IMILL. HDRABC/DTLSIG/TRLABC.</summary>
    public const string CbkT4 = "CBK-T4";
    /// <summary>Text, RHH. HDRSMS/DTLSMS/TRLSMS with bank code.</summary>
    public const string CbkT5 = "CBK-T5";
    /// <summary>Text, RHH. CR #4488(RHH) - CBK Portal, Other Bank Beneficiaries Requirements.</summary>
    public const string CbkT6 = "CBK-T6";
    /// <summary>Text, NEC. CR #4847(NEC) - CBK Bank File Format For Salary Transfer.</summary>
    public const string CbkT7 = "CBK-T7";
    /// <summary>Grid, ALL clients.</summary>
    public const string CbkG1 = "CBK-G1";
    /// <summary>Grid, KHAT. Appends a Net Amount grand-total row.</summary>
    public const string CbkG2 = "CBK-G2";

    // ---- GBK / GB (Gulf Bank) ----
    /// <summary>Text, KPHC. HDR/DTL/TRL.</summary>
    public const string GbkT1 = "GBK-T1";
    /// <summary>Grid, ALL clients.</summary>
    public const string GbkG1 = "GBK-G1";
    /// <summary>Grid, HAMRA. Adds MOSAL ID + Employer English Name.</summary>
    public const string GbkG2 = "GBK-G2";

    // ---- ABK (Al Ahli Bank of Kuwait) ----
    /// <summary>Grid, ALL clients. On-hold filtered, 0-amount filtered, sorted.</summary>
    public const string AbkG1 = "ABK-G1";
    /// <summary>Grid, RCF. Same as G1 but retains the EmployeeCode column.</summary>
    public const string AbkG2 = "ABK-G2";
    /// <summary>Grid, HAMRA. No on-hold filter, no 0-amount filter.</summary>
    public const string AbkG3 = "ABK-G3";

    // ---- KIB (Kuwait International Bank) ----
    /// <summary>Grid, ALL clients.</summary>
    public const string KibG1 = "KIB-G1";

    // ---- WRB (Warba Bank) ----
    /// <summary>Grid, ALL clients.</summary>
    public const string WrbG1 = "WRB-G1";

    // ---- BUR (Burgan Bank) ----
    /// <summary>Grid, ALL clients.</summary>
    public const string BurG1 = "BUR-G1";
    /// <summary>Text, KHUFF / RCF. HDR/DTL/TRL fixed width.</summary>
    public const string BurT1 = "BUR-T1";
    /// <summary>Grid, NEC. CR #4546(NEC) - Burgan Bank Salary Sheet Format update.</summary>
    public const string BurG2 = "BUR-G2";

    // ---- BURBD (Burgan B-Dinar) ----
    /// <summary>Text, ALL clients. 0/1/2 record-type fixed width.</summary>
    public const string BurBdT1 = "BURBD-T1";
    /// <summary>Grid, ALL clients. CR #4546(NEC) - Burgan Bank Salary Sheet Format update.</summary>
    public const string BurBdG1 = "BURBD-G1";

    // ---- NBR (National Bank of Ras Al Khaimah / legacy "NBR") ----
    /// <summary>Grid, ALL clients.</summary>
    public const string NbrG1 = "NBR-G1";

    // ---- KFH (Kuwait Finance House) ----
    /// <summary>Grid, ALL clients.</summary>
    public const string KfhG1 = "KFH-G1";

    // ---- BB (Boubyan Bank) ----
    /// <summary>Grid, ALL clients.</summary>
    public const string BbG1 = "BB-G1";

    /// <summary>
    /// All 28 codes in legacy declaration order (uscBankFile2.ascx.cs:2675-2715). Used to seed/verify the
    /// <c>BankFile</c> lookup table -- the HCMv3 equivalent of legacy <c>SaveBankFileFormats()</c>.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = new[]
    {
        NbkG1, NbkG2, NbkG3,
        CbkT1, CbkT3, CbkG1, CbkG2, CbkT2, CbkT4, CbkT5, CbkT6, CbkT7,
        GbkT1, GbkG1, GbkG2,
        AbkG1, AbkG2, AbkG3,
        KibG1,
        WrbG1,
        BurG1, BurT1, BurG2,
        BurBdT1, BurBdG1,
        NbrG1,
        KfhG1,
        BbG1
    };
}
