using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface ILoanTypesAppService : IApplicationService
{
    Task<PagedResultDto<LoanTypeDto>> GetAll(GetAllLoanTypesInput input);

    Task<GetLoanTypeForEditOutput> GetLoanTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditLoanTypeDto input);

    Task Delete(EntityDto input);

    /// <summary>
    /// The wage types a loan type may recover through: legacy <c>LoanType.LoanDedWageType</c>. The input's id is
    /// the loan type's current wage type, listed even if it no longer passes the filter.
    /// </summary>
    Task<ListResultDto<LoanDeductionWageTypeDto>> GetDeductionWageTypes(NullableIdDto input);
}
