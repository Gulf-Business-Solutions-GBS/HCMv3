using System.Collections.Generic;
using System.Threading.Tasks;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.Ess;

public class ProxyEssLookupAppService : ProxyAppServiceBase, IEssLookupAppService
{
    public Task<List<SimpleLookupDto>> GetAbsenceTypes() => GetLookup(nameof(GetAbsenceTypes));

    public Task<List<SimpleLookupDto>> GetAssets() => GetLookup(nameof(GetAssets));

    public Task<List<SimpleLookupDto>> GetAttendanceTypes() => GetLookup(nameof(GetAttendanceTypes));

    public Task<List<SimpleLookupDto>> GetBanks() => GetLookup(nameof(GetBanks));

    public Task<List<SimpleLookupDto>> GetCurrencies() => GetLookup(nameof(GetCurrencies));

    public Task<List<SimpleLookupDto>> GetDocumentTypes() => GetLookup(nameof(GetDocumentTypes));

    public Task<List<SimpleLookupDto>> GetLetterTypes() => GetLookup(nameof(GetLetterTypes));

    public Task<List<SimpleLookupDto>> GetExpenseTypes() => GetLookup(nameof(GetExpenseTypes));

    public Task<List<SimpleLookupDto>> GetHRServiceTypes() => GetLookup(nameof(GetHRServiceTypes));

    public Task<List<SimpleLookupDto>> GetHotels() => GetLookup(nameof(GetHotels));

    public Task<List<SimpleLookupDto>> GetLoanTypes() => GetLookup(nameof(GetLoanTypes));

    public Task<List<SimpleLookupDto>> GetOvertimeTypes() => GetLookup(nameof(GetOvertimeTypes));

    public Task<List<SimpleLookupDto>> GetTicketTypes() => GetLookup(nameof(GetTicketTypes));

    public Task<List<SimpleLookupDto>> GetPunchTypes() => GetLookup(nameof(GetPunchTypes));

    public Task<List<SimpleLookupDto>> GetTravelAirlines() => GetLookup(nameof(GetTravelAirlines));

    public Task<List<SimpleLookupDto>> GetTravelDestinations() => GetLookup(nameof(GetTravelDestinations));

    public async Task<List<EssColleagueLookupDto>> GetDelegationCandidates()
    {
        return await ApiClient.GetAsync<List<EssColleagueLookupDto>>(GetEndpoint(nameof(GetDelegationCandidates)));
    }

    public async Task<List<EssWithdrawableDocumentDto>> GetMyWithdrawableDocuments()
    {
        return await ApiClient.GetAsync<List<EssWithdrawableDocumentDto>>(GetEndpoint(nameof(GetMyWithdrawableDocuments)));
    }

    private async Task<List<SimpleLookupDto>> GetLookup(string methodName)
    {
        return await ApiClient.GetAsync<List<SimpleLookupDto>>(GetEndpoint(methodName));
    }
}
