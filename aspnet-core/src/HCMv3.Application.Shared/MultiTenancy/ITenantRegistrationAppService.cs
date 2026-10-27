using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Editions.Dto;
using HCMv3.MultiTenancy.Dto;

namespace HCMv3.MultiTenancy;

public interface ITenantRegistrationAppService : IApplicationService
{
    Task<RegisterTenantOutput> RegisterTenant(RegisterTenantInput input);

    Task<EditionsSelectOutput> GetEditionsForSelect();

    Task<EditionSelectDto> GetEdition(int editionId);

    Task BuyNowSucceed(long paymentId);

    Task NewRegistrationSucceed(long paymentId);

    Task UpgradeSucceed(long paymentId);

    Task ExtendSucceed(long paymentId);
}

