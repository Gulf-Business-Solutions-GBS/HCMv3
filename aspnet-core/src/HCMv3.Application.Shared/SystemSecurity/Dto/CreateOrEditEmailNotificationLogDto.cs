using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.SystemSecurity.Dto;

public class CreateOrEditEmailNotificationLogDto
{
    public int? Id { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [StringLength(256)]
    public string Email { get; set; }

    [StringLength(256)]
    public string Source { get; set; }

    [StringLength(500)]
    public string Subject { get; set; }

    public string Body { get; set; }

    public bool IsSuccessful { get; set; }

    public string Exception { get; set; }
}
