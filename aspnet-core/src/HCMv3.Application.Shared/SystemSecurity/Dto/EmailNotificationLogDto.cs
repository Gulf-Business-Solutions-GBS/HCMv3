using System;
using Abp.Application.Services.Dto;

namespace HCMv3.SystemSecurity.Dto;

public class EmailNotificationLogDto : EntityDto<int>
{
    public DateTime Date { get; set; }
    public string Email { get; set; }
    public string Source { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public bool IsSuccessful { get; set; }
    public string Exception { get; set; }
}
