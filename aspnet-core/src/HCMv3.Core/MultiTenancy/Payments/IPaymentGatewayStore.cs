using System.Collections.Generic;

namespace HCMv3.MultiTenancy.Payments;

public interface IPaymentGatewayStore
{
    List<PaymentGatewayModel> GetActiveGateways();
}

