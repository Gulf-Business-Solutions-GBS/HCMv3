using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.SystemSecurity.Dto;

public class CreateOrEditBulkUploadLogDto
{
    public int? Id { get; set; }

    [Required]
    public BulkUploadLogType LogType { get; set; }

    public string Message { get; set; }

    [Required]
    public DateTime RunTimeStamp { get; set; }

    public int? EmployeeId { get; set; }

    public int Status { get; set; }

    [Required]
    public DateTime InPeriod { get; set; }

    public bool StopFlag { get; set; }

    public int NoOfEmployee { get; set; }

    public int RecordNo { get; set; }
}
