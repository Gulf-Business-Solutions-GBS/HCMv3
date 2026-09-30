using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.Dashboards.Workforce.Dto;

namespace HCMv3.Dashboards.Workforce;

/// <summary>A pay hold of one employee.</summary>
public record WorkforceHold(int EmployeeId, DateTime StartDate, DateTime EndDate);

/// <summary>An unrevoked separation.</summary>
public record WorkforceSeparation(int EmployeeId, DateTime SubmissionDate, DateTime LastDayInService);

/// <summary>A rejoin, or a final-settlement payment: one employee, one date.</summary>
public record WorkforceDatedEvent(int EmployeeId, DateTime Date);

public record WorkforceDocument(int EmployeeId, int DocumentTypeId, string DocumentNum, DateTime ExpiryDate);

public record WorkforceDocumentType(int Id, string NameEn, string NameAr, int NotificationPeriod);

/// <summary>
/// The two Workforce tiles ported from legacy's separate Employee Status and document-expiry screens: employee
/// movements and documents expiring. Pure and in-memory, over employees the app service has already scoped and
/// filtered, like <see cref="WorkforceDashboardCalculator"/>.
///
/// <para><b>Awaiting settlement</b> is someone whose last day in service has passed with no final settlement paid
/// since their separation was submitted, and who has not rejoined since. <b>Documents</b> count only each
/// employee's latest document of a type, so a renewed passport does not leave the old one showing as expired.</para>
/// </summary>
public class WorkforceActivityCalculator
{
    /// <summary>How far ahead "expiring" looks when a document type has no notification period set.</summary>
    public const int DefaultNotificationDays = 30;

    private static readonly WorkforceMovementKind[] KindOrder =
    {
        WorkforceMovementKind.OnHold, WorkforceMovementKind.Rejoined, WorkforceMovementKind.Separated,
        WorkforceMovementKind.Settled, WorkforceMovementKind.AwaitingSettlement
    };

    private readonly DateTime _from;
    private readonly DateTime _asOf;
    private readonly DashboardOrgTree _tree;
    private readonly IReadOnlyDictionary<int, WorkforceEmployee> _population;
    private readonly IReadOnlyDictionary<int, WorkforceDocumentType> _documentTypes;

    public WorkforceActivityCalculator(
        DateTime from,
        DateTime asOf,
        DashboardOrgTree tree,
        IEnumerable<WorkforceEmployee> population,
        IEnumerable<WorkforceDocumentType> documentTypes)
    {
        _from = from.Date;
        _asOf = asOf.Date;
        _tree = tree;
        _population = population.ToDictionary(e => e.Id);
        _documentTypes = documentTypes.ToDictionary(t => t.Id);
    }

    // ---------------------------------------------------------------- movements

    /// <summary>Every movement of the population, one row per employee and kind.</summary>
    public List<WorkforceMovementRowDto> Movements(
        IReadOnlyCollection<WorkforceHold> holds,
        IReadOnlyCollection<WorkforceDatedEvent> rejoins,
        IReadOnlyCollection<WorkforceSeparation> separations,
        IReadOnlyCollection<WorkforceDatedEvent> settlements)
    {
        var rows = new List<WorkforceMovementRowDto>();
        bool InPeriod(DateTime d) => d.Date >= _from && d.Date <= _asOf;

        foreach (var g in holds.Where(h => _population.ContainsKey(h.EmployeeId) && h.StartDate.Date <= _asOf && h.EndDate.Date >= _asOf)
                     .GroupBy(h => h.EmployeeId))
        {
            var hold = g.OrderByDescending(h => h.StartDate).First();
            rows.Add(Row(g.Key, WorkforceMovementKind.OnHold, hold.StartDate, hold.EndDate));
        }

        foreach (var g in rejoins.Where(r => _population.ContainsKey(r.EmployeeId) && InPeriod(r.Date)).GroupBy(r => r.EmployeeId))
        {
            rows.Add(Row(g.Key, WorkforceMovementKind.Rejoined, g.Max(r => r.Date)));
        }

        var inPopulation = separations.Where(s => _population.ContainsKey(s.EmployeeId)).ToList();
        foreach (var g in inPopulation.Where(s => InPeriod(s.LastDayInService)).GroupBy(s => s.EmployeeId))
        {
            rows.Add(Row(g.Key, WorkforceMovementKind.Separated, g.Max(s => s.LastDayInService)));
        }

        foreach (var g in settlements.Where(p => _population.ContainsKey(p.EmployeeId) && InPeriod(p.Date)).GroupBy(p => p.EmployeeId))
        {
            rows.Add(Row(g.Key, WorkforceMovementKind.Settled, g.Max(p => p.Date)));
        }

        var settledBy = settlements.ToLookup(p => p.EmployeeId);
        var rejoinedBy = rejoins.ToLookup(r => r.EmployeeId);
        foreach (var g in inPopulation.Where(s => s.LastDayInService.Date <= _asOf).GroupBy(s => s.EmployeeId))
        {
            var latest = g.OrderByDescending(s => s.LastDayInService).First();
            var settled = settledBy[g.Key].Any(p => p.Date.Date >= latest.SubmissionDate.Date);
            var rejoined = rejoinedBy[g.Key].Any(r => r.Date.Date > latest.LastDayInService.Date);
            if (!settled && !rejoined)
            {
                rows.Add(Row(g.Key, WorkforceMovementKind.AwaitingSettlement, latest.LastDayInService));
            }
        }

        return rows;
    }

