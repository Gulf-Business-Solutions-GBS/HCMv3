using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for the Employee master-data hub (ported from legacy XAF Employee business
/// object -- Wave A scope only, see <see cref="Employee"/> for what was intentionally left out).
/// Entity/DTO mapping is manual (not Mapperly) since the list DTO resolves FK display names
/// (NationalityName, TitleName), mirroring <c>CitiesAppService</c>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_Employees)]
public class EmployeesAppService : HCMv3AppServiceBase, IEmployeesAppService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IEmployeeCodeGenerator _employeeCodeGenerator;
    private readonly IEmployeeValidator _employeeValidator;
    private readonly IEntityImageStore _imageStore;

    public EmployeesAppService(
        IRepository<Employee> employeeRepository,
        IEmployeeCodeGenerator employeeCodeGenerator,
        IEmployeeValidator employeeValidator,
        IEntityImageStore imageStore)
    {
        _employeeRepository = employeeRepository;
        _employeeCodeGenerator = employeeCodeGenerator;
        _employeeValidator = employeeValidator;
        _imageStore = imageStore;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeDto>> GetAll(GetAllEmployeesInput input)
    {
        input.Normalize();

        var query = _employeeRepository.GetAll()
            .Include(e => e.Nationality)
            .Include(e => e.Title)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), e =>
                e.Code.Contains(input.Filter) ||
                e.FullNameEn.Contains(input.Filter) ||
                e.FullNameAr.Contains(input.Filter) ||
                e.Email.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeForEditOutput> GetEmployeeForEdit(EntityDto input)
    {
        var employee = await _employeeRepository.GetAsync(input.Id);

        var dto = ToCreateOrEditDto(employee);

        // The photo travels in the DTO as before; only where it is kept changed (disk, not the database).
        dto.Photo = await _imageStore.ReadWithLegacyAsync(employee.TenantId, employee.PhotoPath, employee.LegacyPhoto);

        return new GetEmployeeForEditOutput
        {
            Employee = dto
        };
    }

    public async Task<EmployeeProfileDto> GetEmployeeProfile(EntityDto input)
    {
        // Nine Includes, one round trip. The alternative -- returning ids and letting the client fetch
        // nine lookup lists to translate them -- is nine round trips to draw one record.
        var employee = await _employeeRepository.GetAll()
            .Include(e => e.Title)
            .Include(e => e.Nationality)
            .Include(e => e.Religion)
            .Include(e => e.MaritalStatus)
            .Include(e => e.Qualification)
            .Include(e => e.BirthPlace)
            .Include(e => e.Governorate)
            .Include(e => e.Country)
            .Include(e => e.RecruitmentType)
            .FirstOrDefaultAsync(e => e.Id == input.Id);

        if (employee == null)
        {
            throw new UserFriendlyException(L("EmployeeNotFound"));
        }

        return ToProfileDto(employee);
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeDto input)
    {
        if (input.Id.HasValue)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_Employees_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeDto input)
    {
        var employee = MapToEntity(input, new Employee());
        employee.TenantId = AbpSession.TenantId ?? 0;

        // Legacy Employee.AfterConstruction() always sets Status = Active for new employees --
        // the DTO's Status field only applies on edit (e.g. later marking an employee Inactive).
        employee.Status = EmployeeStatus.Active;

        _employeeCodeGenerator.AssignCodeIfEmpty(employee);
        ApplyComputedFields(employee);
        _employeeValidator.Validate(employee);

        // The file name carries the employee id, so the row is inserted first.
        await _employeeRepository.InsertAndGetIdAsync(employee);
        employee.PhotoPath = await _imageStore.SaveIfChangedAsync(
            EntityImageKinds.EmployeePhoto, employee.TenantId, employee.Id, null, input.Photo);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_Employees_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeDto input)
    {
        var employee = await _employeeRepository.GetAsync(input.Id.Value);
        MapToEntity(input, employee);

        _employeeCodeGenerator.AssignCodeIfEmpty(employee);
        ApplyComputedFields(employee);
        _employeeValidator.Validate(employee);

        employee.PhotoPath = await _imageStore.SaveIfChangedAsync(
            EntityImageKinds.EmployeePhoto, employee.TenantId, employee.Id, employee.PhotoPath, input.Photo);

        // Whatever the screen sent back is now on disk (or was removed), so a not-yet-moved copy in the old
        // column is obsolete either way.
        employee.LegacyPhoto = null;

        await _employeeRepository.UpdateAsync(employee);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_Employees_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: FullNameEn/Ar concatenation and ProbationEndDate are
    /// computed and stored on every save.
    /// </summary>
    private static void ApplyComputedFields(Employee employee)
    {
        employee.ApplyFullNames();

        // Calendar-days probation calculation only (legacy "Working Days" branch depends on
        // DutyPeriod/HolidayCalendar/TimeRegister -- Wave B/TimeManagement, not available yet).
        employee.ProbationEndDate = employee.JoiningDate.AddDays(employee.ProbationDays);
    }

    private static Employee MapToEntity(CreateOrEditEmployeeDto input, Employee employee)
    {
        employee.Code = input.Code;
        employee.FirstNameEn = input.FirstNameEn;
        employee.SecondNameEn = input.SecondNameEn;
        employee.ThirdNameEn = input.ThirdNameEn;
        employee.LastNameEn = input.LastNameEn;
        employee.FirstNameAr = input.FirstNameAr;
        employee.SecondNameAr = input.SecondNameAr;
        employee.ThirdNameAr = input.ThirdNameAr;
        employee.LastNameAr = input.LastNameAr;
        employee.TitleId = input.TitleId;
        employee.Status = input.Status;
        employee.BirthDate = input.BirthDate;
        employee.Gender = input.Gender;
        employee.MaritalStatusId = input.MaritalStatusId;
        employee.NationalityId = input.NationalityId;
        employee.ReligionId = input.ReligionId;
        employee.FlatNumn = input.FlatNumn;
        employee.FloorNum = input.FloorNum;
        employee.BuildingNum = input.BuildingNum;
        employee.StreetNum = input.StreetNum;
        employee.BlockNum = input.BlockNum;
        employee.LaneNumber = input.LaneNumber;
        employee.PACINumber = input.PACINumber;
        employee.ZipCode = input.ZipCode;
        employee.City = input.City;
        employee.GovernorateId = input.GovernorateId;
        employee.CountryId = input.CountryId;
        employee.Telephone = input.Telephone;
        employee.Mobile = input.Mobile;
        employee.CurrentAddress = input.CurrentAddress;
        employee.PermanentAddress = input.PermanentAddress;
        employee.PermanentTelephone = input.PermanentTelephone;
        employee.FatherNameEn = input.FatherNameEn;
        employee.FatherNameAr = input.FatherNameAr;
        employee.MotherNameEn = input.MotherNameEn;
        employee.MotherNameAr = input.MotherNameAr;
        employee.QualificationId = input.QualificationId;
        employee.BloodGroup = input.BloodGroup;
        employee.CivilID = input.CivilID;
        employee.LoginID = input.LoginID;
        employee.AccessID = input.AccessID;
        employee.Email = input.Email;
        employee.AlternateEmail = input.AlternateEmail;
        employee.BirthPlaceId = input.BirthPlaceId;
        employee.PreviousEmpCode = input.PreviousEmpCode;
        employee.ReferenceCode = input.ReferenceCode;
        employee.EmergencyContact = input.EmergencyContact;
        employee.EmergencyNumber = input.EmergencyNumber;
        employee.EmergencyRelation = input.EmergencyRelation;
        employee.EmergencyContactAddress = input.EmergencyContactAddress;
        employee.CriticalIllness = input.CriticalIllness;
        employee.EmploymentType = input.EmploymentType;
        employee.BusinessLandline = input.BusinessLandline;
        employee.BusinessExtension = input.BusinessExtension;
        employee.RecruitmentTypeId = input.RecruitmentTypeId;
        employee.JoiningDate = input.JoiningDate;
        employee.ProbationDays = input.ProbationDays;

        return employee;
    }

    private static EmployeeDto ToDto(Employee employee)
    {
        return new EmployeeDto
        {
            Id = employee.Id,
            Code = employee.Code,
            FirstNameEn = employee.FirstNameEn,
            LastNameEn = employee.LastNameEn,
            FirstNameAr = employee.FirstNameAr,
            LastNameAr = employee.LastNameAr,
            FullNameEn = employee.FullNameEn,
            FullNameAr = employee.FullNameAr,
            Status = employee.Status,
            BirthDate = employee.BirthDate,
            JoiningDate = employee.JoiningDate,
            ProbationDays = employee.ProbationDays,
            ProbationEndDate = employee.ProbationEndDate,
            CivilID = employee.CivilID,
            Email = employee.Email,
            EmploymentType = employee.EmploymentType,
            NationalityId = employee.NationalityId,
            NationalityName = employee.Nationality?.NameEn,
            TitleId = employee.TitleId,
            TitleName = employee.Title?.NameEn
        };
    }

    /// <summary>
    /// Read-shaped projection: lookups become names. Every `?.` is load-bearing -- eight of the nine
    /// lookup FKs are nullable on Employee, so a record with no religion or no birth place recorded is
    /// an ordinary state, not a broken one, and must render as blank rather than throw.
    /// </summary>
    private static EmployeeProfileDto ToProfileDto(Employee employee)
    {
        return new EmployeeProfileDto
        {
            Id = employee.Id,
            Code = employee.Code,

            FullNameEn = employee.FullNameEn,
            FullNameAr = employee.FullNameAr,
            FirstNameEn = employee.FirstNameEn,
            SecondNameEn = employee.SecondNameEn,
            ThirdNameEn = employee.ThirdNameEn,
            LastNameEn = employee.LastNameEn,
            FirstNameAr = employee.FirstNameAr,
            SecondNameAr = employee.SecondNameAr,
            ThirdNameAr = employee.ThirdNameAr,
            LastNameAr = employee.LastNameAr,
            TitleName = employee.Title?.NameEn,

            Status = employee.Status,
            EmploymentType = employee.EmploymentType,
            Gender = employee.Gender,
            BirthDate = employee.BirthDate,
            BirthPlaceName = employee.BirthPlace?.NameEn,
            NationalityName = employee.Nationality?.NameEn,
            ReligionName = employee.Religion?.NameEn,
            MaritalStatusName = employee.MaritalStatus?.NameEn,
            QualificationName = employee.Qualification?.NameEn,
            BloodGroup = employee.BloodGroup,
            CivilID = employee.CivilID,
            PACINumber = employee.PACINumber,
            FatherNameEn = employee.FatherNameEn,
            MotherNameEn = employee.MotherNameEn,
            CriticalIllness = employee.CriticalIllness,

            JoiningDate = employee.JoiningDate,
            ProbationDays = employee.ProbationDays,
            ProbationEndDate = employee.ProbationEndDate,
            RecruitmentTypeName = employee.RecruitmentType?.NameEn,
            PreviousEmpCode = employee.PreviousEmpCode,
            ReferenceCode = employee.ReferenceCode,
            LoginID = employee.LoginID,
            AccessID = employee.AccessID,

            Email = employee.Email,
            AlternateEmail = employee.AlternateEmail,
            Mobile = employee.Mobile,
            Telephone = employee.Telephone,
            BusinessLandline = employee.BusinessLandline,
            BusinessExtension = employee.BusinessExtension,

            CurrentAddress = employee.CurrentAddress,
            PermanentAddress = employee.PermanentAddress,
            PermanentTelephone = employee.PermanentTelephone,
            FlatNumn = employee.FlatNumn,
            FloorNum = employee.FloorNum,
            BuildingNum = employee.BuildingNum,
            StreetNum = employee.StreetNum,
            BlockNum = employee.BlockNum,
            LaneNumber = employee.LaneNumber,
            City = employee.City,
            GovernorateName = employee.Governorate?.NameEn,
            CountryName = employee.Country?.NameEn,
            ZipCode = employee.ZipCode,

            EmergencyContact = employee.EmergencyContact,
            EmergencyNumber = employee.EmergencyNumber,
            EmergencyRelation = employee.EmergencyRelation,
            EmergencyContactAddress = employee.EmergencyContactAddress,
        };
    }

    private static CreateOrEditEmployeeDto ToCreateOrEditDto(Employee employee)
    {
        return new CreateOrEditEmployeeDto
        {
            Id = employee.Id,
            Code = employee.Code,
            FirstNameEn = employee.FirstNameEn,
            SecondNameEn = employee.SecondNameEn,
            ThirdNameEn = employee.ThirdNameEn,
            LastNameEn = employee.LastNameEn,
            FirstNameAr = employee.FirstNameAr,
            SecondNameAr = employee.SecondNameAr,
            ThirdNameAr = employee.ThirdNameAr,
            LastNameAr = employee.LastNameAr,
            TitleId = employee.TitleId,
            Status = employee.Status,
            BirthDate = employee.BirthDate,
            Gender = employee.Gender,
            MaritalStatusId = employee.MaritalStatusId,
            NationalityId = employee.NationalityId,
            ReligionId = employee.ReligionId,
            FlatNumn = employee.FlatNumn,
            FloorNum = employee.FloorNum,
            BuildingNum = employee.BuildingNum,
            StreetNum = employee.StreetNum,
            BlockNum = employee.BlockNum,
            LaneNumber = employee.LaneNumber,
            PACINumber = employee.PACINumber,
            ZipCode = employee.ZipCode,
            City = employee.City,
            GovernorateId = employee.GovernorateId,
            CountryId = employee.CountryId,
            Telephone = employee.Telephone,
            Mobile = employee.Mobile,
            CurrentAddress = employee.CurrentAddress,
            PermanentAddress = employee.PermanentAddress,
            PermanentTelephone = employee.PermanentTelephone,
            FatherNameEn = employee.FatherNameEn,
            FatherNameAr = employee.FatherNameAr,
            MotherNameEn = employee.MotherNameEn,
            MotherNameAr = employee.MotherNameAr,
            QualificationId = employee.QualificationId,
            BloodGroup = employee.BloodGroup,
            CivilID = employee.CivilID,
            LoginID = employee.LoginID,
            AccessID = employee.AccessID,
            Email = employee.Email,
            AlternateEmail = employee.AlternateEmail,
            BirthPlaceId = employee.BirthPlaceId,
            PreviousEmpCode = employee.PreviousEmpCode,
            ReferenceCode = employee.ReferenceCode,
            EmergencyContact = employee.EmergencyContact,
            EmergencyNumber = employee.EmergencyNumber,
            EmergencyRelation = employee.EmergencyRelation,
            EmergencyContactAddress = employee.EmergencyContactAddress,
            CriticalIllness = employee.CriticalIllness,
            EmploymentType = employee.EmploymentType,
            BusinessLandline = employee.BusinessLandline,
            BusinessExtension = employee.BusinessExtension,
            RecruitmentTypeId = employee.RecruitmentTypeId,
            JoiningDate = employee.JoiningDate,
            ProbationDays = employee.ProbationDays
        };
    }
}
