using System.Threading.Tasks;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Clones a wage type for the "Copy ... and create a new wage type" actions on setup screens (legacy
/// <c>ObjectCopyController.GetWageType</c>, shared by Copy Loan Type, Copy Leave Type, Copy Ticket Type, ...).
/// </summary>
public interface IWageTypeCopyService
{
    /// <summary>
    /// Inserts a copy of <paramref name="sourceWageTypeId"/> named <paramref name="name"/>, with the next free
    /// custom code, its leave / exception / security-role rows, and a copy of its arrear wage type if it has one.
    /// Returns the new wage type's id.
    /// </summary>
    Task<int> CopyAsync(int sourceWageTypeId, string name);
}
