using System;
using System.Threading.Tasks;
using HCMv3.AiAgent.Tools;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.AiAgent.Actions;

/// <summary>
/// Records a one-off earning or deduction an administrator confirmed, through the Employee Payments service --
/// the direct route, chosen over the payment-batch workflow because a batch requires the person entering it to
/// be linked to an employee record, and the administrator deliberately never is.
/// </summary>
public class CreateOneTimePaymentAiPendingActionHandler : AiPendingActionHandlerBase
{
    public const string Type = "CreateOneTimePayment";

    private const int MaxComments = 500;

    private readonly IEmployeePaymentsAppService _payments;

    public CreateOneTimePaymentAiPendingActionHandler(IEmployeePaymentsAppService payments)
    {
        _payments = payments;
    }

    public override string ActionType => Type;

    public override AiPendingActionPreview Describe(string payloadJson)
    {
        var payload = Read<Payload>(payloadJson);

        return new AiPendingActionPreview
            {
                Title = L(payload.BillingType == BillingType.Deduction ? "AiAgentActionOneTimeDeduction" : "AiAgentActionOneTimePayment")
            }
            .Add(L("AiAgentEmployee"), $"{payload.EmployeeCode} · {PickName(payload.EmployeeNameEn, payload.EmployeeNameAr)}")
            .Add(L("AiAgentWageType"), $"{payload.WageTypeCode} · {PickName(payload.WageTypeNameEn, payload.WageTypeNameAr)}")
            .Add(L("AiAgentAmount"), string.IsNullOrEmpty(payload.CurrencyCode)
                ? AiPayLookup.FormatAmount(payload.Amount)
                : $"{AiPayLookup.FormatAmount(payload.Amount)} {payload.CurrencyCode}")
            .Add(L("AiAgentPaymentDate"), FormatDate(payload.PaymentDate))
            .Add(L("AiAgentComments"), payload.Comments);
    }

    public override async Task<string> ExecuteAsync(string payloadJson)
    {
        var payload = Read<Payload>(payloadJson);

        await _payments.CreateOrEdit(new CreateOrEditEmployeePaymentDto
        {
            EmployeeId = payload.EmployeeId,
            WageTypeId = payload.WageTypeId,
            Amount = payload.Amount,
            CurrencyId = payload.CurrencyId,
            PaymentDate = payload.PaymentDate,
            UnitType = UnitType.Amount,
            Comments = payload.Comments == null || payload.Comments.Length <= MaxComments
                ? payload.Comments
                : payload.Comments.Substring(0, MaxComments)
        });

        return L(payload.BillingType == BillingType.Deduction ? "AiAgentDeductionCreated" : "AiAgentPaymentCreated", payload.EmployeeCode);
    }

    public class Payload
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string EmployeeNameEn { get; set; }

        public string EmployeeNameAr { get; set; }

        public int WageTypeId { get; set; }

        public string WageTypeCode { get; set; }

        public string WageTypeNameEn { get; set; }

        public string WageTypeNameAr { get; set; }

        public BillingType BillingType { get; set; }

        public double Amount { get; set; }

        public int? CurrencyId { get; set; }

        public string CurrencyCode { get; set; }

        public DateTime PaymentDate { get; set; }

        public string Comments { get; set; }
    }
}
