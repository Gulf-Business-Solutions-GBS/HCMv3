using Abp.Events.Bus;

namespace HCMv3.MultiTenancy.Subscription;

public class RecurringPaymentsEnabledEventData : EventData
{
    public int TenantId { get; set; }
}

