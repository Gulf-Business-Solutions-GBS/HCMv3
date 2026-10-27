using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.EntityChanges.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.EntityChanges;

public interface IEntityChangeAppService : IApplicationService
{
    Task<ListResultDto<EntityAndPropertyChangeListDto>> GetEntityChangesByEntity(GetEntityChangesByEntityInput input);
}

