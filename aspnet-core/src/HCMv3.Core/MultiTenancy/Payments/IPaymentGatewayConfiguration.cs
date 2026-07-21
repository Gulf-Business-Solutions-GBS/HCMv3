using Abp.Dependency;

namespace HCMv3.MultiTenancy.Payments;

public interface IPaymentGatewayConfiguration : ITransientDependency
{
    bool IsActive { get; }

    bool SupportsRecurringPayments { get; }

    SubscriptionPaymentGatewayType GatewayType { get; }
}

