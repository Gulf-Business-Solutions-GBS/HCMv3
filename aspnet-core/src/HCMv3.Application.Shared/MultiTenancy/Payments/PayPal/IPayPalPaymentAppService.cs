using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.MultiTenancy.Payments.PayPal.Dto;

namespace HCMv3.MultiTenancy.Payments.PayPal;

public interface IPayPalPaymentAppService : IApplicationService
{
    Task ConfirmPayment(long paymentId, string paypalOrderId);

    PayPalConfigurationDto GetConfiguration();
}

