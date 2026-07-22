using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface ILoanTypeDetailsAppService : IApplicationService
{
    Task<PagedResultDto<LoanTypeDetailDto>> GetAll(GetAllLoanTypeDetailsInput input);

    Task<GetLoanTypeDetailForEditOutput> GetLoanTypeDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditLoanTypeDetailDto input);

    Task Delete(EntityDto input);
}
