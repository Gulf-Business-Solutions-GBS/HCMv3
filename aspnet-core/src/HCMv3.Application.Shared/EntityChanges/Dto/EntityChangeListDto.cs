using Abp.Application.Services.Dto;
using Abp.Events.Bus.Entities;
using System;

namespace HCMv3.EntityChanges.Dto;

public class EntityChangeListDto : EntityDto<long>
{
    public long? UserId { get; set; }

    public string UserName { get; set; }

    public DateTime ChangeTime { get; set; }

    public string EntityTypeFullName { get; set; }

    public EntityChangeType ChangeType { get; set; }

    public string ChangeTypeName => ChangeType.ToString();

    public long EntityChangeSetId { get; set; }

    public string EntityId { get; set; }

    public string ImpersonatorUserName { get; set; }
}

