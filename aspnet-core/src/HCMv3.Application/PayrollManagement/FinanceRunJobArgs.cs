using System;
using HCMv3.PayrollManagement.Costing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Arguments of <see cref="CostingFileBackgroundJob"/> and <see cref="PostJvBackgroundJob"/>: the screen's
/// inputs, already validated and normalised, plus who started the run and its <c>ExecutionLog</c> row.
/// Legacy passed the same values as its 9-element <c>args</c> array to <c>Main</c> on a pool thread
/// (<c>uscCostingFile.ascx.cs</c> lines 239-278 / 740-776).
/// </summary>
[Serializable]
public class FinanceRunJobArgs
{
    public int TenantId { get; set; }

    public long? UserId { get; set; }

    public int ExecutionLogId { get; set; }

    public string PayrollAreaName { get; set; }

    public string EmployeeCode { get; set; }

    /// <summary>Already normalised to the last day of the month (legacy line 442).</summary>
    public DateTime PayInDate { get; set; }

    public DateTime PostingDate { get; set; }

    public PayrollRunType PayrollRunType { get; set; }

    /// <summary>Post JV only: the fingerprint of the preview the operator checked.</summary>
    public string PreviewFingerprint { get; set; }

    public static FinanceRunJobArgs From(CostingRunInput input, int tenantId, long? userId, int executionLogId,
                                         string previewFingerprint)
    {
        return new FinanceRunJobArgs
        {
            TenantId = tenantId,
            UserId = userId,
            ExecutionLogId = executionLogId,
            PayrollAreaName = input.PayrollAreaName,
            EmployeeCode = input.EmployeeCode,
            PayInDate = input.PayInDate,
            PostingDate = input.PostingDate,
            PayrollRunType = input.PayrollRunType,
            PreviewFingerprint = previewFingerprint
        };
    }

    public CostingRunInput ToInput()
    {
        return new CostingRunInput
        {
            PayrollAreaName = PayrollAreaName,
            EmployeeCode = EmployeeCode,
            PayInDate = PayInDate,
            PostingDate = PostingDate,
            PayrollRunType = PayrollRunType
        };
    }
}
