using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.Ess;

/// <summary>
/// The reference lists every self-service form needs to fill its dropdowns.
///
/// <para>
/// This service exists because the admin services that own these lists are gated behind administrator
/// permissions -- <c>Pages_GeneralMasterInfo_Banks</c>, <c>Pages_PayrollManagement_AbsenceTypes</c> and the
/// rest. An employee holds none of them, so every self-service dropdown was empty or refused outright. The
/// fix is a new narrow door rather than a wider existing one: the admin services keep their permissions and
/// their full DTOs untouched, and this service hands out id-and-name only, behind
/// <c>AppPermissions.Ess</c>, which every self-service user holds.
/// </para>
///
/// <para>
/// Every method is read-only, unpaged and returns a short list, following <c>MyTravelAppService.GetTravelTypes</c>
/// and <c>MyClearanceAppService.GetClearanceTypes</c> -- the two lookups that were already written this way.
/// </para>
/// </summary>
public interface IEssLookupAppService : IApplicationService
{
    /// <summary>Leave types, for the leave request form.</summary>
    Task<List<SimpleLookupDto>> GetAbsenceTypes();

    /// <summary>Company assets an employee may ask to be issued.</summary>
    Task<List<SimpleLookupDto>> GetAssets();

    /// <summary>Attendance correction reasons.</summary>
    Task<List<SimpleLookupDto>> GetAttendanceTypes();

    /// <summary>Banks, for a change-of-bank-details request.</summary>
    Task<List<SimpleLookupDto>> GetBanks();

    /// <summary>Currencies, for travel cost lines.</summary>
    Task<List<SimpleLookupDto>> GetCurrencies();

    /// <summary>Every document type, for the "issue me a copy of X" request.</summary>
    Task<List<SimpleLookupDto>> GetDocumentTypes();

    /// <summary>
    /// Only the document types that are letters -- salary certificates and the like.
    ///
    /// <para>
    /// A separate method rather than a flag on <see cref="GetDocumentTypes"/>: the two callers are different
    /// screens asking different questions, and a boolean query parameter on a lookup is the kind of thing
    /// that gets passed the wrong way round. Mirrors <c>DocumentTypesAppService.GetAll</c>'s
    /// <c>OnlyLetterTypes</c>, which filters on <c>DocumentType.IsLetterType</c>.
    /// </para>
    /// </summary>
    Task<List<SimpleLookupDto>> GetLetterTypes();

    /// <summary>Expense types, for travel cost lines.</summary>
    Task<List<SimpleLookupDto>> GetExpenseTypes();

    /// <summary>The HR service catalogue.</summary>
    Task<List<SimpleLookupDto>> GetHRServiceTypes();

    /// <summary>Hotels, for a travel leg's optional accommodation.</summary>
    Task<List<SimpleLookupDto>> GetHotels();

    /// <summary>Loan types.</summary>
    Task<List<SimpleLookupDto>> GetLoanTypes();

    /// <summary>Overtime types.</summary>
    Task<List<SimpleLookupDto>> GetOvertimeTypes();

    /// <summary>Ticket types.</summary>
    Task<List<SimpleLookupDto>> GetTicketTypes();

    /// <summary>
    /// Punch (clock transaction) types, for a missing-punch request. Backed by <c>TransType</c>; named for
    /// what the self-service screen calls it rather than for the table.
    /// </summary>
    Task<List<SimpleLookupDto>> GetPunchTypes();

    /// <summary>Airlines, for a travel leg's optional flight.</summary>
    Task<List<SimpleLookupDto>> GetTravelAirlines();

    /// <summary>Departure and arrival points for a travel leg.</summary>
    Task<List<SimpleLookupDto>> GetTravelDestinations();

    /// <summary>
    /// Colleagues the signed-in employee may delegate approval duties to: active employees in their tenant,
    /// minus themselves.
    /// </summary>
    Task<List<EssColleagueLookupDto>> GetDelegationCandidates();
}
