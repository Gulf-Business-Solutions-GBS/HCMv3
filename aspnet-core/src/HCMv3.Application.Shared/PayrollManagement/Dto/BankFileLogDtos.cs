using System;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>Legacy log popup radios <c>rdLogAll</c> / <c>rdLogSuccess</c> / <c>rdLogFailed</c>.</summary>
public enum BankFileLogFilter
{
    All = 0,

    /// <summary>Status SUCCESS.</summary>
    Successful = 1,

    /// <summary>Anything other than SUCCESS -- legacy counted Failed as All minus Successful.</summary>
    Failed = 2
}

public class GetBankFileLogsInput : PagedAndSortedResultRequestDto
{
    public BankFileLogFilter StatusFilter { get; set; }
}

/// <summary>One bank-file run, as legacy's "Bank File Generation Log" popup listed it (<c>BankFileLogPopupModel</c>).</summary>
public class BankFileLogDto : EntityDto
{
    public string UserName { get; set; }

    public DateTime ActionDateTime { get; set; }

    public ExecutionLogStatus ExecutionStatus { get; set; }

    /// <summary>True for SUCCESS -- what legacy showed as "Successful", everything else "Failed".</summary>
    public bool Succeeded { get; set; }

    /// <summary>Every filter the run was made with, in legacy's "Label : value | ..." form.</summary>
    public string Parameters { get; set; }

    public int TotalRecords { get; set; }

    public int Successfull { get; set; }

    public int Failed { get; set; }

    /// <summary>The stored copy of the file(s) the run produced; null when nothing was stored.</summary>
    public int? AttachmentId { get; set; }

    public string FileName { get; set; }

    public bool HasFile { get; set; }
}

public class BankFileLogsOutput : PagedResultDto<BankFileLogDto>
{
    /// <summary>Legacy "All (n)".</summary>
    public int AllCount { get; set; }

    /// <summary>Legacy "Successful (n)".</summary>
    public int SuccessfulCount { get; set; }

    /// <summary>Legacy "Failed (n)" = All - Successful.</summary>
    public int FailedCount { get; set; }
}
