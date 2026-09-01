using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.UI;
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

    public PersonalDataChangeWorkflowTransactionHandler(
        IRepository<EmployeePersonalDataChange> repository,
        IRepository<Employee> employeeRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _employeeRepository = employeeRepository;
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

        var changed = DescribeChangedFields(change, employee);

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
        // recomputed both in OnSaving; here the recomputation lives in EmployeesAppService, which this path
        // does not go through, so the same two lines are applied directly. Kept in step with
        // `EmployeesAppService.ApplyComputedFields`.
        employee.FullNameEn = JoinName(employee.FirstNameEn, employee.SecondNameEn, employee.ThirdNameEn, employee.LastNameEn);
        employee.FullNameAr = JoinName(employee.FirstNameAr, employee.SecondNameAr, employee.ThirdNameAr, employee.LastNameAr);
    }

    private static string JoinName(params string[] parts)
    {
        return string.Join(" ", parts.Where(p => !p.IsNullOrWhiteSpace()));
    }

    private async Task<Employee> GetEmployeeAsync(int employeeId)
    {
        // Employee is deliberately NOT IWorkflowEnabledEntity, so there is no ApprovedOnly filter to disable
        // here -- and that is the point of the design: an in-flight profile edit must never hide the
        // employee from payroll.
        return await _employeeRepository.FirstOrDefaultAsync(e => e.Id == employeeId);
    }

    /// <summary>
    /// Which of the forty fields actually differ from what the master record says today.
    ///
    /// <para>
    /// A field the employee left as it came back from the pre-filled form is not a change and does not
    /// belong in the approver's inbox line. Null and empty compare equal for the same reason: a text box
    /// that was empty and stayed empty has not changed, whichever of the two the client sent.
    /// </para>
    /// </summary>
    private static List<(string LabelEn, string LabelAr)> DescribeChangedFields(
        EmployeePersonalDataChange change,
        Employee employee)
    {
        var changed = new List<(string, string)>();

        void Text(string proposed, string current, string labelEn, string labelAr)
        {
            if (proposed == null)
            {
                return;
            }

            var same = string.IsNullOrEmpty(proposed) && string.IsNullOrEmpty(current)
                       || string.Equals(proposed, current, StringComparison.Ordinal);

            if (!same)
            {
                changed.Add((labelEn, labelAr));
            }
        }

        void Value<T>(T? proposed, T current, string labelEn, string labelAr) where T : struct
        {
            if (proposed.HasValue && !Equals(proposed.Value, current))
            {
                changed.Add((labelEn, labelAr));
            }
        }

        void Lookup(int? proposed, int? current, string labelEn, string labelAr)
        {
            if (proposed.HasValue && proposed != current)
            {
                changed.Add((labelEn, labelAr));
            }
        }

        Text(change.FirstNameEn, employee?.FirstNameEn, "First Name (En)", "الاسم الأول (إنجليزي)");
        Text(change.SecondNameEn, employee?.SecondNameEn, "Second Name (En)", "الاسم الثاني (إنجليزي)");
        Text(change.ThirdNameEn, employee?.ThirdNameEn, "Third Name (En)", "الاسم الثالث (إنجليزي)");
        Text(change.LastNameEn, employee?.LastNameEn, "Last Name (En)", "اسم العائلة (إنجليزي)");
        Text(change.FirstNameAr, employee?.FirstNameAr, "First Name (Ar)", "الاسم الأول (عربي)");
        Text(change.SecondNameAr, employee?.SecondNameAr, "Second Name (Ar)", "الاسم الثاني (عربي)");
        Text(change.ThirdNameAr, employee?.ThirdNameAr, "Third Name (Ar)", "الاسم الثالث (عربي)");
        Text(change.LastNameAr, employee?.LastNameAr, "Last Name (Ar)", "اسم العائلة (عربي)");

        Lookup(change.TitleId, employee?.TitleId, "Title", "اللقب");
        Value(change.BirthDate?.Date, employee?.BirthDate.Date ?? default, "Birth Date", "تاريخ الميلاد");
        Lookup(change.NationalityId, employee?.NationalityId, "Nationality", "الجنسية");
        Lookup(change.ReligionId, employee?.ReligionId, "Religion", "الديانة");
        Value(change.Gender, employee?.Gender ?? default, "Gender", "الجنس");
        Lookup(change.MaritalStatusId, employee?.MaritalStatusId, "Marital Status", "الحالة الاجتماعية");

        Text(change.FlatNumn, employee?.FlatNumn, "Flat No.", "رقم الشقة");
        Text(change.FloorNum, employee?.FloorNum, "Floor No.", "رقم الطابق");
        Text(change.BuildingNum, employee?.BuildingNum, "Building No.", "رقم المبنى");
        Text(change.StreetNum, employee?.StreetNum, "Street No.", "رقم الشارع");
        Text(change.BlockNum, employee?.BlockNum, "Block No.", "رقم القطعة");
        Text(change.ZipCode, employee?.ZipCode, "Zip Code", "الرمز البريدي");
        Text(change.City, employee?.City, "City", "المدينة");
        Lookup(change.GovernorateId, employee?.GovernorateId, "Governorate", "المحافظة");
        Lookup(change.CountryId, employee?.CountryId, "Country", "الدولة");
        Value(change.Telephone, employee?.Telephone ?? default, "Telephone", "الهاتف");
        Value(change.Mobile, employee?.Mobile ?? default, "Mobile", "الهاتف النقال");
        Text(change.CurrentAddress, employee?.CurrentAddress, "Current Address", "العنوان الحالي");
        Text(change.PermanentAddress, employee?.PermanentAddress, "Permanent Address", "العنوان الدائم");

        Text(change.FatherNameEn, employee?.FatherNameEn, "Father's Name (En)", "اسم الأب (إنجليزي)");
        Text(change.FatherNameAr, employee?.FatherNameAr, "Father's Name (Ar)", "اسم الأب (عربي)");
        Text(change.MotherNameEn, employee?.MotherNameEn, "Mother's Name (En)", "اسم الأم (إنجليزي)");
        Text(change.MotherNameAr, employee?.MotherNameAr, "Mother's Name (Ar)", "اسم الأم (عربي)");

        Text(change.BloodGroup, employee?.BloodGroup, "Blood Group", "فصيلة الدم");
        Text(change.Email, employee?.Email, "Email", "البريد الإلكتروني");
        Text(change.PACINumber, employee?.PACINumber, "PACI Number", "الرقم الآلي");
        Text(change.EmergencyContact, employee?.EmergencyContact, "Emergency Contact", "جهة الاتصال للطوارئ");
        Text(change.CriticalIllness, employee?.CriticalIllness, "Critical Illness", "الأمراض المزمنة");
        Text(change.AlternateEmail, employee?.AlternateEmail, "Alternate Email", "بريد إلكتروني بديل");
        Text(change.EmergencyNumber, employee?.EmergencyNumber, "Emergency Number", "رقم الطوارئ");
        Text(change.EmergencyRelation, employee?.EmergencyRelation, "Emergency Relation", "صلة القرابة");
        Lookup(change.BirthPlaceId, employee?.BirthPlaceId, "Birth Place", "مكان الميلاد");

        return changed;
    }
}
