namespace HCMv3.PayrollManagement;

/// <summary>
/// Determines how an employee's air ticket entitlement is priced (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.AirTicketPayType</c>, declared inside
/// <c>EmployeeSubGroup.cs</c>).
/// </summary>
public enum AirTicketPayType
{
    /// <summary>Uploaded in the Ticket Fare screen.</summary>
    FareAmount = 0,

    /// <summary>Picked from the Salary Profile's EmployeeSubGroupTicket.FixAmount field.</summary>
    FixAmount = 1,

    /// <summary>User enters the Amount field on the Employee Ticket record.</summary>
    ActualAmount = 2,

    /// <summary>User enters the Amount field, capped by EmployeeSubGroupTicket.MaxAmount.</summary>
    ActualAmountWithinMaxlimit = 3
}
