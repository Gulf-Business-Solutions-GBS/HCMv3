using System;
using System.Linq.Expressions;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// What an employee may do with a <see cref="DocumentType"/> in self-service, read from the switches on the
/// type. Stated once, as expressions, so the dropdown that offers a type and the request that accepts it are
/// the same rule and cannot drift: a filtered dropdown is a convenience, not a check.
/// </summary>
public static class DocumentTypeEssRules
{
    /// <summary>
    /// The employee may record a document of this type -- a new passport, a renewed civil ID. Offered in
    /// self-service, not hidden from employees, and not a letter (legacy <c>AllowUpdate</c>).
    /// </summary>
    public static readonly Expression<Func<DocumentType, bool>> CanBeRecordedByEmployee =
        t => t.IncludeInWorkflow && !t.IsHiddenFromEmployee && !t.IsLetterType;

    /// <summary>The employee may ask the company to issue this type as a letter.</summary>
    public static readonly Expression<Func<DocumentType, bool>> CanBeRequestedAsLetter =
        t => t.IncludeInWorkflow && t.IsLetterType;
}
