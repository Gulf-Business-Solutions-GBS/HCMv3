using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

/// <summary>
/// Input of the basic-pay (salary) Excel import. The first worksheet is read; its header row must hold the
/// legacy columns "Employee Code", "EffectiveDate", "PayType", "PayInterval", "WageType", "Amount" and
/// "Currency" (matched ignoring case and surrounding spaces).
/// </summary>
public class ImportEmployeeBasicPaysInput
{
    [StringLength(260)]
    public string FileName { get; set; }

    /// <summary>Base64-encoded <c>.xlsx</c>; a <c>data:...;base64,</c> prefix is accepted.</summary>
    [Required]
    [DisableValidation]
    public string FileBase64 { get; set; }
}

/// <summary>Legacy <c>ImportStatus</c> popup: the counts and the status text.</summary>
public class ImportEmployeeBasicPaysOutput
{
    /// <summary>Every data row, including rows skipped for a blank Employee Code.</summary>
    public int TotalRecords { get; set; }

    public int SuccessfulRecords { get; set; }

    public int ErrorRecords { get; set; }

    /// <summary>
    /// "Data has been imported successfully." when every row succeeded; otherwise the error lines joined by new
    /// lines; or "Employee Basic Pay has been imported successfully." when nothing failed but rows were skipped.
    /// </summary>
    public string Status { get; set; }

    /// <summary>One line per failed row: "Excel RowNo : {n} - {message}".</summary>
    public List<string> Errors { get; set; } = new List<string>();
}
