using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.SystemSecurity.Dto;

public class CreateOrEditExecutionLogDto
{
    public int? Id { get; set; }

    [StringLength(256)]
    public string UserName { get; set; }

    [Required]
    public DateTime ActionDateTime { get; set; }

    [Required]
    public ExecutionLogAction Action { get; set; }

    [Required]
    public ExecutionLogModule Module { get; set; }

    [StringLength(256)]
    public string ScreenName { get; set; }

    [StringLength(256)]
    public string ScreenCaption { get; set; }

    public string Parameters { get; set; }

    [Required]
    public ExecutionLogStatus ExecutionStatus { get; set; }

    public int TotalRecords { get; set; }

    public int Successfull { get; set; }

    public int Failed { get; set; }
}
