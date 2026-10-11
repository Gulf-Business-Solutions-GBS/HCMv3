using System;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Legacy <c>EmployeeDocument.IsActive</c>: a document's status is worked out, never stored. Nothing "makes"
/// an old passport inactive -- it reads as inactive from the moment a newer one of the same type exists.
/// One place for the rule, so HR's screen and the employee's own list cannot disagree about it.
/// </summary>
public static class DocumentStatusCalculator
{
    /// <param name="issueDate">The document's own issue date.</param>
    /// <param name="expiryDate">The document's own expiry date; unset means it does not expire.</param>
    /// <param name="latestOtherIssueDate">The latest issue date among the employee's OTHER documents of the
    /// same type, or null when there are none.</param>
    /// <param name="today">The day the status is being read on.</param>
    public static DocumentStatus Calculate(
        DateTime issueDate,
        DateTime expiryDate,
        DateTime? latestOtherIssueDate,
        DateTime today)
    {
        if (latestOtherIssueDate.HasValue
            && latestOtherIssueDate.Value != default
            && latestOtherIssueDate.Value > issueDate)
        {
            return DocumentStatus.Inactive;
        }

        if (expiryDate == default || (issueDate <= today && expiryDate >= today))
        {
            return DocumentStatus.Active;
        }

        return DocumentStatus.Expired;
    }
}
