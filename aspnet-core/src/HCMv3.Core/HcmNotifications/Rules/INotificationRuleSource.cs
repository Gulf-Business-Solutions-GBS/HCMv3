using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HCMv3.BusinessRules.Services;

namespace HCMv3.HcmNotifications.Rules;

/// <summary>
/// One kind of record a Notification Master rule may be written about -- the "Notification Type". This is the
/// whitelist that replaced legacy's "any persistent class" picker: a rule can only ever reach a table some
/// source declares, and only the fields that source exposes to its criterion.
///
/// <para>
/// Each source declares the five things the design asks of it: which records it reads
/// (<see cref="GetCandidatesAsync"/>), the date each is about (<see cref="NotificationRuleCandidate.SubjectDate"/>),
/// its sub type list (<see cref="GetSubTypesAsync"/>, which -- unlike legacy -- really filters), its fields for
/// the criterion (<see cref="Criterion"/>), and how the employee is found (the record itself is the
/// recipient resolver's subject; it reads <c>EmployeeId</c> off it).
/// </para>
///
/// <para><b>Name the class <c>{Something}NotificationRuleSource</c>.</b> Castle binds a conventionally
/// registered class to its interfaces by name, so a class called <c>LoanSource</c> would register only as
/// itself, the registry would never see it, and its rules would silently never run. The registry test checks
/// every code in <c>NotificationRuleSourceCodes</c> resolves to a source -- keep it green.</para>
/// </summary>
public interface INotificationRuleSource
{
    /// <summary>Stored in <c>NotificationMaster.NotificationType</c>. One of <c>NotificationRuleSourceCodes</c>.</summary>
    string Code { get; }

    /// <summary>Localization key of the source's name in the type picker.</summary>
    string DisplayNameKey { get; }

    /// <summary>Localization key of the sub type's label (e.g. "DocumentType"); null when the source has none.</summary>
    string SubTypeDisplayNameKey { get; }

    /// <summary>
    /// What the criterion can see: a rule base object built with the business rule engine's own builder, so
    /// fields are reached through compiled delegates and a criterion cannot touch anything not declared here.
    /// Deliberately NOT registered with the business rule registry -- these are not entity-event base objects
    /// and must not appear on the Business Rules screen.
    /// </summary>
    RuleBaseObjectDescriptor Criterion { get; }

    /// <summary>The parameter the record is bound to in the criterion, e.g. <c>Document</c> in <c>Document.ExpiryDate</c>.</summary>
    string ParameterName { get; }

    /// <summary>The sub types to choose from. Empty when <see cref="SubTypeDisplayNameKey"/> is null.</summary>
    Task<List<NotificationRuleSubTypeItem>> GetSubTypesAsync();

    /// <summary>The next page of records, in id order, honouring the sub type and the sent log.</summary>
    Task<List<NotificationRuleCandidate>> GetCandidatesAsync(NotificationRuleCandidateQuery query);

    /// <summary>Puts a candidate's record into a criterion context under <see cref="ParameterName"/>.</summary>
    void Bind(RuleExecutionContext context, NotificationRuleCandidate candidate);
}

/// <summary>One record a rule may fire about.</summary>
public class NotificationRuleCandidate
{
    public long RecordId { get; set; }

    /// <summary>The entity itself: the criterion's parameter and the recipient resolver's subject.</summary>
    public object Record { get; set; }

    /// <summary>The date the record is about (an expiry date, a leave start), shown on the preview and history.</summary>
    public DateTime? SubjectDate { get; set; }

    /// <summary>A human label for the preview and history, e.g. "E0012 - Passport P123456".</summary>
    public string Label { get; set; }
}

public class NotificationRuleCandidateQuery
{
    /// <summary>Only records of this sub type. Ignored by a source with no sub type.</summary>
    public int? SubTypeId { get; set; }

    /// <summary>Skip records this rule has already logged. Null reads every record (the preview).</summary>
    public int? ExcludeLoggedForRuleId { get; set; }

    /// <summary>Paging: only records with an id above this.</summary>
    public long AfterRecordId { get; set; }

    public int MaxCount { get; set; } = NotificationRuleConsts.CandidatePageSize;
}

public class NotificationRuleSubTypeItem
{
    public int Id { get; set; }

    public string Name { get; set; }
}
