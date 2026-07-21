using System;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.MultiTenancy;

namespace HCMv3.Authorization.Users;

[Table("UserAccountLinks")]
[MultiTenancySide(MultiTenancySides.Host)]
public class UserAccountLink : Entity<long>, IHasCreationTime
{
	public long UserAccountId { get; set; }

	public long LinkedUserAccountId { get; set; }

	public DateTime CreationTime { get; set; }
}


