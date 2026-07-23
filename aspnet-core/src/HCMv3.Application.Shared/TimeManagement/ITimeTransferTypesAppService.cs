using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement;

/// <summary>
/// Same shared contract/DTOs as the other pure lookups (see <see cref="ISimpleLookupAppService"/>), but the
/// implementation (<see cref="TimeTransferTypesAppService"/>) is bespoke rather than deriving
/// <c>SimpleLookupAppServiceBase&lt;TEntity&gt;</c> because <c>Delete</c> must always throw (legacy
/// <c>OnDeleting</c> parity).
/// </summary>
public interface ITimeTransferTypesAppService : ISimpleLookupAppService
{
}
