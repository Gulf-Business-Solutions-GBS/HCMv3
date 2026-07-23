using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IAbsencePaymentsAppService : IApplicationService
{
    Task<PagedResultDto<AbsencePaymentDto>> GetAll(GetAllAbsencePaymentsInput input);

    Task<GetAbsencePaymentForEditOutput> GetAbsencePaymentForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditAbsencePaymentDto input);

    Task Delete(EntityDto input);
}
