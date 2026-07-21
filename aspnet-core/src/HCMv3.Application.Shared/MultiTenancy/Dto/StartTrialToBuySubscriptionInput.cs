using HCMv3.MultiTenancy.Payments;

namespace HCMv3.MultiTenancy.Dto;

public class StartTrialToBuySubscriptionInput
{
    public PaymentPeriodType PaymentPeriodType { get; set; }

    public string SuccessUrl { get; set; }

    public string ErrorUrl { get; set; }
}

