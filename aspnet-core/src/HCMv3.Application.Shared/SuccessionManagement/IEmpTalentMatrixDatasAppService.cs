using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SuccessionManagement.Dto;

namespace HCMv3.SuccessionManagement;

public interface IEmpTalentMatrixDatasAppService : IApplicationService
{
    Task<PagedResultDto<EmpTalentMatrixDataDto>> GetAll(GetAllEmpTalentMatrixDatasInput input);

    Task<GetEmpTalentMatrixDataForEditOutput> GetEmpTalentMatrixDataForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmpTalentMatrixDataDto input);

    Task Delete(EntityDto input);
}
