using System.Collections.Generic;

namespace HCMv3.Workflow;

/// <summary>
/// The tables a transaction type's sub type can come from -- the values
/// <c>WorkflowTransactionType.SubTypeSource</c> may hold. The same list as legacy's
/// <c>WorkflowGroupDetail</c> sub-type sources (Leave -> AbsenceType, Document -> DocumentType ...), reduced to
/// those that are lookup tables in HCMv3. Each is a tenant table with Code, NameEn and NameAr.
/// </summary>
public static class WorkflowSubTypeSources
{
    public const string AbsenceType = "AbsenceType";
    public const string AttendanceType = "AttendanceType";
    public const string ClearanceRequestType = "ClearanceRequestType";
    public const string DisciplinaryType = "DisciplinaryType";
    public const string DocumentType = "DocumentType";
    public const string Duty = "Duty";
    public const string FormType = "FormType";
    public const string HRServiceType = "HRServiceType";
    public const string ITServiceType = "ITServiceType";
    public const string LoanType = "LoanType";
    public const string PersonalAction = "PersonalAction";
    public const string TransType = "TransType";
    public const string TravelType = "TravelType";

    public static readonly IReadOnlyList<string> All = new[]
    {
        AbsenceType, AttendanceType, ClearanceRequestType, DisciplinaryType, DocumentType, Duty, FormType,
        HRServiceType, ITServiceType, LoanType, PersonalAction, TransType, TravelType
    };
}
