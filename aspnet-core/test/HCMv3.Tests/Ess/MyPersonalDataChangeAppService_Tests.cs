using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Self-service personnel-file update (legacy <c>PersonalDataUpdateRequest</c>) -- the highest-consequence
/// type in the wave, because approving one writes to the EMPLOYEE MASTER RECORD.
///
/// <para>
/// <b>The field-by-field copy test is the point of this file.</b> Every other risk here produces an error
/// somebody notices; getting one of the forty copied fields wrong produces no error at all, just a personnel
/// file quietly holding the wrong data -- a father's name in the mother's field, a permanent address where
/// the current one should be. <see cref="Approving_Should_Copy_Every_One_Of_The_Forty_Fields_Onto_The_Employee"/>
/// drives one request carrying a distinct value for all forty fields end to end through the real approval
/// engine and then checks every landing site, and
/// <see cref="The_Copy_Should_Cover_Every_Field_On_The_Proposal"/> fails if a field is ever added to the
/// proposal without being added to the copy.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyPersonalDataChangeAppService_Tests : AppTestBase
{
    private readonly IMyPersonalDataChangeAppService _changes;

    public MyPersonalDataChangeAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _changes = Resolve<IMyPersonalDataChangeAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    #region Fixtures

    private static string Unique() => Guid.NewGuid().ToString("N").Substring(0, 8);

    /// <summary>
    /// Any Code/NameEn/NameAr reference row. Constrained on <c>IBilingualLookup</c> rather than on
    /// <c>SimpleLookupEntity</c> because Nationality and City carry extra columns and so do not derive from
    /// it -- both of which this feature needs.
    /// </summary>
    private int CreateLookup<TLookup>(string name)
        where TLookup : class, IEntity<int>, IMustHaveTenant, IBilingualLookup, new()
    {
        TLookup entity = null;
        UsingDbContext(context =>
        {
            entity = new TLookup
            {
                TenantId = TenantId,
                Code = Unique(),
                NameEn = name,
                NameAr = name
            };

            context.Set<TLookup>().Add(entity);
        });

        return entity.Id;
    }

    private int CreateCity(string name) => CreateLookup<City>(name);

    /// <summary>
    /// An employee whose forty editable fields all hold a recognisable "before" value, so that a copy that
    /// lands in the wrong field is visible as the wrong string rather than as a coincidental match.
    /// </summary>
    private int CreateFullyPopulatedEmployee(string code, long? userId)
    {
        var nationalityId = CreateLookup<Nationality>("Before nationality");
        var religionId = CreateLookup<Religion>("Before religion");
        var titleId = CreateLookup<Title>("Before title");
        var maritalStatusId = CreateLookup<MaritalStatus>("Before marital");
        var governorateId = CreateLookup<Governorate>("Before governorate");
        var countryId = CreateLookup<Country>("Before country");
        var birthPlaceId = CreateCity("Before birthplace");

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                UserId = userId,

                FirstNameEn = "BeforeFirstEn",
                SecondNameEn = "BeforeSecondEn",
                ThirdNameEn = "BeforeThirdEn",
                LastNameEn = "BeforeLastEn",
                FirstNameAr = "BeforeFirstAr",
                SecondNameAr = "BeforeSecondAr",
                ThirdNameAr = "BeforeThirdAr",
                LastNameAr = "BeforeLastAr",
                FullNameEn = "BeforeFirstEn BeforeSecondEn BeforeThirdEn BeforeLastEn",
                FullNameAr = "BeforeFirstAr BeforeSecondAr BeforeThirdAr BeforeLastAr",

                TitleId = titleId,
                BirthDate = new DateTime(1980, 3, 2),
                NationalityId = nationalityId,
                ReligionId = religionId,
                Gender = Gender.Male,
                MaritalStatusId = maritalStatusId,
                BirthPlaceId = birthPlaceId,

                FlatNumn = "BeforeFlat",
                FloorNum = "BeforeFloor",
                BuildingNum = "BeforeBuilding",
                StreetNum = "BeforeStreet",
                BlockNum = "BeforeBlock",
                ZipCode = "BeforeZip",
                PACINumber = "BeforePACI",
                City = "BeforeCity",
                GovernorateId = governorateId,
                CountryId = countryId,
                CurrentAddress = "Before current address",
                PermanentAddress = "Before permanent address",
                Telephone = 22220000,
                Mobile = 55550000,

                Email = "before@example.com",
                AlternateEmail = "before.alt@example.com",

                FatherNameEn = "BeforeFatherEn",
                FatherNameAr = "BeforeFatherAr",
                MotherNameEn = "BeforeMotherEn",
                MotherNameAr = "BeforeMotherAr",

                BloodGroup = "O-",
                CriticalIllness = "Before illness",
                EmergencyContact = "Before contact",
                EmergencyNumber = "Before 999",
                EmergencyRelation = "Before relation",

                JoiningDate = new DateTime(2015, 1, 1),
                ProbationEndDate = new DateTime(2015, 4, 1)
            }).Entity;
        });

        return employee.Id;
    }

    /// <summary>A bare employee, for the roles (approver, other employee) whose own fields do not matter.</summary>
    private int CreateBareEmployee(string code)
    {
        var nationalityId = CreateLookup<Nationality>("N");

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = code,
                LastNameEn = "T",
                FirstNameAr = "ت",
                LastNameAr = "ت",
                FullNameEn = code + " T",
                FullNameAr = "ت ت",
                NationalityId = nationalityId,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1)
            }).Entity;
        });

        return employee.Id;
    }

    /// <summary>Turns workflow on for this type, pointing it at a one-step chain with a real approver.</summary>
    private int EnableWorkflow(string approverCode)
    {
        var approverId = CreateBareEmployee(approverCode);

        UsingDbContext(context =>
        {
            var role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = Unique().Substring(0, 6),
                NameEn = "Approver",
                NameAr = "Approver"
            }).Entity;

            var master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = TenantId,
                Code = "PDU-STD",
                NameEn = "Standard",
                NameAr = "Standard",
                WorkflowSteps = 1
            }).Entity;

            context.SaveChanges();

            context.Set<RequestDetail>().Add(new RequestDetail
            {
                TenantId = TenantId,
                RequestMasterId = master.Id,
                Step = 1,
                WorkflowRoleId = role.Id,
                ApproverPerson = WorkflowApproverPersonType.Person,
                ApprovalRequire = ApprovalRequire.Single,
                CanReject = true,
                CanSendBack = true
            });

            context.Set<EmployeeWorkflowRole>().Add(new EmployeeWorkflowRole
            {
                TenantId = TenantId,
                EmployeeId = approverId,
                WorkflowRoleId = role.Id,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2)
            });

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeePersonalDataChange");
            type.IsWorkflowEnabled = true;
            type.IsEssEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    private Employee ReloadEmployee(int employeeId)
    {
        return UsingDbContext(context => context.Set<Employee>().IgnoreQueryFilters().Single(e => e.Id == employeeId));
    }

    private static GetMyRequestsInput ListInput() =>
        new GetMyRequestsInput { IncludeHistory = true, MaxResultCount = 50, SkipCount = 0 };

    /// <summary>
    /// The minimum a request must carry to pass validation: the four names. Used by the tests whose subject
    /// is something other than the names.
    /// </summary>
    private static RequestPersonalDataChangeInput MinimalInput(Employee employee)
    {
        return new RequestPersonalDataChangeInput
        {
            FirstNameEn = employee.FirstNameEn,
            LastNameEn = employee.LastNameEn,
            FirstNameAr = employee.FirstNameAr,
            LastNameAr = employee.LastNameAr
        };
    }

    #endregion

    #region The forty-field copy -- the point of this file

    /// <summary>
    /// The proposed "after" values, and where each one must land on the employee.
    ///
    /// <para>
    /// Written as a table rather than as forty inline assertions so that a single run reports EVERY field
    /// that went astray, not just the first. A copy bug is usually a transposed pair -- father/mother,
    /// current/permanent, En/Ar -- and seeing both halves in one failure is what identifies it.
    /// </para>
    /// </summary>
    private sealed class CopyExpectation
    {
        public string ProposalField { get; init; }

        public Func<Employee, object> ReadFromEmployee { get; init; }

        public object Expected { get; init; }
    }

    [Fact]
    public async Task Approving_Should_Copy_Every_One_Of_The_Forty_Fields_Onto_The_Employee()
    {
        var meId = CreateFullyPopulatedEmployee("PDU100", AbpSession.UserId);
        var approverId = EnableWorkflow("PDU100APP");

        // Every lookup gets a SECOND row, so "copied correctly" cannot be satisfied by the value that was
        // already there.
        var newTitleId = CreateLookup<Title>("After title");
        var newNationalityId = CreateLookup<Nationality>("After nationality");
        var newReligionId = CreateLookup<Religion>("After religion");
        var newMaritalStatusId = CreateLookup<MaritalStatus>("After marital");
        var newGovernorateId = CreateLookup<Governorate>("After governorate");
        var newCountryId = CreateLookup<Country>("After country");
        var newBirthPlaceId = CreateCity("After birthplace");

        var newBirthDate = new DateTime(1985, 6, 15);

        var input = new RequestPersonalDataChangeInput
        {
            FirstNameEn = "AfterFirstEn",
            SecondNameEn = "AfterSecondEn",
            ThirdNameEn = "AfterThirdEn",
            LastNameEn = "AfterLastEn",
            FirstNameAr = "AfterFirstAr",
            SecondNameAr = "AfterSecondAr",
            ThirdNameAr = "AfterThirdAr",
            LastNameAr = "AfterLastAr",

            TitleId = newTitleId,
            BirthDate = newBirthDate,
            NationalityId = newNationalityId,
            ReligionId = newReligionId,
            Gender = Gender.Female,
            MaritalStatusId = newMaritalStatusId,
            BirthPlaceId = newBirthPlaceId,

            FlatNumn = "AfterFlat",
            FloorNum = "AfterFloor",
            BuildingNum = "AfterBuilding",
            StreetNum = "AfterStreet",
            BlockNum = "AfterBlock",
            ZipCode = "AfterZip",
            PACINumber = "AfterPACI",
            City = "AfterCity",
            GovernorateId = newGovernorateId,
            CountryId = newCountryId,
            CurrentAddress = "After current address",
            PermanentAddress = "After permanent address",
            Telephone = 22221111,
            Mobile = 55551111,

            Email = "after@example.com",
            AlternateEmail = "after.alt@example.com",

            FatherNameEn = "AfterFatherEn",
            FatherNameAr = "AfterFatherAr",
            MotherNameEn = "AfterMotherEn",
            MotherNameAr = "AfterMotherAr",

            BloodGroup = "AB+",
            CriticalIllness = "After illness",
            EmergencyContact = "After contact",
            EmergencyNumber = "After 112",
            EmergencyRelation = "After relation",

            Comments = "Moved house and changed my number."
        };

        var expectations = new List<CopyExpectation>
        {
            new() { ProposalField = nameof(EmployeePersonalDataChange.FirstNameEn), ReadFromEmployee = e => e.FirstNameEn, Expected = "AfterFirstEn" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.SecondNameEn), ReadFromEmployee = e => e.SecondNameEn, Expected = "AfterSecondEn" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.ThirdNameEn), ReadFromEmployee = e => e.ThirdNameEn, Expected = "AfterThirdEn" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.LastNameEn), ReadFromEmployee = e => e.LastNameEn, Expected = "AfterLastEn" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.FirstNameAr), ReadFromEmployee = e => e.FirstNameAr, Expected = "AfterFirstAr" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.SecondNameAr), ReadFromEmployee = e => e.SecondNameAr, Expected = "AfterSecondAr" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.ThirdNameAr), ReadFromEmployee = e => e.ThirdNameAr, Expected = "AfterThirdAr" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.LastNameAr), ReadFromEmployee = e => e.LastNameAr, Expected = "AfterLastAr" },

            new() { ProposalField = nameof(EmployeePersonalDataChange.TitleId), ReadFromEmployee = e => e.TitleId, Expected = newTitleId },
            new() { ProposalField = nameof(EmployeePersonalDataChange.BirthDate), ReadFromEmployee = e => e.BirthDate, Expected = newBirthDate },
            new() { ProposalField = nameof(EmployeePersonalDataChange.NationalityId), ReadFromEmployee = e => e.NationalityId, Expected = newNationalityId },
            new() { ProposalField = nameof(EmployeePersonalDataChange.ReligionId), ReadFromEmployee = e => e.ReligionId, Expected = newReligionId },
            new() { ProposalField = nameof(EmployeePersonalDataChange.Gender), ReadFromEmployee = e => e.Gender, Expected = Gender.Female },
            new() { ProposalField = nameof(EmployeePersonalDataChange.MaritalStatusId), ReadFromEmployee = e => e.MaritalStatusId, Expected = newMaritalStatusId },
            new() { ProposalField = nameof(EmployeePersonalDataChange.BirthPlaceId), ReadFromEmployee = e => e.BirthPlaceId, Expected = newBirthPlaceId },

            new() { ProposalField = nameof(EmployeePersonalDataChange.FlatNumn), ReadFromEmployee = e => e.FlatNumn, Expected = "AfterFlat" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.FloorNum), ReadFromEmployee = e => e.FloorNum, Expected = "AfterFloor" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.BuildingNum), ReadFromEmployee = e => e.BuildingNum, Expected = "AfterBuilding" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.StreetNum), ReadFromEmployee = e => e.StreetNum, Expected = "AfterStreet" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.BlockNum), ReadFromEmployee = e => e.BlockNum, Expected = "AfterBlock" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.ZipCode), ReadFromEmployee = e => e.ZipCode, Expected = "AfterZip" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.PACINumber), ReadFromEmployee = e => e.PACINumber, Expected = "AfterPACI" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.City), ReadFromEmployee = e => e.City, Expected = "AfterCity" },

            // Legacy called this Governate. The rename is the one field on this list whose two sides do not
            // share a name, so it is the one most likely to be dropped in a port.
            new() { ProposalField = nameof(EmployeePersonalDataChange.GovernorateId), ReadFromEmployee = e => e.GovernorateId, Expected = newGovernorateId },

            new() { ProposalField = nameof(EmployeePersonalDataChange.CountryId), ReadFromEmployee = e => e.CountryId, Expected = newCountryId },
            new() { ProposalField = nameof(EmployeePersonalDataChange.CurrentAddress), ReadFromEmployee = e => e.CurrentAddress, Expected = "After current address" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.PermanentAddress), ReadFromEmployee = e => e.PermanentAddress, Expected = "After permanent address" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.Telephone), ReadFromEmployee = e => e.Telephone, Expected = 22221111L },
            new() { ProposalField = nameof(EmployeePersonalDataChange.Mobile), ReadFromEmployee = e => e.Mobile, Expected = 55551111L },

            new() { ProposalField = nameof(EmployeePersonalDataChange.Email), ReadFromEmployee = e => e.Email, Expected = "after@example.com" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.AlternateEmail), ReadFromEmployee = e => e.AlternateEmail, Expected = "after.alt@example.com" },

            new() { ProposalField = nameof(EmployeePersonalDataChange.FatherNameEn), ReadFromEmployee = e => e.FatherNameEn, Expected = "AfterFatherEn" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.FatherNameAr), ReadFromEmployee = e => e.FatherNameAr, Expected = "AfterFatherAr" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.MotherNameEn), ReadFromEmployee = e => e.MotherNameEn, Expected = "AfterMotherEn" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.MotherNameAr), ReadFromEmployee = e => e.MotherNameAr, Expected = "AfterMotherAr" },

            new() { ProposalField = nameof(EmployeePersonalDataChange.BloodGroup), ReadFromEmployee = e => e.BloodGroup, Expected = "AB+" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.CriticalIllness), ReadFromEmployee = e => e.CriticalIllness, Expected = "After illness" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.EmergencyContact), ReadFromEmployee = e => e.EmergencyContact, Expected = "After contact" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.EmergencyNumber), ReadFromEmployee = e => e.EmergencyNumber, Expected = "After 112" },
            new() { ProposalField = nameof(EmployeePersonalDataChange.EmergencyRelation), ReadFromEmployee = e => e.EmergencyRelation, Expected = "After relation" }
        };

        expectations.Count.ShouldBe(40, "The legacy copy block assigns forty fields; this table must cover all of them.");

        var result = await _changes.RequestPersonalDataChange(input);

        await Resolve<IWorkflowApprovalService>().ApproveAsync(result.WorkflowRequestId.Value, approverId, "ok");

        var employee = ReloadEmployee(meId);

        var wrong = expectations
            .Where(x => !Equals(x.ReadFromEmployee(employee), x.Expected))
            .Select(x => $"{x.ProposalField}: expected '{x.Expected}' but the employee holds '{x.ReadFromEmployee(employee)}'")
            .ToList();

        wrong.ShouldBeEmpty(
            "Fields that did not reach the employee master record correctly:" +
            Environment.NewLine + string.Join(Environment.NewLine, wrong));
    }

    /// <summary>
    /// Fails if a field is ever added to the proposal without being added to the copy.
    ///
    /// <para>
    /// The copy test above can only check what it knows about, so on its own it would silently keep passing
    /// after somebody added a forty-first field and forgot the assignment. This compares the proposal's own
    /// declared fields against the table, which is what makes the pair complete.
    /// </para>
    /// </summary>
    [Fact]
    public void The_Copy_Should_Cover_Every_Field_On_The_Proposal()
    {
        // Not copyable data: identity, tenancy, ownership, the approver's note, the workflow columns and the
        // EF navigation properties (their FK ids are what is copied).
        var notCopied = new HashSet<string>
        {
            nameof(EmployeePersonalDataChange.TenantId),
            nameof(EmployeePersonalDataChange.EmployeeId),
            nameof(EmployeePersonalDataChange.Employee),
            nameof(EmployeePersonalDataChange.Comments),
            nameof(EmployeePersonalDataChange.PreviousValuesJson),
            nameof(EmployeePersonalDataChange.WorkflowRequestId),
            nameof(EmployeePersonalDataChange.WorkflowStatus),
            nameof(EmployeePersonalDataChange.Title),
            nameof(EmployeePersonalDataChange.Nationality),
            nameof(EmployeePersonalDataChange.Religion),
            nameof(EmployeePersonalDataChange.MaritalStatus),
            nameof(EmployeePersonalDataChange.Governorate),
            nameof(EmployeePersonalDataChange.Country),
            nameof(EmployeePersonalDataChange.BirthPlace)
        };

        var declared = typeof(EmployeePersonalDataChange)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(p => p.Name)
            .Where(n => !notCopied.Contains(n))
            .OrderBy(n => n)
            .ToList();

        declared.Count.ShouldBe(40,
            "The proposal declares " + declared.Count + " copyable fields; the copy covers forty. " +
            "If a field was added, add it to PersonalDataChangeWorkflowTransactionHandler.Apply and to the " +
            "expectation table in Approving_Should_Copy_Every_One_Of_The_Forty_Fields_Onto_The_Employee.");

        // And the copy really does touch each of them: a field left out of Apply would be reported here as
        // untouched even though the proposal carried a value for it.
        var proposal = new EmployeePersonalDataChange();
        var employee = new Employee();

        for (var i = 0; i < declared.Count; i++)
        {
            SetSampleValue(proposal, declared[i], i);
        }

        HCMv3.PersonnelAdministration.Workflow.PersonalDataChangeWorkflowTransactionHandler.Apply(proposal, employee);

        var untouched = declared
            .Where(name => !WasCopied(proposal, employee, name))
            .ToList();

        untouched.ShouldBeEmpty(
            "These proposal fields were supplied but never reached the employee:" +
            Environment.NewLine + string.Join(Environment.NewLine, untouched));
    }

    /// <summary>
    /// Puts a non-default, field-UNIQUE value on a proposal property, whatever its type. Unique matters: if
    /// every lookup id were the same number, one copied id would make all of them look copied.
    /// </summary>
    private static void SetSampleValue(EmployeePersonalDataChange proposal, string name, int index)
    {
        var property = typeof(EmployeePersonalDataChange).GetProperty(name);
        var type = Nullable.GetUnderlyingType(property!.PropertyType) ?? property.PropertyType;

        object value;
        if (type == typeof(string)) value = "sample-" + name;
        else if (type == typeof(DateTime)) value = new DateTime(1979, 1, 1).AddDays(index);
        else if (type == typeof(int)) value = 1000 + index;
        else if (type == typeof(long)) value = 500000L + index;
        else if (type.IsEnum) value = Enum.GetValues(type).Cast<object>().Last();
        else throw new InvalidOperationException($"No sample value defined for {name} ({type.Name}).");

        property.SetValue(proposal, value);
    }

    /// <summary>
    /// Whether the value the proposal carried actually landed somewhere on the employee. Matched by value
    /// rather than by name, so a copy that assigns the right value to the wrong Employee property is still
    /// counted -- the transposition case is the copy test's job, and this one is only about omissions.
    /// </summary>
    private static bool WasCopied(EmployeePersonalDataChange proposal, Employee employee, string name)
    {
        var proposed = typeof(EmployeePersonalDataChange).GetProperty(name)!.GetValue(proposal);

        return typeof(Employee)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(p => Equals(p.GetValue(employee), proposed));
    }

    #endregion

    #region Partial updates, rejection

    [Fact]
    public async Task A_Null_Proposed_Value_Should_Leave_The_Employees_Existing_Value_Untouched()
    {
        // The single most damaging thing this feature could do is blank a field nobody edited. An employee
        // changing only their mobile number sends only their mobile number (plus the four names the rules
        // require), and everything else must come back exactly as it was.
        var meId = CreateFullyPopulatedEmployee("PDU200", AbpSession.UserId);
        var approverId = EnableWorkflow("PDU200APP");

        var before = ReloadEmployee(meId);

        var input = MinimalInput(before);
        input.Mobile = 66660000;

        var result = await _changes.RequestPersonalDataChange(input);

        await Resolve<IWorkflowApprovalService>().ApproveAsync(result.WorkflowRequestId.Value, approverId, "ok");

        var after = ReloadEmployee(meId);

        after.Mobile.ShouldBe(66660000);

        // Everything else, field by field.
        after.SecondNameEn.ShouldBe(before.SecondNameEn);
        after.ThirdNameEn.ShouldBe(before.ThirdNameEn);
        after.SecondNameAr.ShouldBe(before.SecondNameAr);
        after.ThirdNameAr.ShouldBe(before.ThirdNameAr);
        after.TitleId.ShouldBe(before.TitleId);
        after.BirthDate.ShouldBe(before.BirthDate);
        after.NationalityId.ShouldBe(before.NationalityId);
        after.ReligionId.ShouldBe(before.ReligionId);
        after.Gender.ShouldBe(before.Gender);
        after.MaritalStatusId.ShouldBe(before.MaritalStatusId);
        after.BirthPlaceId.ShouldBe(before.BirthPlaceId);
        after.FlatNumn.ShouldBe(before.FlatNumn);
        after.FloorNum.ShouldBe(before.FloorNum);
        after.BuildingNum.ShouldBe(before.BuildingNum);
        after.StreetNum.ShouldBe(before.StreetNum);
        after.BlockNum.ShouldBe(before.BlockNum);
        after.ZipCode.ShouldBe(before.ZipCode);
        after.PACINumber.ShouldBe(before.PACINumber);
        after.City.ShouldBe(before.City);
        after.GovernorateId.ShouldBe(before.GovernorateId);
        after.CountryId.ShouldBe(before.CountryId);
        after.CurrentAddress.ShouldBe(before.CurrentAddress);
        after.PermanentAddress.ShouldBe(before.PermanentAddress);
        after.Telephone.ShouldBe(before.Telephone);
        after.Email.ShouldBe(before.Email);
        after.AlternateEmail.ShouldBe(before.AlternateEmail);
        after.FatherNameEn.ShouldBe(before.FatherNameEn);
        after.FatherNameAr.ShouldBe(before.FatherNameAr);
        after.MotherNameEn.ShouldBe(before.MotherNameEn);
        after.MotherNameAr.ShouldBe(before.MotherNameAr);
        after.BloodGroup.ShouldBe(before.BloodGroup);
        after.CriticalIllness.ShouldBe(before.CriticalIllness);
        after.EmergencyContact.ShouldBe(before.EmergencyContact);
        after.EmergencyNumber.ShouldBe(before.EmergencyNumber);
        after.EmergencyRelation.ShouldBe(before.EmergencyRelation);

        // And nothing that is not the employee's to change about themselves.
        after.Code.ShouldBe(before.Code);
        after.JoiningDate.ShouldBe(before.JoiningDate);
        after.ProbationEndDate.ShouldBe(before.ProbationEndDate);
        after.UserId.ShouldBe(before.UserId);
    }

    [Fact]
    public async Task A_Rejected_Request_Should_Change_Nothing_On_The_Employee()
    {
        var meId = CreateFullyPopulatedEmployee("PDU210", AbpSession.UserId);
        var approverId = EnableWorkflow("PDU210APP");

        var before = ReloadEmployee(meId);

        var input = MinimalInput(before);
        input.FirstNameEn = "RejectedFirstEn";
        input.Mobile = 77770000;
        input.Email = "rejected@example.com";
        input.CurrentAddress = "Rejected address";

        var result = await _changes.RequestPersonalDataChange(input);

        await Resolve<IWorkflowApprovalService>().RejectAsync(result.WorkflowRequestId.Value, approverId, "no");

        var after = ReloadEmployee(meId);

        after.FirstNameEn.ShouldBe(before.FirstNameEn);
        after.Mobile.ShouldBe(before.Mobile);
        after.Email.ShouldBe(before.Email);
        after.CurrentAddress.ShouldBe(before.CurrentAddress);
        after.FullNameEn.ShouldBe(before.FullNameEn);
    }

    [Fact]
    public async Task Approving_A_Name_Change_Should_Refresh_The_Stored_Full_Name()
    {
        // FullNameEn/Ar are stored, not computed at read time. A name change that left them alone would look
        // accepted and show the old name on every screen, letter and report that reads the stored value.
        var meId = CreateFullyPopulatedEmployee("PDU220", AbpSession.UserId);
        var approverId = EnableWorkflow("PDU220APP");

        var before = ReloadEmployee(meId);

        var input = MinimalInput(before);
        input.LastNameEn = "Married";
        input.LastNameAr = "متزوجة";

        var result = await _changes.RequestPersonalDataChange(input);

        await Resolve<IWorkflowApprovalService>().ApproveAsync(result.WorkflowRequestId.Value, approverId, "ok");

        var after = ReloadEmployee(meId);

        after.FullNameEn.ShouldBe("BeforeFirstEn BeforeSecondEn BeforeThirdEn Married");
        after.FullNameAr.ShouldBe("BeforeFirstAr BeforeSecondAr BeforeThirdAr متزوجة");
    }

    #endregion

    #region Validation

    [Theory]
    [InlineData(nameof(RequestPersonalDataChangeInput.FirstNameEn))]
    [InlineData(nameof(RequestPersonalDataChangeInput.FirstNameAr))]
    [InlineData(nameof(RequestPersonalDataChangeInput.LastNameEn))]
    [InlineData(nameof(RequestPersonalDataChangeInput.LastNameAr))]
    public async Task Should_Refuse_A_Request_That_Blanks_A_Required_Name(string fieldName)
    {
        // Ported from WorkFlowServices, not from the entity: these four are the rules that actually ran in
        // legacy. Note they are enforced in the HANDLER rather than as [Required] on the DTO, so they apply
        // however the request was raised -- and so that a mobile-number-only request is still possible.
        var meId = CreateFullyPopulatedEmployee("PDU300" + fieldName, AbpSession.UserId);
        EnableWorkflow("PDU300APP" + fieldName);

        var input = MinimalInput(ReloadEmployee(meId));
        typeof(RequestPersonalDataChangeInput).GetProperty(fieldName)!.SetValue(input, "   ");

        await Assert.ThrowsAsync<UserFriendlyException>(() => _changes.RequestPersonalDataChange(input));
    }

    [Fact]
    public async Task Should_Refuse_A_Birth_Date_That_Is_Not_Strictly_In_The_Past()
    {
        // Legacy used `>=`, so today is refused as well as the future.
        var meId = CreateFullyPopulatedEmployee("PDU310", AbpSession.UserId);
        EnableWorkflow("PDU310APP");

        var today = MinimalInput(ReloadEmployee(meId));
        today.BirthDate = DateTime.Today;

        await Assert.ThrowsAsync<UserFriendlyException>(() => _changes.RequestPersonalDataChange(today));

        var future = MinimalInput(ReloadEmployee(meId));
        future.BirthDate = DateTime.Today.AddDays(1);

        await Assert.ThrowsAsync<UserFriendlyException>(() => _changes.RequestPersonalDataChange(future));
    }

    [Fact]
    public async Task Should_Refuse_A_Birth_Date_On_Or_After_The_Joining_Date()
    {
        // The employee joined on 2015-01-01. Nobody joined the company before, or on, the day they were born.
        var meId = CreateFullyPopulatedEmployee("PDU320", AbpSession.UserId);
        EnableWorkflow("PDU320APP");

        var input = MinimalInput(ReloadEmployee(meId));
        input.BirthDate = new DateTime(2015, 1, 1);

        await Assert.ThrowsAsync<UserFriendlyException>(() => _changes.RequestPersonalDataChange(input));
    }

    [Fact]
    public async Task Should_Accept_A_Request_That_Does_Not_Touch_The_Birth_Date()
    {
        // The date rules must not fire on a request that did not propose a birth date. Legacy could not tell
        // the difference -- its BirthDate was non-nullable -- but refusing a phone-number change because of a
        // birth date nobody proposed would block an edit the employee cannot fix from this screen.
        var meId = CreateFullyPopulatedEmployee("PDU330", AbpSession.UserId);
        EnableWorkflow("PDU330APP");

        var input = MinimalInput(ReloadEmployee(meId));
        input.Mobile = 99990000;
        input.BirthDate = null;

        var result = await _changes.RequestPersonalDataChange(input);

        result.SubmittedForApproval.ShouldBeTrue();
    }

    #endregion

    #region Visibility and access

    [Fact]
    public async Task Should_Refuse_A_User_With_No_Employee_Record()
    {
        // Admin accounts are never linked to an employee, so this is the state the screen must handle rather
        // than an edge case.
        await Assert.ThrowsAsync<UserFriendlyException>(() => _changes.GetMyPersonalDataForEdit());
        await Assert.ThrowsAsync<UserFriendlyException>(() => _changes.GetMyPersonalDataChanges(ListInput()));
    }

    [Fact]
    public async Task A_Submitted_Change_Should_Be_Hidden_Until_It_Is_Approved()
    {
        var meId = CreateFullyPopulatedEmployee("PDU400", AbpSession.UserId);
        EnableWorkflow("PDU400APP");

        var before = ReloadEmployee(meId);
        var input = MinimalInput(before);
        input.Mobile = 88880000;

        var result = await _changes.RequestPersonalDataChange(input);

        result.SubmittedForApproval.ShouldBeTrue();
        result.WorkflowRequestId.ShouldNotBeNull();

        var unitOfWorkManager = Resolve<IUnitOfWorkManager>();

        using (unitOfWorkManager.Begin())
        {
            var proposals = Resolve<IRepository<EmployeePersonalDataChange>>();
            proposals.GetAll().Any(c => c.Id == result.TransactionId)
                .ShouldBeFalse("An undecided proposal was visible as if it had been agreed.");

            // THE assertion this whole design exists for. The employee themselves must stay visible while
            // their profile edit is in flight -- putting the workflow columns on Employee instead of on a
            // separate proposal row would hide a serving employee from payroll for as long as somebody's
            // phone-number change sat in an approver's inbox.
            Resolve<IRepository<Employee>>().GetAll().Any(e => e.Id == meId)
                .ShouldBeTrue("An in-flight profile edit hid the employee from the rest of the system.");
        }

        // And nothing has reached the master record yet.
        ReloadEmployee(meId).Mobile.ShouldBe(before.Mobile);

        // But the employee sees their own request.
        var mine = await _changes.GetMyPersonalDataChanges(ListInput());
        mine.Items.Select(i => i.Id).ShouldContain(result.TransactionId);
        mine.Items.Single(i => i.Id == result.TransactionId).Proposed.Mobile.ShouldBe(88880000);
    }

    [Fact]
    public async Task GetForEdit_Should_Pre_Fill_The_Employees_Current_Values()
    {
        // Without the pre-fill the employee retypes forty fields to change one, and every retyped field is a
        // chance to enter something slightly different -- the screen would manufacture the corruption this
        // feature exists to control.
        var meId = CreateFullyPopulatedEmployee("PDU500", AbpSession.UserId);

        var form = await _changes.GetMyPersonalDataForEdit();

        form.EmployeeId.ShouldBe(meId);
        form.HasPendingRequest.ShouldBeFalse();

        var current = form.Current;
        var employee = ReloadEmployee(meId);

        current.FirstNameEn.ShouldBe("BeforeFirstEn");
        current.LastNameAr.ShouldBe("BeforeLastAr");
        current.BirthDate.ShouldBe(new DateTime(1980, 3, 2));
        current.Gender.ShouldBe(Gender.Male);
        current.TitleId.ShouldBe(employee.TitleId);
        current.NationalityId.ShouldBe(employee.NationalityId);
        current.ReligionId.ShouldBe(employee.ReligionId);
        current.MaritalStatusId.ShouldBe(employee.MaritalStatusId);
        current.GovernorateId.ShouldBe(employee.GovernorateId);
        current.CountryId.ShouldBe(employee.CountryId);
        current.BirthPlaceId.ShouldBe(employee.BirthPlaceId);
        current.Mobile.ShouldBe(55550000);
        current.Telephone.ShouldBe(22220000);
        current.Email.ShouldBe("before@example.com");
        current.AlternateEmail.ShouldBe("before.alt@example.com");
        current.CurrentAddress.ShouldBe("Before current address");
        current.PermanentAddress.ShouldBe("Before permanent address");
        current.FatherNameEn.ShouldBe("BeforeFatherEn");
        current.MotherNameAr.ShouldBe("BeforeMotherAr");
        current.PACINumber.ShouldBe("BeforePACI");
        current.EmergencyRelation.ShouldBe("Before relation");
    }

    [Fact]
    public async Task GetForEdit_Should_Say_When_A_Request_Is_Already_In_Flight()
    {
        var meId = CreateFullyPopulatedEmployee("PDU510", AbpSession.UserId);
        EnableWorkflow("PDU510APP");

        await _changes.RequestPersonalDataChange(MinimalInput(ReloadEmployee(meId)));

        (await _changes.GetMyPersonalDataForEdit()).HasPendingRequest.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_To_Withdraw_Somebody_Elses_Request()
    {
        var otherId = CreateFullyPopulatedEmployee("PDU600OTHER", null);
        CreateFullyPopulatedEmployee("PDU600", AbpSession.UserId);
        EnableWorkflow("PDU600APP");

        // A proposal belonging to another employee, inserted directly -- there is no endpoint that would
        // create one for somebody else, which is the point.
        EmployeePersonalDataChange theirs = null;
        UsingDbContext(context =>
        {
            theirs = context.Set<EmployeePersonalDataChange>().Add(new EmployeePersonalDataChange
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                FirstNameEn = "Theirs",
                WorkflowStatus = RequestStatus.Open
            }).Entity;
        });

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _changes.CancelMyPersonalDataChange(new EntityDto(theirs.Id)));
    }

    #endregion

    #region Old value beside new value -- what the approver decides on

    /// <summary>The handler's own projection of a request, read inside a unit of work as the engine reads it.</summary>
    private async Task<WorkflowRequestDetail> BuildDetailAsync(int requestId, int actingEmployeeId)
    {
        using (var unitOfWork = Resolve<IUnitOfWorkManager>().Begin())
        {
            var request = await Resolve<IRepository<WorkflowRequest>>().GetAsync(requestId);
            var handler = Resolve<IWorkflowTransactionHandlerResolver>().Resolve(request.RequestType);

            var detail = await handler.BuildDetailAsync(new WorkflowTransactionContext(request, actingEmployeeId));

            await unitOfWork.CompleteAsync();

            return detail;
        }
    }

    [Fact]
    public async Task Should_Show_The_Approver_Only_The_Changed_Fields_With_Old_And_New_Values()
    {
        var meId = CreateFullyPopulatedEmployee("PDU700", AbpSession.UserId);
        var approverId = EnableWorkflow("PDU700APP");

        // The four names go on every request, unchanged. They must not be reported as changes.
        var input = MinimalInput(ReloadEmployee(meId));
        input.Mobile = 66660000;
        input.Email = "after@example.com";
        input.Comments = "New number.";

        var result = await _changes.RequestPersonalDataChange(input);

        var detail = await BuildDetailAsync(result.WorkflowRequestId.Value, approverId);

        detail.SubmissionComment.ShouldBe("New number.");
        detail.Fields.Select(f => f.Name).ShouldBe(new[]
        {
            nameof(EmployeePersonalDataChange.Mobile),
            nameof(EmployeePersonalDataChange.Email)
        });
        detail.Fields.ShouldAllBe(f => f.IsChange);

        var mobile = detail.Fields.Single(f => f.Name == nameof(EmployeePersonalDataChange.Mobile));
        mobile.PreviousValue.ShouldBe("55550000");
        mobile.Value.ShouldBe("66660000");

        var email = detail.Fields.Single(f => f.Name == nameof(EmployeePersonalDataChange.Email));
        email.PreviousValue.ShouldBe("before@example.com");
        email.Value.ShouldBe("after@example.com");
    }

    [Fact]
    public async Task Should_Show_A_Changed_Lookup_By_Name_And_A_Changed_Date_As_A_Date()
    {
        // An approver shown "Nationality: 14 -> 22" has been shown nothing.
        var meId = CreateFullyPopulatedEmployee("PDU710", AbpSession.UserId);
        var approverId = EnableWorkflow("PDU710APP");

        var input = MinimalInput(ReloadEmployee(meId));
        input.NationalityId = CreateLookup<Nationality>("After nationality");
        input.BirthPlaceId = CreateCity("After birthplace");
        input.Gender = Gender.Female;
        input.BirthDate = new DateTime(1985, 6, 15);

        var result = await _changes.RequestPersonalDataChange(input);

        var detail = await BuildDetailAsync(result.WorkflowRequestId.Value, approverId);

        var nationality = detail.Fields.Single(f => f.Name == nameof(EmployeePersonalDataChange.NationalityId));
        nationality.PreviousValue.ShouldBe("Before nationality");
        nationality.Value.ShouldBe("After nationality");

        var birthPlace = detail.Fields.Single(f => f.Name == nameof(EmployeePersonalDataChange.BirthPlaceId));
        birthPlace.PreviousValue.ShouldBe("Before birthplace");
        birthPlace.Value.ShouldBe("After birthplace");

        var gender = detail.Fields.Single(f => f.Name == nameof(EmployeePersonalDataChange.Gender));
        gender.PreviousValue.ShouldBe("Male");
        gender.Value.ShouldBe("Female");

        var birthDate = detail.Fields.Single(f => f.Name == nameof(EmployeePersonalDataChange.BirthDate));
        birthDate.Kind.ShouldBe(WorkflowDetailFieldKind.Date);
        birthDate.PreviousValue.ShouldBe("1980-03-02");
        birthDate.Value.ShouldBe("1985-06-15");
    }

    [Fact]
    public async Task Should_Still_Show_The_Old_Value_After_The_Request_Is_Approved()
    {
        // Approval overwrites the personnel file with the proposed values. Compared against the file, an
        // approved request would then report that it changed nothing.
        var meId = CreateFullyPopulatedEmployee("PDU720", AbpSession.UserId);
        var approverId = EnableWorkflow("PDU720APP");

        var input = MinimalInput(ReloadEmployee(meId));
        input.Mobile = 66660000;

        var result = await _changes.RequestPersonalDataChange(input);

        await Resolve<IWorkflowApprovalService>().ApproveAsync(result.WorkflowRequestId.Value, approverId, "ok");

        ReloadEmployee(meId).Mobile.ShouldBe(66660000);

        var detail = await BuildDetailAsync(result.WorkflowRequestId.Value, approverId);

        var mobile = detail.Fields.Single();
        mobile.Name.ShouldBe(nameof(EmployeePersonalDataChange.Mobile));
        mobile.PreviousValue.ShouldBe("55550000");
        mobile.Value.ShouldBe("66660000");
    }

    [Fact]
    public async Task Should_Show_A_Field_That_Was_Empty_Before_As_A_Change_From_Nothing()
    {
        var meId = CreateBareEmployee("PDU730");
        UsingDbContext(context =>
        {
            context.Set<Employee>().IgnoreQueryFilters().Single(e => e.Id == meId).UserId = AbpSession.UserId;
        });
        var approverId = EnableWorkflow("PDU730APP");

        var input = MinimalInput(ReloadEmployee(meId));
        input.Mobile = 66660000;
        input.EmergencyContact = "Somebody";

        var result = await _changes.RequestPersonalDataChange(input);

        var detail = await BuildDetailAsync(result.WorkflowRequestId.Value, approverId);

        // No mobile number is stored as zero; the approver is shown an empty old value, not "0".
        detail.Fields.Single(f => f.Name == nameof(EmployeePersonalDataChange.Mobile)).PreviousValue.ShouldBeNull();
        detail.Fields.Single(f => f.Name == nameof(EmployeePersonalDataChange.EmergencyContact)).PreviousValue.ShouldBeNull();
        detail.Fields.ShouldAllBe(f => f.IsChange);
    }

    #endregion

    #region Drafts, and requests sent back

    [Fact]
    public async Task Save_Should_File_A_Draft_Without_Sending_It_To_Anybody()
    {
        var meId = CreateFullyPopulatedEmployee("PDU800", AbpSession.UserId);
        EnableWorkflow("PDU800APP");

        var result = await _changes.SaveMyPersonalDataChangeDraft(new SavePersonalDataChangeDraftInput { Mobile = 66660000 });

        result.SavedAsDraft.ShouldBeTrue();
        result.SubmittedForApproval.ShouldBeFalse();
        result.WorkflowRequestId.ShouldBeNull();

        UsingDbContext(context => context.Set<WorkflowRequest>().IgnoreQueryFilters()
            .Any(r => r.TargetEntityId == result.TransactionId && r.RequestType == "EmployeePersonalDataChange")
            .ShouldBeFalse("A draft raised a workflow request."));

        var row = (await _changes.GetMyPersonalDataChanges(ListInput())).Items.Single(i => i.Id == result.TransactionId);
        row.IsDraft.ShouldBeTrue();
        row.CanEdit.ShouldBeTrue();
        row.RequestNumber.ShouldBeNull();

        ReloadEmployee(meId).Mobile.ShouldBe(55550000);
    }

    [Fact]
    public async Task Saving_A_Draft_Again_Should_Take_Out_A_Change_The_Form_No_Longer_Proposes()
    {
        CreateFullyPopulatedEmployee("PDU810", AbpSession.UserId);

        var saved = await _changes.SaveMyPersonalDataChangeDraft(
            new SavePersonalDataChangeDraftInput { Mobile = 66660000, Email = "first@example.com" });

        await _changes.SaveMyPersonalDataChangeDraft(
            new SavePersonalDataChangeDraftInput { Id = saved.TransactionId, Email = "second@example.com", Comments = "Changed my mind." });

        var draft = await _changes.GetMyPersonalDataChangeDraftForEdit(new EntityDto(saved.TransactionId));

        draft.Proposed.Mobile.ShouldBeNull();
        draft.Proposed.Email.ShouldBe("second@example.com");
        draft.Comments.ShouldBe("Changed my mind.");
        draft.IsDraft.ShouldBeTrue();
    }

    [Fact]
    public async Task Submitting_A_Draft_Should_Send_It_For_Approval_And_Approval_Should_Apply_It()
    {
        var meId = CreateFullyPopulatedEmployee("PDU820", AbpSession.UserId);
        var approverId = EnableWorkflow("PDU820APP");

        var input = new SavePersonalDataChangeDraftInput { Mobile = 66660000 };
        var names = MinimalInput(ReloadEmployee(meId));
        input.FirstNameEn = names.FirstNameEn;
        input.LastNameEn = names.LastNameEn;
        input.FirstNameAr = names.FirstNameAr;
        input.LastNameAr = names.LastNameAr;

        var saved = await _changes.SaveMyPersonalDataChangeDraft(input);

        var submitted = await _changes.SubmitMyPersonalDataChangeDraft(new EntityDto(saved.TransactionId));

        submitted.SubmittedForApproval.ShouldBeTrue();
        submitted.WorkflowRequestId.ShouldNotBeNull();

        var row = (await _changes.GetMyPersonalDataChanges(ListInput())).Items.Single(i => i.Id == saved.TransactionId);
        row.IsDraft.ShouldBeFalse();
        row.CanEdit.ShouldBeFalse();
        row.RequestNumber.ShouldNotBeNullOrWhiteSpace();

        await Resolve<IWorkflowApprovalService>().ApproveAsync(submitted.WorkflowRequestId.Value, approverId, "ok");

        ReloadEmployee(meId).Mobile.ShouldBe(66660000);
    }

    [Fact]
    public async Task Submitting_A_Draft_Should_Apply_The_Same_Rules_As_A_New_Request()
    {
        CreateFullyPopulatedEmployee("PDU830", AbpSession.UserId);
        EnableWorkflow("PDU830APP");

        // Saved without the names: allowed for a draft, refused when it is sent.
        var saved = await _changes.SaveMyPersonalDataChangeDraft(new SavePersonalDataChangeDraftInput { Mobile = 66660000 });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _changes.SubmitMyPersonalDataChangeDraft(new EntityDto(saved.TransactionId)));
    }

    [Fact]
    public async Task Should_Refuse_To_Send_A_Request_While_Approval_Is_Switched_Off_For_This_Type()
    {
        // With no approver the proposal would be filed as "recorded" and never reach the personnel file,
        // because the copy onto the employee happens on approval. Legacy refused a request with no chain.
        var meId = CreateFullyPopulatedEmployee("PDU835", AbpSession.UserId);

        var input = MinimalInput(ReloadEmployee(meId));
        input.Mobile = 66660000;

        await Assert.ThrowsAsync<UserFriendlyException>(() => _changes.RequestPersonalDataChange(input));

        // A draft can still be saved -- it is sent to nobody -- but not submitted.
        var saved = await _changes.SaveMyPersonalDataChangeDraft(new SavePersonalDataChangeDraftInput
        {
            FirstNameEn = input.FirstNameEn,
            LastNameEn = input.LastNameEn,
            FirstNameAr = input.FirstNameAr,
            LastNameAr = input.LastNameAr,
            Mobile = 66660000
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _changes.SubmitMyPersonalDataChangeDraft(new EntityDto(saved.TransactionId)));

        UsingDbContext(context => context.Set<EmployeePersonalDataChange>().IgnoreQueryFilters()
            .Single(c => c.Id == saved.TransactionId).WorkflowStatus
            .ShouldBe(RequestStatus.Draft));

        ReloadEmployee(meId).Mobile.ShouldBe(55550000);
    }

    [Fact]
    public async Task A_Request_Sent_Back_Should_Be_Corrected_And_Sent_Again_Under_The_Same_Request()
    {
        var meId = CreateFullyPopulatedEmployee("PDU840", AbpSession.UserId);
        var approverId = EnableWorkflow("PDU840APP");

        var input = MinimalInput(ReloadEmployee(meId));
        input.Mobile = 66660000;

        var result = await _changes.RequestPersonalDataChange(input);

        await Resolve<IWorkflowApprovalService>().SendBackAsync(result.WorkflowRequestId.Value, approverId, "Wrong number.");

        var row = (await _changes.GetMyPersonalDataChanges(ListInput())).Items.Single(i => i.Id == result.TransactionId);
        row.IsSentBack.ShouldBeTrue();
        row.CanEdit.ShouldBeTrue();

        var corrected = new SavePersonalDataChangeDraftInput
        {
            Id = result.TransactionId,
            FirstNameEn = input.FirstNameEn,
            LastNameEn = input.LastNameEn,
            FirstNameAr = input.FirstNameAr,
            LastNameAr = input.LastNameAr,
            Mobile = 77770000
        };

        await _changes.SaveMyPersonalDataChangeDraft(corrected);

        var resubmitted = await _changes.SubmitMyPersonalDataChangeDraft(new EntityDto(result.TransactionId));

        resubmitted.WorkflowRequestId.ShouldBe(result.WorkflowRequestId);

        var detail = await BuildDetailAsync(result.WorkflowRequestId.Value, approverId);
        var mobile = detail.Fields.Single();
        mobile.PreviousValue.ShouldBe("55550000");
        mobile.Value.ShouldBe("77770000");
    }

    [Fact]
    public async Task Should_Refuse_To_Open_Or_Change_Somebody_Elses_Draft()
    {
        var otherId = CreateFullyPopulatedEmployee("PDU850OTHER", null);
        CreateFullyPopulatedEmployee("PDU850", AbpSession.UserId);

        EmployeePersonalDataChange theirs = null;
        UsingDbContext(context =>
        {
            theirs = context.Set<EmployeePersonalDataChange>().Add(new EmployeePersonalDataChange
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                Mobile = 11110000,
                WorkflowStatus = RequestStatus.Draft
            }).Entity;
        });

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _changes.GetMyPersonalDataChangeDraftForEdit(new EntityDto(theirs.Id)));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _changes.SaveMyPersonalDataChangeDraft(new SavePersonalDataChangeDraftInput { Id = theirs.Id, Mobile = 99990000 }));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _changes.SubmitMyPersonalDataChangeDraft(new EntityDto(theirs.Id)));
    }

    #endregion

    #region The form's pickers

    [Fact]
    public async Task Lookups_Should_Offer_The_Choices_For_All_Seven_Pickers()
    {
        // The fixture creates one row in each of the seven tables.
        CreateFullyPopulatedEmployee("PDU900", AbpSession.UserId);

        var lookups = await _changes.GetMyPersonalDataLookups();

        lookups.Titles.Select(l => l.NameEn).ShouldContain("Before title");
        lookups.Nationalities.Select(l => l.NameEn).ShouldContain("Before nationality");
        lookups.Religions.Select(l => l.NameEn).ShouldContain("Before religion");
        lookups.MaritalStatuses.Select(l => l.NameEn).ShouldContain("Before marital");
        lookups.Cities.Select(l => l.NameEn).ShouldContain("Before birthplace");
        lookups.Governorates.Select(l => l.NameEn).ShouldContain("Before governorate");
        lookups.Countries.Select(l => l.NameEn).ShouldContain("Before country");
    }

    #endregion
}
