using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// HR-side maintenance of employment letter requests: raising one on an employee's behalf (the person who
/// walks into the HR office rather than opening self-service), correcting an addressee before the letter is
/// printed, and removing a request raised in error.
/// </summary>
public interface IEmployeeLetterRequestsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeLetterRequestDto>> GetAll(GetAllEmployeeLetterRequestsInput input);

    Task<GetEmployeeLetterRequestForEditOutput> GetEmployeeLetterRequestForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeLetterRequestDto input);

    Task Delete(EntityDto input);

    /// <summary>
    /// Prints the letter an approved request asked for and keeps the PDF on the request. HR's action, as in
    /// legacy: the employee is told it is ready and collects the paper.
    /// </summary>
    Task<PrintedLetterDto> PrintLetter(EntityDto input);
}
