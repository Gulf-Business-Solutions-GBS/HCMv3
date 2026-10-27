using Abp.Application.Services.Dto;
using Abp.Domain.Repositories;
using Abp.EntityHistory;
using Microsoft.EntityFrameworkCore;
using HCMv3.Authorization.Users;
using HCMv3.EntityChanges.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace HCMv3.EntityChanges;

public class EntityChangeAppService : HCMv3AppServiceBase, IEntityChangeAppService
{
    private readonly IRepository<EntityChange, long> _entityChangeRepository;
    private readonly IRepository<EntityChangeSet, long> _entityChangeSetRepository;
    private readonly IRepository<EntityPropertyChange, long> _entityPropertyChangeRepository;
    private readonly IRepository<User, long> _userRepository;

    public EntityChangeAppService(
        IRepository<EntityChange, long> entityChangeRepository,
        IRepository<EntityChangeSet, long> entityChangeSetRepository,
        IRepository<User, long> userRepository,
        IRepository<EntityPropertyChange, long> entityPropertyChangeRepository)
    {
        _entityChangeRepository = entityChangeRepository;
        _entityChangeSetRepository = entityChangeSetRepository;
        _userRepository = userRepository;
        _entityPropertyChangeRepository = entityPropertyChangeRepository;
    }

    public async Task<ListResultDto<EntityAndPropertyChangeListDto>> GetEntityChangesByEntity(GetEntityChangesByEntityInput input)
    {
        var entityId = "\"" + input.EntityId + "\"";

        var query = from entityChange in _entityChangeRepository.GetAll()
                    join entityChangeSet in _entityChangeSetRepository.GetAll() on entityChange.EntityChangeSetId equals entityChangeSet.Id
                    join user in _userRepository.GetAll() on entityChangeSet.UserId equals user.Id
                    join entityPropertyChange in _entityPropertyChangeRepository.GetAll() on entityChange.Id equals entityPropertyChange.EntityChangeId into propertyChanges
                    select new EntityChangePropertyAndUser
                    {
                        EntityChange = entityChange,
                        EntityChangeSet = entityChangeSet,
                        User = user,
                        PropertyChanges = propertyChanges.ToList()
                    };

        var filteredQuery = query.Where(q => q.EntityChange.EntityTypeFullName == input.EntityTypeFullName &&
                                                           (q.EntityChange.EntityId == input.EntityId || q.EntityChange.EntityId == entityId));

        var results = await filteredQuery
            .OrderByDescending(q => q.EntityChange.ChangeTime)
            .Select(q => new EntityChangePropertyAndUser
            {
                EntityChange = q.EntityChange,
                EntityChangeSet = q.EntityChangeSet,
                User = q.User,
                PropertyChanges = q.PropertyChanges
            })
            .ToListAsync();

        var dtoList = ConvertToEntityAndPropertyChangeListDtos(results);

        return new ListResultDto<EntityAndPropertyChangeListDto>(dtoList);
    }

    private List<EntityAndPropertyChangeListDto> ConvertToEntityAndPropertyChangeListDtos(List<EntityChangePropertyAndUser> results)
    {
        var impersonatorUsers = GetImpersonatorUserNames(results);

        return results.Select(entityChange =>
        {
            var entityChangeDto = ObjectMapper.Map<EntityChangeListDto>(entityChange.EntityChange);
            entityChangeDto.UserName = entityChange.User?.UserName;

            entityChangeDto.ImpersonatorUserName = entityChange.EntityChangeSet.ImpersonatorUserId.HasValue
                ? impersonatorUsers.GetValueOrDefault(entityChange.EntityChangeSet.ImpersonatorUserId.Value)
                : null;

            var entityPropertyChangeDtos = entityChange.PropertyChanges?
                .Select(epc => ObjectMapper.Map<EntityPropertyChangeDto>(epc))
                .ToList() ?? new List<EntityPropertyChangeDto>();

            return new EntityAndPropertyChangeListDto
            {
                EntityChange = entityChangeDto,
                EntityPropertyChanges = entityPropertyChangeDtos
            };
        }).ToList();
    }

    private Dictionary<long, string> GetImpersonatorUserNames(List<EntityChangePropertyAndUser> results)
    {
        var tenantSpecificUserIds = results
            .Where(e => e.EntityChangeSet.ImpersonatorUserId.HasValue && e.EntityChangeSet.ImpersonatorTenantId.HasValue)
            .Select(e => e.EntityChangeSet.ImpersonatorUserId!.Value)
            .Distinct()
            .ToList();

        var crossTenantUserIds = results
            .Where(e => e.EntityChangeSet.ImpersonatorUserId.HasValue && !e.EntityChangeSet.ImpersonatorTenantId.HasValue)
            .Select(e => e.EntityChangeSet.ImpersonatorUserId!.Value)
            .Distinct()
            .ToList();

        Dictionary<long, string> impersonatorUsers = new();

        if (tenantSpecificUserIds.Any())
        {
            var tenantUsers = _userRepository.GetAll()
                .Where(u => tenantSpecificUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToDictionary(u => u.Id, u => u.UserName);

            foreach (var user in tenantUsers)
            {
                impersonatorUsers[user.Key] = user.Value;
            }
        }

        if (crossTenantUserIds.Any())
        {
            using (CurrentUnitOfWork.SetTenantId(null))
            {
                var crossTenantUsers = _userRepository.GetAll()
                    .Where(u => crossTenantUserIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.UserName })
                    .ToDictionary(u => u.Id, u => u.UserName);

                foreach (var user in crossTenantUsers)
                {
                    impersonatorUsers[user.Key] = user.Value;
                }
            }
        }

        return impersonatorUsers;
    }
}