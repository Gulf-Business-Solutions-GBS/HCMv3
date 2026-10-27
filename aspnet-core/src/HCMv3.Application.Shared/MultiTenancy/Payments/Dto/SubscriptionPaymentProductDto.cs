using Abp.Application.Services.Dto;
using HCMv3.ExtraProperties;

namespace HCMv3.MultiTenancy.Payments.Dto;

public class SubscriptionPaymentProductDto : EntityDto<long>
{
    public string Description { get; set; }

    public decimal Amount { get; set; }

    public int Count { get; set; }

    public ExtraPropertyDictionary ExtraProperties { get; set; }

    public decimal GetTotalAmount()
    {
        return Amount * Count;
    }
}

