using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Ess.Dto;

/// <summary>
/// One of the employee's own trips as the self-service list shows it, legs and cost lines included.
///
/// <para>
/// The legs come back with the header rather than from a second call, because a trip without its legs says
/// nothing useful -- "Travel, 3-7 March" is not something an employee can check, and the legs are what they
/// are actually asking for.
/// </para>
/// </summary>
public class MyTravelDto : MyRequestDtoBase
{
    /// <summary>The travel desk's own reference. Null on a trip raised through self service until the desk
    /// assigns one -- while it is in flight the workflow request number identifies it.</summary>
    public string Code { get; set; }

    public int TravelTypeId { get; set; }

    public string TravelTypeNameEn { get; set; }

    public string TravelTypeNameAr { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>Summed from the cost lines at read time.</summary>
    public double TotalTravelCost { get; set; }

    /// <summary>Why the trip is being made -- the question the approver is really answering.</summary>
    public string TravelPurpose { get; set; }

    public string Comments { get; set; }

    public List<MyTravelLegDto> Legs { get; set; }
}

/// <summary>One leg of a trip -- a single departure/arrival pair and everything to be arranged for it.</summary>
public class MyTravelLegDto
{
    public int Id { get; set; }

    public int DepartureId { get; set; }

    public string DepartureNameEn { get; set; }

    public string DepartureNameAr { get; set; }

    public int ArrivalId { get; set; }

    public string ArrivalNameEn { get; set; }

    public string ArrivalNameAr { get; set; }

    public DateTime DepartureDate { get; set; }

    public DateTime DepartureTime { get; set; }

    public DateTime ArrivalDate { get; set; }

    public DateTime ArrivalTime { get; set; }

    public int? HotelId { get; set; }

    public int? TravelAirlineId { get; set; }

    public bool NightStay { get; set; }

    public int TotalNights { get; set; }

    public double TotalAmount { get; set; }

    public bool IsVisaRequired { get; set; }

    public string VisaComments { get; set; }

    public bool IsHotelRequired { get; set; }

    public string HotelComments { get; set; }

    public bool AnyOtherExpense { get; set; }

    public string OtherComments { get; set; }

    public string Comments { get; set; }

    public List<MyTravelCostDto> Costs { get; set; }
}

/// <summary>One expected cost line on a leg.</summary>
public class MyTravelCostDto
{
    public int Id { get; set; }

    public int ExpenseTypeId { get; set; }

    public string ExpenseTypeNameEn { get; set; }

    public int? CurrencyId { get; set; }

    public double Amount { get; set; }

    public string Comments { get; set; }
}

/// <summary>A travel type, reduced to what the request form needs to let the employee pick one.</summary>
public class TravelTypeLookupDto
{
    public int Id { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }
}

/// <summary>
/// What an employee fills in to ask for a trip.
///
/// <para>
/// The legs -- and their cost lines -- come in WITH the header, in one call. Not a convenience: a trip with
/// no legs is not a request anybody can approve, and the handler refuses one at submit. Since the row is
/// created and submitted in a single unit of work, the children have to be part of the same entity graph or
/// they would not exist by the time that check runs.
/// </para>
/// <para>
/// There is no employee field, deliberately: the request is attributed to whoever is signed in.
/// </para>
/// </summary>
public class RequestTravelInput
{
    [Range(1, int.MaxValue)]
    public int TravelTypeId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [StringLength(4000)]
    public string TravelPurpose { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }

    /// <summary>At least one. Refused outright when empty.</summary>
    [Required]
    [MinLength(1)]
    public List<RequestTravelLegInput> Legs { get; set; }
}

/// <summary>One leg of the requested trip.</summary>
public class RequestTravelLegInput
{
    [Range(1, int.MaxValue)]
    public int DepartureId { get; set; }

    [Range(1, int.MaxValue)]
    public int ArrivalId { get; set; }

    public DateTime DepartureDate { get; set; }

    public DateTime DepartureTime { get; set; }

    public DateTime ArrivalDate { get; set; }

    public DateTime ArrivalTime { get; set; }

    public int? HotelId { get; set; }

    /// <summary>
    /// The airline. Present on the legacy request and simply never copied onto the TravelDetail it created
    /// -- so the employee named an airline and the travel desk never received it.
    /// </summary>
    public int? TravelAirlineId { get; set; }

    public bool NightStay { get; set; }

    [Range(0, 366)]
    public int TotalNights { get; set; }

    public bool IsVisaRequired { get; set; }

    [StringLength(500)]
    public string VisaComments { get; set; }

    public bool IsHotelRequired { get; set; }

    [StringLength(500)]
    public string HotelComments { get; set; }

    public bool AnyOtherExpense { get; set; }

    [StringLength(500)]
    public string OtherComments { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }

    public List<RequestTravelCostInput> Costs { get; set; }
}

/// <summary>One expected cost line on a requested leg.</summary>
public class RequestTravelCostInput
{
    [Range(1, int.MaxValue)]
    public int ExpenseTypeId { get; set; }

    /// <summary>
    /// What the amount is in. Present on the legacy request and never copied across either, which silently
    /// reinterpreted every foreign-currency estimate as company currency.
    /// </summary>
    public int? CurrencyId { get; set; }

    [Range(0, 9999999)]
    public double Amount { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}
