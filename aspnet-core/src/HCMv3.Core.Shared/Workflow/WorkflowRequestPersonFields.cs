using System;

namespace HCMv3.Workflow;

/// <summary>
/// How a "Person on Request" step names the field its approver comes from: <c>{RequestType}:{PropertyName}</c>,
/// for example <c>EmployeeDocumentMvmt:GuarantorId</c>.
///
/// <para>
/// Nothing here lists the fields. They are discovered from each transaction type's own entity (see
/// <c>RequestPersonFieldDiscovery</c>), so a new employee field on a transaction is offered without any change
/// to this file or to the engine. The request type is part of the code because a chain can serve several
/// types: a step naming the passport withdrawal's guarantor is simply skipped for a leave that uses the chain.
/// </para>
/// </summary>
public static class WorkflowRequestPersonFields
{
    public const int MaxCodeLength = 128;

    private const char Separator = ':';

    public static string Compose(string requestType, string propertyName) => requestType + Separator + propertyName;

    /// <summary>Splits a stored code. False for anything that is not <c>{RequestType}:{PropertyName}</c>.</summary>
    public static bool TryParse(string code, out string requestType, out string propertyName)
    {
        requestType = null;
        propertyName = null;

        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var at = code.IndexOf(Separator);
        if (at <= 0 || at == code.Length - 1)
        {
            return false;
        }

        requestType = code.Substring(0, at);
        propertyName = code.Substring(at + 1);

        return true;
    }
}