    /// <summary>Every kind, in a fixed order, so the chart keeps its shape.</summary>
    public static List<WorkforceMovementCountDto> Counts(IEnumerable<WorkforceMovementRowDto> rows)
    {
        var byKind = rows.GroupBy(r => r.Kind).ToDictionary(g => g.Key, g => g.Count());
        return KindOrder.Select(k => new WorkforceMovementCountDto { Kind = k, Count = byKind.GetValueOrDefault(k) }).ToList();
    }

    private WorkforceMovementRowDto Row(int employeeId, WorkforceMovementKind kind, DateTime date, DateTime? endDate = null)
    {
        var e = _population[employeeId];
        return new WorkforceMovementRowDto
        {
            EmployeeId = e.Id,
            EmployeeCode = e.Code,
            EmployeeNameEn = e.NameEn,
            EmployeeNameAr = e.NameAr,
            OrganizationUnitName = e.OrganizationUnitId is long unitId ? _tree.NameOf(unitId) : null,
            Kind = kind,
            Date = date.Date,
            EndDate = endDate?.Date
        };
    }

    // ---------------------------------------------------------------- documents

    public int NotificationDaysOf(int documentTypeId) =>
        _documentTypes.TryGetValue(documentTypeId, out var t) && t.NotificationPeriod > 0 ? t.NotificationPeriod : DefaultNotificationDays;

    /// <summary>
    /// Each employed person's latest document of each type that has expired by the as-of date or expires within its
    /// type's notification period after it. Soonest first.
    /// </summary>
    public List<WorkforceDocumentRowDto> Documents(IReadOnlyCollection<WorkforceDocument> documents)
    {
        return documents
            .Where(d => _population.TryGetValue(d.EmployeeId, out var e) && WorkforceDashboardCalculator.IsEmployedOn(e, _asOf))
            .GroupBy(d => (d.EmployeeId, d.DocumentTypeId))
            .Select(g => g.OrderByDescending(d => d.ExpiryDate).First())
            .Select(d => (Document: d, DaysLeft: (int)(d.ExpiryDate.Date - _asOf).TotalDays))
            .Where(x => x.DaysLeft <= NotificationDaysOf(x.Document.DocumentTypeId))
            .Select(x =>
            {
                var e = _population[x.Document.EmployeeId];
                _documentTypes.TryGetValue(x.Document.DocumentTypeId, out var type);
                return new WorkforceDocumentRowDto
                {
                    EmployeeId = e.Id,
                    EmployeeCode = e.Code,
                    EmployeeNameEn = e.NameEn,
                    EmployeeNameAr = e.NameAr,
                    OrganizationUnitName = e.OrganizationUnitId is long unitId ? _tree.NameOf(unitId) : null,
                    DocumentTypeId = x.Document.DocumentTypeId,
                    DocumentTypeNameEn = type?.NameEn,
                    DocumentTypeNameAr = type?.NameAr,
                    DocumentNum = x.Document.DocumentNum,
                    ExpiryDate = x.Document.ExpiryDate.Date,
                    DaysLeft = x.DaysLeft,
                    Status = x.DaysLeft < 0 ? WorkforceDocumentStatus.Expired : WorkforceDocumentStatus.Expiring
                };
            })
            .OrderBy(r => r.ExpiryDate)
            .ThenBy(r => r.EmployeeCode)
            .ToList();
    }

    public List<WorkforceDocumentBucketDto> DocumentBuckets(IEnumerable<WorkforceDocumentRowDto> rows)
    {
        return rows
            .GroupBy(r => r.DocumentTypeId)
            .Select(g => new WorkforceDocumentBucketDto
            {
                DocumentTypeId = g.Key,
                NameEn = g.First().DocumentTypeNameEn,
                NameAr = g.First().DocumentTypeNameAr,
                NotificationDays = NotificationDaysOf(g.Key),
                Expired = g.Count(r => r.Status == WorkforceDocumentStatus.Expired),
                Expiring = g.Count(r => r.Status == WorkforceDocumentStatus.Expiring)
            })
            .OrderByDescending(b => b.Expired + b.Expiring)
            .ThenBy(b => b.NameEn)
            .ToList();
    }
}
