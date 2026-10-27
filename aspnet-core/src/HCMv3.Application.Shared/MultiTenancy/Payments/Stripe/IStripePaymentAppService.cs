using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.MultiTenancy.Payments.Dto;
using HCMv3.MultiTenancy.Payments.Stripe.Dto;

namespace HCMv3.MultiTenancy.Payments.Stripe;

public interface IStripePaymentAppService : IApplicationService
{
    Task ConfirmPayment(StripeConfirmPaymentInput input);

    StripeConfigurationDto GetConfiguration();

    Task<string> CreatePaymentSession(StripeCreatePaymentSessionInput input);
}

