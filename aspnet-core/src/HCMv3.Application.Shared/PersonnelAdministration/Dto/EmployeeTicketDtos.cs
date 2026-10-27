using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeTicketDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int TicketTypeId { get; set; }
    public string TicketTypeName { get; set; }
    public DateTime TicketDate { get; set; }
    public DateTime PaymentDate { get; set; }
    public double Compensate { get; set; }
    public TicketPaymentType TicketPaymentType { get; set; }
    public double Amount { get; set; }
    public int AdvancedTicketPaid { get; set; }
    public string Comments { get; set; }
    public bool OverrideAmount { get; set; }
}

public class CreateOrEditEmployeeTicketDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int TicketTypeId { get; set; }

    [Required]
    public DateTime TicketDate { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    public double Compensate { get; set; }

    public TicketPaymentType TicketPaymentType { get; set; }

    public double Amount { get; set; }

    public int AdvancedTicketPaid { get; set; }

    [StringLength(4000)]
    public string Comments { get; set; }

    public bool OverrideAmount { get; set; }

    /// <summary>
    /// Record this as final instead of sending it for approval.
    ///
    /// <para>
    /// Offered only to a user holding <c>RecordWithoutApproval</c>, and only on a type whose catalog row
    /// permits a bypass. The server checks both and refuses otherwise, so this being true is a request
    /// rather than a decision.
    /// </para>
    /// </summary>
    public bool RecordDirectly { get; set; }
}

public class GetEmployeeTicketForEditOutput
{
    public CreateOrEditEmployeeTicketDto EmployeeTicket { get; set; }
}

public class GetAllEmployeeTicketsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "PaymentDate DESC";
        }
    }
}
