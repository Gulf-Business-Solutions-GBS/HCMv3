using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IPayeeBankDetailsAppService : IApplicationService
{
    Task<PagedResultDto<PayeeBankDetailDto>> GetAll(GetAllPayeeBankDetailsInput input);

    Task<GetPayeeBankDetailForEditOutput> GetPayeeBankDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPayeeBankDetailDto input);

    Task Delete(EntityDto input);
}
