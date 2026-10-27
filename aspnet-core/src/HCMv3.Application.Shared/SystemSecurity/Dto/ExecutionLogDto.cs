using System;
using Abp.Application.Services.Dto;

namespace HCMv3.SystemSecurity.Dto;

public class ExecutionLogDto : EntityDto<int>
{
    public string UserName { get; set; }
    public DateTime ActionDateTime { get; set; }
    public ExecutionLogAction Action { get; set; }
    public ExecutionLogModule Module { get; set; }
    public string ScreenName { get; set; }
    public string ScreenCaption { get; set; }
    public string Parameters { get; set; }
    public ExecutionLogStatus ExecutionStatus { get; set; }
    public int TotalRecords { get; set; }
    public int Successfull { get; set; }
    public int Failed { get; set; }
}
