using Abp.Mapperly;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

/// <summary>
/// Mappers for the self-service personnel-file update.
///
/// <para>
/// Generated rather than hand-written on purpose, and it is the one place in this feature where that is the
/// safer choice. Forty fields have to survive three hops -- form to proposal, proposal back to the screen,
/// and the employee's current values into the pre-filled form -- and a hand-written copy of each is a
/// hundred and twenty chances to put <c>MotherNameAr</c> where <c>MotherNameEn</c> belongs. Mapperly matches
/// by name at compile time. The one hop that is NOT generated is the copy onto the employee master record
/// (<c>PersonalDataChangeWorkflowTransactionHandler.Apply</c>), which stays forty explicit guarded lines
/// because there the interesting information is which fields are deliberately excluded.
/// </para>
/// </summary>
[Mapper]
public partial class RequestPersonalDataChangeInputToEmployeePersonalDataChangeMapper
    : MapperBase<RequestPersonalDataChangeInput, EmployeePersonalDataChange>
{
    public override partial EmployeePersonalDataChange Map(RequestPersonalDataChangeInput source);
    public override partial void Map(RequestPersonalDataChangeInput source, EmployeePersonalDataChange destination);
}

[Mapper]
public partial class EmployeePersonalDataChangeToPersonalDataChangeValuesDtoMapper
    : MapperBase<EmployeePersonalDataChange, PersonalDataChangeValuesDto>
{
    public override partial PersonalDataChangeValuesDto Map(EmployeePersonalDataChange source);
    public override partial void Map(EmployeePersonalDataChange source, PersonalDataChangeValuesDto destination);
}

/// <summary>
/// The employee's current values into the pre-filled form. Only the forty editable fields cross; nothing
/// that is not the employee's to change about themselves is on the destination type at all.
/// </summary>
[Mapper]
public partial class EmployeeToPersonalDataChangeValuesDtoMapper
    : MapperBase<Employee, PersonalDataChangeValuesDto>
{
    public override partial PersonalDataChangeValuesDto Map(Employee source);
    public override partial void Map(Employee source, PersonalDataChangeValuesDto destination);
}
