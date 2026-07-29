using System;
using Abp.Application.Services.Dto;

namespace HCMv3.SystemSecurity.Dto;

public class BulkUploadLogDto : EntityDto<int>
{
    public BulkUploadLogType LogType { get; set; }
    public string Message { get; set; }
    public DateTime RunTimeStamp { get; set; }
    public int? EmployeeId { get; set; }
    public string EmployeeFullName { get; set; }
    public int Status { get; set; }
    public DateTime InPeriod { get; set; }
    public bool StopFlag { get; set; }
    public int NoOfEmployee { get; set; }
    public int RecordNo { get; set; }
}
