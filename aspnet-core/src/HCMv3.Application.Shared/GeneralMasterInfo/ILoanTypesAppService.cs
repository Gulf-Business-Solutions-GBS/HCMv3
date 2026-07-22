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
}
