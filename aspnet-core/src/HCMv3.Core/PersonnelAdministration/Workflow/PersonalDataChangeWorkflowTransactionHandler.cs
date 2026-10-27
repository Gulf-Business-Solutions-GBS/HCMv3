using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for an employee's own personnel-file update -- <see cref="EmployeePersonalDataChange"/>
/// (ported from legacy <c>PersonalDataUpdateRequest</c>).
///
/// <para>
/// <b>The most consequential handler in the wave.</b> Approving one of these writes to the EMPLOYEE MASTER
/// RECORD: the row payroll, government reporting, insurance and every letter template read from. The copy in
/// <see cref="OnApprovedAsync"/> is the entire feature, and every safeguard here exists because of what a
/// mistake in it would look like -- not an error message, but somebody's personnel file quietly holding
/// somebody else's data.
/// </para>
///
/// <para>
/// <b>The rules are the ones legacy actually enforced, which were not the ones on the entity.</b> Legacy
/// <c>PersonalDataUpdateRequest</c> carried a rule requiring at least one of Mobile/Photo/Email; it is dead
/// code and is NOT ported. <c>Mobile</c> is a non-nullable <c>long</c> there, so its null check can never
/// fire, and <c>Photo</c>'s getter returns a default image rather than null, so neither can that one. The
/// rules that ran are in <c>WorkFlowServices.SubmitRequest</c>, and those five are what is ported below.
/// </para>
///
/// <para>
/// <b>No Photo.</b> Legacy kept a Photo column on the request whose copy line was commented out "for
/// performance issue" -- an employee could upload a picture that never reached their profile, and nothing
/// told them. The column is not ported and the photo is not on the form; it remains HR-managed. See
/// <see cref="EmployeePersonalDataChange"/>.
/// </para>
/// </summary>
public class PersonalDataChangeWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeePersonalDataChange>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for personal data updates.</summary>
    public const string PersonalDataChangeRequestType = "EmployeePersonalDataChange";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<Title> _titleRepository;
    private readonly IRepository<Nationality> _nationalityRepository;
    private readonly IRepository<Religion> _religionRepository;
    private readonly IRepository<MaritalStatus> _maritalStatusRepository;
    private readonly IRepository<City> _cityRepository;
    private readonly IRepository<Governorate> _governorateRepository;
    private readonly IRepository<Country> _countryRepository;

    public PersonalDataChangeWorkflowTransactionHandler(
        IRepository<EmployeePersonalDataChange> repository,
        IRepository<Employee> employeeRepository,
        IRepository<Title> titleRepository,
        IRepository<Nationality> nationalityRepository,
        IRepository<Religion> religionRepository,
        IRepository<MaritalStatus> maritalStatusRepository,
        IRepository<City> cityRepository,
        IRepository<Governorate> governorateRepository,
        IRepository<Country> countryRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _employeeRepository = employeeRepository;
        _titleRepository = titleRepository;
        _nationalityRepository = nationalityRepository;
        _religionRepository = religionRepository;
        _maritalStatusRepository = maritalStatusRepository;
        _cityRepository = cityRepository;
        _governorateRepository = governorateRepository;
        _countryRepository = countryRepository;
    }

    public override string RequestType => PersonalDataChangeRequestType;

    /// <summary>
    /// The five rules legacy enforced in <c>WorkFlowServices</c> (lines 3052-3097), and nothing else.
    ///
    /// <para>
    /// The date rules are checked only when the employee actually proposed a birth date. Legacy could not
    /// make that distinction -- its <c>BirthDate</c> was a non-nullable <c>DateTime</c>, so an untouched
    /// field arrived as <c>01/01/0001</c> and the rules ran against it regardless. Here a null means the
    /// birth date is not part of the request, and refusing a phone-number change because of a birth date
    /// nobody proposed would block a legitimate edit over data the employee cannot fix from this screen.
    /// </para>
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var change = await GetTargetAsync(context);
        if (change == null)
        {
            return;
        }

        if (change.FirstNameEn.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("FirstNameEnIsRequired"));
        }

        if (change.FirstNameAr.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("FirstNameArIsRequired"));
        }

        if (change.LastNameEn.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("LastNameEnIsRequired"));
        }

        if (change.LastNameAr.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("LastNameArIsRequired"));
        }

        if (!change.BirthDate.HasValue)
        {
            return;
        }

        // Strictly in the past: today is refused as well as the future, matching legacy's `>=`.
        if (change.BirthDate.Value.Date >= DateTime.Today)
        {
            throw new UserFriendlyException(L("BirthDateCannotBeFutureOrCurrent"));
        }

        var employee = await GetEmployeeAsync(change.EmployeeId);
        if (employee == null)
        {
            throw new UserFriendlyException(L("EmployeeNotFound"));
        }

        // Legacy: `if (emp.JoiningDate.Date <= PersonalDataUpdateObj.BirthDate)`. Equal dates are refused
        // too -- nobody joined the company on the day they were born.
        if (employee.JoiningDate.Date <= change.BirthDate.Value.Date)
        {
            throw new UserFriendlyException(L("JoiningDateCannotBeEarlierThanBirthDate"));
        }
    }

    /// <summary>
    /// Names WHICH fields are changing, e.g. "Profile update: Mobile, Email, Current Address".
    ///
    /// <para>
    /// Deliberately not the generic "profile update" every other implementation of this reaches for. The
    /// approver is agreeing to a change to somebody's IDENTITY, and "profile update" gives them nothing to
    /// decide on -- a mobile number and a change of surname would read identically in their inbox. The form
    /// carries the employee's current values pre-filled, so most of what arrives is unchanged; only the
    /// fields that genuinely differ from the master record are listed.
    /// </para>
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var change = await GetTargetAsync(context);
        if (change == null)
        {
            return null;
        }

        var employee = await GetEmployeeAsync(change.EmployeeId);

        var changed = PersonalDataChangeFields.Changes(change, PreviousValues(change, employee))
            .Select(c => c.Field)
            .ToList();

        if (changed.Count == 0)
        {
            return new WorkflowSummary("Personal data update", "تحديث البيانات الشخصية");
        }

        // Capped so the inbox line stays a line. The full before/after is on the request screen.
        const int maxListed = 6;

        var listedEn = string.Join(", ", changed.Take(maxListed).Select(f => f.LabelEn));
        var listedAr = string.Join("، ", changed.Take(maxListed).Select(f => f.LabelAr));

        if (changed.Count > maxListed)
        {
            var more = changed.Count - maxListed;
            listedEn += $" (+{more} more)";
            listedAr += $" (+{more} أخرى)";
        }

        return new WorkflowSummary(
            $"Profile update: {listedEn}",
            $"تحديث البيانات: {listedAr}");
    }

    /// <summary>
    /// What an approver decides on: each field the request changes, with what it held before beside what is
    /// proposed. Only the changed fields -- the other thirty-odd are what the file already says, and listing
    /// them would bury the two or three the approver is being asked about.
    /// </summary>
    public override async Task<WorkflowRequestDetail> BuildDetailAsync(WorkflowTransactionContext context)
    {
        var change = await GetTargetAsync(context);
        if (change == null)
        {
            return await base.BuildDetailAsync(context);
        }

        var employee = await GetEmployeeAsync(change.EmployeeId);

        var detail = new WorkflowRequestDetail
        {
            SubmissionComment = change.Comments
        };

        foreach (var changed in PersonalDataChangeFields.Changes(change, PreviousValues(change, employee)))
        {
            detail.Add(WorkflowDetailField.Change(
                changed.Field.Name,
                L(changed.Field.LabelKey),
                await DisplayAsync(changed.Field, changed.Previous),
                await DisplayAsync(changed.Field, changed.Proposed),
                changed.Field.Type == PersonalDataFieldType.Date ? WorkflowDetailFieldKind.Date : WorkflowDetailFieldKind.Text));
        }

        return detail;
    }

    /// <summary>
    /// Keeps what the personnel file says at the moment the request is sent, so the request can go on
    /// showing old beside new after approval has replaced the old. Refreshed on every submission: a request
    /// sent back and corrected is compared against the file as it stands when it is sent again.
    /// </summary>
    public override async Task OnSubmittedAsync(WorkflowTransactionContext context)
    {
        if (!context.IsAmendment)
        {
            var change = await GetTargetAsync(context);
            if (change != null)
            {
                change.PreviousValuesJson = PersonalDataChangeFields.Serialize(await GetEmployeeAsync(change.EmployeeId));
            }
        }

        await base.OnSubmittedAsync(context);
    }

    /// <summary>
    /// Writes the approved values onto the employee master record. This is the whole feature.
    ///
    /// <para>
    /// <b>A null proposed value means "leave it alone", never "blank it out".</b> A partial update is the
    /// normal case -- somebody changing only their mobile number sends only their mobile number -- so
    /// assigning unconditionally would wipe every field they did not fill in. That is the single most
    /// damaging thing this feature could do, and it is why every one of the forty assignments below is
    /// guarded.
    /// </para>
    /// <para>
    /// Runs inside the approving unit of work, so if anything downstream throws the master record rolls back
    /// with the approval. A request marked approved whose values never landed, or values that landed under a
    /// request that was not approved, would both be invisible from the screens.
    /// </para>
    /// </summary>
    public override async Task OnApprovedAsync(WorkflowTransactionContext context)
    {
        await base.OnApprovedAsync(context);

        // Amendments are not supported for this type (SupportsAmendment is false, inherited), so an
        // amendment context can never reach the copy. Guarded anyway: the base dispatches amendments from a
        // method this one overrides, which makes the omission invisible from here.
        if (context.IsAmendment)
        {
            return;
        }

        var change = await GetTargetAsync(context);
        if (change == null)
        {
            return;
        }

        var employee = await GetEmployeeAsync(change.EmployeeId);
        if (employee == null)
        {
            throw new UserFriendlyException(L("EmployeeNotFound"));
        }

        Apply(change, employee);

        await _employeeRepository.UpdateAsync(employee);
    }

    /// <summary>
    /// The copy, field by field, in the same order as the legacy block it replaces
    /// (<c>PersonalDataUpdateRequest.OnSaving</c>, lines 437-509). Forty fields; every one guarded by
    /// "did the employee actually supply this".
    ///
    /// <para>
    /// Kept as forty explicit lines rather than a reflective property loop on purpose. Reflection would copy
    /// whatever happened to share a name, which is exactly how a field nobody meant to expose -- a
    /// <c>Status</c>, a <c>JoiningDate</c>, a <c>Code</c> -- ends up self-service editable. The legacy block
    /// had thirteen commented-out lines for precisely those fields, and that list of what must NOT be copied
    /// is only preserved by writing out what is.
    /// </para>
    /// </summary>
    public static void Apply(EmployeePersonalDataChange change, Employee employee)
    {
        if (change.FirstNameEn != null) employee.FirstNameEn = change.FirstNameEn;
        if (change.SecondNameEn != null) employee.SecondNameEn = change.SecondNameEn;
        if (change.ThirdNameEn != null) employee.ThirdNameEn = change.ThirdNameEn;
        if (change.LastNameEn != null) employee.LastNameEn = change.LastNameEn;
        if (change.FirstNameAr != null) employee.FirstNameAr = change.FirstNameAr;
        if (change.SecondNameAr != null) employee.SecondNameAr = change.SecondNameAr;
        if (change.ThirdNameAr != null) employee.ThirdNameAr = change.ThirdNameAr;
        if (change.LastNameAr != null) employee.LastNameAr = change.LastNameAr;

        if (change.TitleId.HasValue) employee.TitleId = change.TitleId;
        if (change.BirthDate.HasValue) employee.BirthDate = change.BirthDate.Value;
        if (change.NationalityId.HasValue) employee.NationalityId = change.NationalityId.Value;
        if (change.ReligionId.HasValue) employee.ReligionId = change.ReligionId;
        if (change.Gender.HasValue) employee.Gender = change.Gender.Value;
        if (change.MaritalStatusId.HasValue) employee.MaritalStatusId = change.MaritalStatusId;

        if (change.FlatNumn != null) employee.FlatNumn = change.FlatNumn;
        if (change.FloorNum != null) employee.FloorNum = change.FloorNum;
        if (change.BuildingNum != null) employee.BuildingNum = change.BuildingNum;
        if (change.StreetNum != null) employee.StreetNum = change.StreetNum;
        if (change.BlockNum != null) employee.BlockNum = change.BlockNum;
        if (change.ZipCode != null) employee.ZipCode = change.ZipCode;
        if (change.City != null) employee.City = change.City;

        // Legacy spelled this `Governate`; HCMv3 spells it `Governorate`. Same field, renamed -- the only
        // name that differs between the two sides of this copy.
        if (change.GovernorateId.HasValue) employee.GovernorateId = change.GovernorateId;

        if (change.CountryId.HasValue) employee.CountryId = change.CountryId;
        if (change.Telephone.HasValue) employee.Telephone = change.Telephone.Value;
        if (change.Mobile.HasValue) employee.Mobile = change.Mobile.Value;
        if (change.CurrentAddress != null) employee.CurrentAddress = change.CurrentAddress;
        if (change.PermanentAddress != null) employee.PermanentAddress = change.PermanentAddress;

        if (change.FatherNameEn != null) employee.FatherNameEn = change.FatherNameEn;
        if (change.FatherNameAr != null) employee.FatherNameAr = change.FatherNameAr;
        if (change.MotherNameEn != null) employee.MotherNameEn = change.MotherNameEn;
        if (change.MotherNameAr != null) employee.MotherNameAr = change.MotherNameAr;

        if (change.BloodGroup != null) employee.BloodGroup = change.BloodGroup;
        if (change.Email != null) employee.Email = change.Email;
        if (change.PACINumber != null) employee.PACINumber = change.PACINumber;
        if (change.EmergencyContact != null) employee.EmergencyContact = change.EmergencyContact;
        if (change.CriticalIllness != null) employee.CriticalIllness = change.CriticalIllness;
        if (change.AlternateEmail != null) employee.AlternateEmail = change.AlternateEmail;
        if (change.EmergencyNumber != null) employee.EmergencyNumber = change.EmergencyNumber;
        if (change.EmergencyRelation != null) employee.EmergencyRelation = change.EmergencyRelation;
        if (change.BirthPlaceId.HasValue) employee.BirthPlaceId = change.BirthPlaceId;

        // NOT copied, matching the commented-out lines in the legacy block: Status, Code, JoiningDate,
        // ProbationDays, ProbationEndDate, Qualification, CivilID, LoginID, AccessID, PreviousEmpCode,
        // ReferenceCode, EmploymentType, BusinessLandline, BusinessExtension, RecruitmentType -- and Photo,
        // whose column is not ported at all. None of those are the employee's to change about themselves.

        // FullNameEn/Ar are stored, not computed at read time, so a name change that did not refresh them
        // would leave the old name on every screen, letter and report that reads the stored value -- the
        // change would look accepted and show nothing. Legacy got this for free because its Employee
        // recomputed both in OnSaving; here that is Employee.ApplyFullNames, which EmployeesAppService calls on
        // save -- this path does not go through that service, so it calls it directly.
        employee.ApplyFullNames();
    }

    private async Task<Employee> GetEmployeeAsync(int employeeId)
    {
        // Employee is deliberately NOT IWorkflowEnabledEntity, so there is no ApprovedOnly filter to disable
        // here -- and that is the point of the design: an in-flight profile edit must never hide the
        // employee from payroll.
        return await _employeeRepository.FirstOrDefaultAsync(e => e.Id == employeeId);
    }

    /// <summary>
    /// What the request is compared against to say what it changes.
    ///
    /// <para>
    /// While the proposal is still the employee's own -- a draft, or one sent back to them -- that is the
    /// personnel file as it stands, because they can still edit it and the comparison has to follow. Once
    /// it has been submitted it is the values kept at that moment, which is what keeps the comparison
    /// meaningful after approval has overwritten the master record with the proposed values.
    /// </para>
    /// </summary>
    private static IReadOnlyDictionary<string, string> PreviousValues(EmployeePersonalDataChange change, Employee employee)
    {
        if (change.WorkflowStatus != RequestStatus.Draft)
        {
            var kept = PersonalDataChangeFields.Deserialize(change.PreviousValuesJson);
            if (kept != null)
            {
                return kept;
            }
        }

        return PersonalDataChangeFields.Read(employee);
    }

    /// <summary>
    /// A stored value in the reader's words: a lookup's name in their language, the gender as a word, a
    /// date left as ISO for the client to format. Null when there is nothing to show.
    /// </summary>
    private async Task<string> DisplayAsync(PersonalDataField field, string stored)
    {
        if (string.IsNullOrEmpty(stored))
        {
            return null;
        }

        switch (field.Type)
        {
            case PersonalDataFieldType.Title: return await LookupNameAsync(_titleRepository, stored);
            case PersonalDataFieldType.Nationality: return await LookupNameAsync(_nationalityRepository, stored);
            case PersonalDataFieldType.Religion: return await LookupNameAsync(_religionRepository, stored);
            case PersonalDataFieldType.MaritalStatus: return await LookupNameAsync(_maritalStatusRepository, stored);
            case PersonalDataFieldType.City: return await LookupNameAsync(_cityRepository, stored);
            case PersonalDataFieldType.Governorate: return await LookupNameAsync(_governorateRepository, stored);
            case PersonalDataFieldType.Country: return await LookupNameAsync(_countryRepository, stored);
            case PersonalDataFieldType.Gender: return L(stored);

            // The personnel file stores "no number" as zero; shown as empty rather than as a number 0.
            case PersonalDataFieldType.Number: return stored == "0" ? null : stored;
            default: return stored;
        }
    }

    private static async Task<string> LookupNameAsync<TLookup>(IRepository<TLookup> repository, string storedId)
        where TLookup : class, IEntity<int>, IBilingualLookup
    {
        if (!int.TryParse(storedId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return null;
        }

        var row = await repository.FirstOrDefaultAsync(id);

        // A row deleted since still has to read as something: the id beats an empty cell that would make
        // the change look like a field being cleared.
        return row == null ? storedId : WorkflowDetailDisplay.Name(row.NameEn, row.NameAr);
    }
}

