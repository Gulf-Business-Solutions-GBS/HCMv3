using System;
using System.Linq;
using HCMv3.EntityFrameworkCore;
using HCMv3.GeneralMasterInfo;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Migrations.Seed.Tenants;

/// <summary>
/// Seeds the two workflow notification configurations a tenant needs before anybody is told anything.
///
/// <para>
/// <b>Why this exists.</b> <c>WorkflowNotificationDispatcher</c> looks up a <see cref="NotificationConfig"/>
/// for the step and action, and returns without sending when it finds none. A tenant whose steps carry no
/// configuration therefore routes approvals perfectly and notifies nobody: the Approvals list fills up while
/// the bell stays silent, which reads as a broken notification system rather than an unconfigured one.
/// </para>
///
/// <para>
/// Two configurations rather than seven, one per action, because there are only two audiences in an ordinary
/// chain. Something has arrived and needs a decision, which concerns whoever it is now sitting with; or
/// something has been decided, which concerns the person who raised it. Administrators can still point any
/// step at anything else -- these are defaults, not rules.
/// </para>
///
/// <para>
/// IDEMPOTENT, keyed on (TenantId, Name) including soft-deleted rows. A tenant that has renamed or removed
/// one is left alone: re-running the seeder must not resurrect something somebody deliberately took out.
/// </para>
/// </summary>
public class WorkflowNotificationConfigBuilder
{
    // Names live in Core.Shared: the application layer looks the same rows up to default a new step at
    // one, and it cannot reference this project.
    private const string ToApproverName = WorkflowNotificationDefaults.ToApproverName;
    private const string ToRequesterName = WorkflowNotificationDefaults.ToRequesterName;

    private readonly HCMv3DbContext _context;
    private readonly int _tenantId;

    public WorkflowNotificationConfigBuilder(HCMv3DbContext context, int tenantId)
    {
        _context = context;
        _tenantId = tenantId;
    }

    public void Create()
    {
        var existing = _context.NotificationConfigs
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == _tenantId && c.Name != null)
            .Select(c => c.Name)
            .ToList()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = false;

        if (!existing.Contains(ToApproverName))
        {
            _context.NotificationConfigs.Add(new NotificationConfig
            {
                // SuppressAutoSetTenantId is on during seeding, so TenantId must be set explicitly.
                TenantId = _tenantId,
                Name = ToApproverName,

                // In-app only. Email needs an SMTP server and a template, and a default that silently tries
                // to send mail on a tenant with neither configured produces a queue of failures nobody asked
                // for. Ticking SendEmail later is one checkbox.
                SendNotification = true,
                SendEmail = false,

                // Whoever the request is pending with right now -- which is precisely the set the engine has
                // just assigned, so this follows the chain wherever it routes.
                SendToApprover = true
            });

            added = true;
        }

        if (!existing.Contains(ToRequesterName))
        {
            _context.NotificationConfigs.Add(new NotificationConfig
            {
                TenantId = _tenantId,
                Name = ToRequesterName,
                SendNotification = true,
                SendEmail = false,

                // The person who raised it. Not ToEmployee: on a request HR raised on somebody's behalf
                // those are two different people, and the one waiting to hear is the one who submitted it.
                ToRequestCreator = true
            });

            added = true;
        }

        if (added)
        {
            _context.SaveChanges();
        }
    }
}
