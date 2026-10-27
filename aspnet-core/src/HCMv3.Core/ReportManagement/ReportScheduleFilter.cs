using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.ReportManagement;

/// <summary>
/// One saved parameter value for a <see cref="ReportSchedule"/> -- the "Filter" step of the wizard.
///
/// Stored as name/value strings rather than typed columns because the two report families parameterise
/// completely differently: a grid report takes a filter DTO whose properties are known only to its app
/// service, while a print report takes DevExpress <c>Parameters</c> whose names and CLR types differ per
/// report. Coercion happens at run time, where the target type is known.
///
/// The burst key is NOT stored here. It is computed per slice and injected at render time, which is
/// what lets one schedule produce many differently-filtered outputs.
/// </summary>
public class ReportScheduleFilter : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int ReportScheduleId { get; set; }

    public virtual ReportSchedule ReportSchedule { get; set; }

    /// <summary>Grid report: the filter DTO property name. Print report: the DevExpress parameter name.</summary>
    [Required]
    [StringLength(128)]
    public virtual string ParameterName { get; set; }

    /// <summary>
    /// Invariant-culture string form of the value. Null is meaningful and distinct from empty: it means
    /// "leave this parameter at its declared default".
    /// </summary>
    [StringLength(1024)]
    public virtual string ParameterValue { get; set; }

    /// <summary>
    /// Relative-date token (for example <c>PreviousMonth</c>, <c>CurrentMonth</c>) resolved at run time.
    /// A recurring schedule must not be pinned to the month it was authored in -- without this, a
    /// monthly attendance report would keep re-sending the same period forever.
    /// </summary>
    [StringLength(64)]
    public virtual string RelativeDateToken { get; set; }
}
