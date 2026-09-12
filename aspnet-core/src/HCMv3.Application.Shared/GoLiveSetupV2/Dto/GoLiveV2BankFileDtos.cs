using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GoLiveSetupV2.Dto;

/// <summary>The month and payroll area whose pay the bank file transfers -- the ones chosen under Run payroll.</summary>
public class GoLiveV2BankFileSetupInput
{
    /// <summary>Any day of the month; the month end is used.</summary>
    [Required]
    public DateTime? PayDate { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string PayrollAreaName { get; set; }
}

/// <summary>What Step 4 offers: the banks a salary file can be made for, and who it would leave out.</summary>
public class GoLiveV2BankFileSetupOutput
{
    /// <summary>The tenant's banks that have at least one file layout, by name.</summary>
    public List<GoLiveV2PayingBankDto> PayingBanks { get; set; } = new();

    /// <summary>Employees in the payroll area with no bank or no IBAN on record: a bank file cannot pay them.</summary>
    public List<GoLiveV2BankFileEmployeeDto> EmployeesWithoutBank { get; set; } = new();

    /// <summary>Employees in the payroll area with a bank and an IBAN.</summary>
    public int EmployeesWithBank { get; set; }

    /// <summary>The company MOSAL ID on record, from its sponsor -- what several layouts write. Null until given.</summary>
    public long? MosalId { get; set; }

    /// <summary>
    /// Of those, how many also have an account number -- all a layout that pays into account numbers rather
    /// than IBANs can reach.
    /// </summary>
    public int EmployeesWithAccountNumber { get; set; }
}

/// <summary>A bank the company can pay salaries from, with the layouts its salary file comes in.</summary>
public class GoLiveV2PayingBankDto
{
    public int BankId { get; set; }

    public string Name { get; set; }

    /// <summary>The bank's code in the bank-file layouts, e.g. "NBK" -- whatever the bank record's own code is.</summary>
    public string Code { get; set; }

    /// <summary>The layout the bank record is set to, or the bank's usual one when none is set.</summary>
    public string SelectedLayout { get; set; }

    /// <summary>The company's account number at this bank, if it has been given before.</summary>
    public string CompanyAccountNo { get; set; }

    /// <summary>The bank's own code as its files write it (the bank record's external code).</summary>
    public string BankCode { get; set; }

    /// <summary>The company's code at the bank.</summary>
    public string CompanyCode { get; set; }

    /// <summary>The bank's usual layout first.</summary>
    public List<GoLiveV2BankFileLayoutDto> Layouts { get; set; } = new();

    /// <summary>
    /// The banks the payroll area's employees are paid into, with the code this paying bank writes for each in
    /// its file. Empty codes are what a blank "bank code" column in the file comes from.
    /// </summary>
    public List<GoLiveV2PayeeBankDto> PayeeBanks { get; set; } = new();
}

/// <summary>One bank employees are paid into, and the code the paying bank knows it by in its salary file.</summary>
public class GoLiveV2PayeeBankDto
{
    public int BankId { get; set; }

    public string BankName { get; set; }

    /// <summary>Given by the paying bank, not by this one. Null until somebody fills it in.</summary>
    public string Code { get; set; }

    /// <summary>How many of the area's employees are paid into it.</summary>
    public int Employees { get; set; }
}

/// <summary>One file layout, and what the company must give for it beyond each employee's bank and IBAN.</summary>
public class GoLiveV2BankFileLayoutDto
{
    /// <summary>E.g. "NBK-G1": G for an Excel sheet, T for a fixed-width text file.</summary>
    public string FormatCode { get; set; }

    public bool IsText { get; set; }

    public bool NeedsCompanyAccount { get; set; }

    public bool NeedsBankCode { get; set; }

    public bool NeedsCompanyCode { get; set; }

    /// <summary>
    /// The layout pays into each employee's account number, not their IBAN: anyone without one is left out.
    /// </summary>
    public bool NeedsEmployeeAccountNumber { get; set; }

    /// <summary>The layout writes a code for each employee's own bank, which the paying bank gives you.</summary>
    public bool NeedsPayeeBankCodes { get; set; }

    /// <summary>The layout writes the company's MOSAL ID beside each employee.</summary>
    public bool NeedsMosalId { get; set; }
}

/// <summary>Make the month's salary file: the paying bank, its layout, and whatever that layout needs.</summary>
public class GoLiveV2GenerateBankFileInput : GoLiveV2BankFileSetupInput
{
    public int PaymentBankId { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string FormatCode { get; set; }

    /// <summary>Saved as the company's account at the paying bank. Left as it was when blank.</summary>
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string CompanyAccountNo { get; set; }

    /// <summary>Saved as the paying bank's external code. Left as it was when blank.</summary>
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string BankCode { get; set; }

    /// <summary>Saved as the company's code at the paying bank. Left as it was when blank.</summary>
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string CompanyCode { get; set; }

    /// <summary>
    /// The company's MOSAL ID. Saved as the company's sponsor record, with every employee in the payroll area
    /// sponsored by it, which is where the file's MOSAL ID column comes from.
    /// </summary>
    public long? MosalId { get; set; }

    /// <summary>The code the paying bank writes for each bank employees are paid into. Blank ones are left as they were.</summary>
    public List<GoLiveV2PayeeBankDto> PayeeBanks { get; set; } = new();
}

/// <summary>The file, inline, and who is not in it.</summary>
public class GoLiveV2GenerateBankFileOutput
{
    public string FileName { get; set; }

    public string Base64Content { get; set; }

    public string ContentType { get; set; }

    public string FormatCode { get; set; }

    public int RecordCount { get; set; }

    public double TotalAmount { get; set; }

    /// <summary>
    /// The paying bank's code before it was corrected to the one the layouts know it by -- e.g. "1" for the
    /// National Bank of Kuwait seeded before the Kuwait pack used bank codes. Null when it was already right.
    /// </summary>
    public string BankCodeCorrectedFrom { get; set; }

    /// <summary>Employees in the payroll area the file does not pay, and why.</summary>
    public List<GoLiveV2BankFileEmployeeDto> LeftOut { get; set; } = new();
}

public class GoLiveV2BankFileEmployeeDto
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public GoLiveV2BankFileLeftOutReason Reason { get; set; }
}

public enum GoLiveV2BankFileLeftOutReason
{
    /// <summary>No bank, or no IBAN, on record for the month.</summary>
    NoBankDetails = 0,

    /// <summary>The month's payroll paid them nothing: no salary register, or a net pay of zero.</summary>
    NotPaid = 1,

    /// <summary>This layout pays into account numbers, and this employee has no account number on record.</summary>
    NoAccountNumber = 2,
}
