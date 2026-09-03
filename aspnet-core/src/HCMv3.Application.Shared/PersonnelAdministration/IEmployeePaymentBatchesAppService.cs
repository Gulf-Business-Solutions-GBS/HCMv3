using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// The HR payment batch screen: pay a one-off amount to many employees under one wage type, on one approval
/// (ported from legacy <c>Workflow.Payroll.PaymentRequest</c>).
/// </summary>
public interface IEmployeePaymentBatchesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeePaymentBatchListDto>> GetAll(GetAllEmployeePaymentBatchesInput input);

    Task<GetEmployeePaymentBatchForEditOutput> GetEmployeePaymentBatchForEdit(EntityDto input);

    /// <summary>
    /// The wage types this screen may offer, narrowed by legacy's own rule -- see
    /// <c>IEmployeePaymentBatchService.GetSelectableWageTypeIdsAsync</c>. Takes no input, so it stays a plain
    /// GET.
    /// </summary>
    Task<List<PaymentWageTypeLookupDto>> GetSelectableWageTypes();

    /// <summary>Creates the batch and submits it for approval in one step.</summary>
    Task<int> CreateEmployeePaymentBatch(CreateOrEditEmployeePaymentBatchDto input);

    /// <summary>Rewrites a batch that has not been routed yet. Refused once it has.</summary>
    Task UpdateEmployeePaymentBatch(CreateOrEditEmployeePaymentBatchDto input);

    /// <summary>Deletes a batch that has not been routed and has paid nobody. Refused once it has.</summary>
    Task Delete(EntityDto input);
}
