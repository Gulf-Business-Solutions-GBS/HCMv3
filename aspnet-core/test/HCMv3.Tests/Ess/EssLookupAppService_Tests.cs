using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Authorization.Users;
using HCMv3.Authorization;
using HCMv3.Authorization.Users;
using HCMv3.Ess;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TravelManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// The self-service lookup service, and the one thing it exists to prove: that an ordinary employee -- who
/// holds <c>Ess</c> and not one of the fifteen administrator permissions that own these reference tables --
/// can actually fill in a form.
///
/// <para>
/// Before this service, every self-service dropdown read from an admin service gated behind
/// <c>Pages_GeneralMasterInfo_Banks</c>, <c>Pages_PayrollManagement_AbsenceTypes</c> and the rest. The bug
/// survived because every test and every manual pass ran as an administrator, who holds all of them. So the
/// central test here does the opposite: it builds a user granted <c>Ess</c> and nothing else, and calls
/// every method through the real authorization interceptor.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class EssLookupAppService_Tests : AppTestBase
{
    private readonly IEssLookupAppService _lookups;

    public EssLookupAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _lookups = Resolve<IEssLookupAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static string Unique() => Guid.NewGuid().ToString("N").Substring(0, 8);

    // ---------------------------------------------------------------- fixtures

    private int CreateBank(string name, bool deleted = false)
    {
        var id = 0;

        UsingDbContext(context =>
        {
            var bank = context.Set<Bank>().Add(new Bank
            {
                TenantId = TenantId,
                Code = Unique(),
                NameEn = name,
                NameAr = name,
                IsDeleted = deleted
            }).Entity;

            context.SaveChanges();
            id = bank.Id;
        });

        return id;
    }

    private int CreateAsset(string name, AssetStatus status)
    {
        var id = 0;

        UsingDbContext(context =>
        {
            var asset = context.Set<Asset>().Add(new Asset
            {
                TenantId = TenantId,
                Code = Unique(),
                NameEn = name,
                NameAr = name,
                Status = status
            }).Entity;

            context.SaveChanges();
            id = asset.Id;
        });

        return id;
    }

    private int CreateDocumentType(string name, bool isLetterType)
    {
        var id = 0;

        UsingDbContext(context =>
        {
            var type = context.Set<DocumentType>().Add(new DocumentType
            {
                TenantId = TenantId,
                Code = Unique(),
                NameEn = name,
                NameAr = name,
                IsLetterType = isLetterType
            }).Entity;

            context.SaveChanges();
            id = type.Id;
        });

        return id;
    }

    private int CreateEmployee(string code, EmployeeStatus status = EmployeeStatus.Active, long? userId = null)
    {
        Nationality nationality = null;

        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Unique(),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

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
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                Status = status,
                UserId = userId,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    /// <summary>
    /// A user granted <c>Ess</c> and nothing else -- no roles, so no administrator permission can leak in
    /// through one. This is the account the whole change is for.
    /// </summary>
    private long CreateEssOnlyUser()
    {
        var userId = 0L;

        UsingDbContext(context =>
        {
            var user = new User
            {
                TenantId = AbpSession.TenantId,
                UserName = "ess" + Unique(),
                Name = "Ess",
                Surname = "Only",
                EmailAddress = "ess" + Unique() + "@testdomain.com",
                IsEmailConfirmed = true,
                Password = "AM4OLBpptxBYmM79lGOX9egzZk3vIQU3d/gFCJzaBjAPXzYIK3tQ2N7X4fcrHtElTw==", //123qwe
                Permissions = new List<UserPermissionSetting>
                {
                    new UserPermissionSetting
                    {
                        Name = AppPermissions.Ess,
                        IsGranted = true,
                        TenantId = AbpSession.TenantId
                    }
                }
            };

            user.SetNormalizedNames();

            var added = context.Users.Add(user).Entity;
            context.SaveChanges();
            userId = added.Id;
        });

        return userId;
    }

    // ---------------------------------------------------------------- THE test

    [Fact]
    public async Task An_Employee_Holding_Only_The_Ess_Permission_Should_Be_Able_To_Read_Every_Lookup()
    {
        // The entire point of the change. Each of these lists was previously owned by an app service
        // carrying an administrator permission, so this call sequence is exactly what used to fail -- and
        // failed unnoticed because testing was done as an administrator, who holds all fifteen.
        CreateBank("Test bank");
        CreateAsset("Test laptop", AssetStatus.Active);
        CreateDocumentType("Salary certificate", isLetterType: true);

        AbpSession.UserId = CreateEssOnlyUser();

        var lookups = Resolve<IEssLookupAppService>();

        // Every method, called through the real authorization interceptor. An admin permission anywhere in
        // this service would throw AbpAuthorizationException here.
        await lookups.GetAbsenceTypes();
        await lookups.GetAssets();
        await lookups.GetAttendanceTypes();
        await lookups.GetBanks();
        await lookups.GetCurrencies();
        await lookups.GetDocumentTypes();
        await lookups.GetLetterTypes();
        await lookups.GetExpenseTypes();
        await lookups.GetHRServiceTypes();
        await lookups.GetHotels();
        await lookups.GetLoanTypes();
        await lookups.GetOvertimeTypes();
        await lookups.GetTicketTypes();
        await lookups.GetPunchTypes();
        await lookups.GetTravelAirlines();
        await lookups.GetTravelDestinations();
        await lookups.GetDelegationCandidates();

        // And the data actually arrives -- an empty list would satisfy "did not throw" while leaving the
        // dropdown just as unusable as before.
        (await lookups.GetBanks()).ShouldNotBeEmpty();
        (await lookups.GetAssets()).ShouldNotBeEmpty();
        (await lookups.GetLetterTypes()).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task A_User_Holding_No_Ess_Permission_Should_Be_Refused()
    {
        // The other half of the guarantee: the new door is narrow, not open. A user with no permissions at
        // all must not be able to enumerate the tenant's reference data or its staff list.
        var userId = 0L;

        UsingDbContext(context =>
        {
            var user = new User
            {
                TenantId = AbpSession.TenantId,
                UserName = "nop" + Unique(),
                Name = "No",
                Surname = "Permissions",
                EmailAddress = "nop" + Unique() + "@testdomain.com",
                IsEmailConfirmed = true,
                Password = "AM4OLBpptxBYmM79lGOX9egzZk3vIQU3d/gFCJzaBjAPXzYIK3tQ2N7X4fcrHtElTw=="
            };

            user.SetNormalizedNames();

            var added = context.Users.Add(user).Entity;
            context.SaveChanges();
            userId = added.Id;
        });

        AbpSession.UserId = userId;

        var lookups = Resolve<IEssLookupAppService>();

        await Assert.ThrowsAsync<AbpAuthorizationException>(() => lookups.GetBanks());
        await Assert.ThrowsAsync<AbpAuthorizationException>(() => lookups.GetDelegationCandidates());
    }

    [Fact]
    public void The_Service_Should_Be_Gated_On_The_General_Ess_Permission_Only()
    {
        // Guards the regression directly: if somebody later "tidies" this service by reusing an admin
        // permission, or adds a method carrying one, the bug comes straight back and no type-check or
        // Angular build would notice.
        var authorize = typeof(EssLookupAppService).GetCustomAttribute<AbpAuthorizeAttribute>();

        authorize.ShouldNotBeNull("The lookup service is not authorized at all.");
        authorize.Permissions.ShouldBe(new[] { AppPermissions.Ess });

        var adminPermissions = new[]
        {
            AppPermissions.Pages_PayrollManagement_AbsenceTypes,
            AppPermissions.Pages_GeneralMasterInfo_Assets,
            AppPermissions.Pages_PayrollManagement_AttendanceTypes,
            AppPermissions.Pages_GeneralMasterInfo_Banks,
            AppPermissions.Pages_GeneralMasterInfo_Currencies,
            AppPermissions.Pages_GeneralMasterInfo_DocumentTypes,
            AppPermissions.Pages_TravelManagement_ExpenseTypes,
            AppPermissions.Pages_GeneralMasterInfo_HRServiceTypes,
            AppPermissions.Pages_TravelManagement_Hotels,
            AppPermissions.Pages_GeneralMasterInfo_LoanTypes,
            AppPermissions.Pages_GeneralMasterInfo_OvertimeTypes,
            AppPermissions.Pages_PayrollManagement_TicketTypes,
            AppPermissions.Pages_TimeManagement_TransTypes,
            AppPermissions.Pages_TravelManagement_TravelAirlines,
            AppPermissions.Pages_TravelManagement_TravelDestinations,
            AppPermissions.Pages_PersonnelAdministration_Employees
        };

        foreach (var method in typeof(EssLookupAppService).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            var methodAuthorize = method.GetCustomAttribute<AbpAuthorizeAttribute>();

            if (methodAuthorize == null)
            {
                continue;
            }

            foreach (var permission in methodAuthorize.Permissions)
            {
                adminPermissions.ShouldNotContain(permission,
                    $"{method.Name} carries the administrator permission {permission}, which no employee holds.");
            }
        }
    }

    [Fact]
    public void The_Ess_Permission_Should_Be_Registered()
    {
        // Guards against [AbpAuthorize] naming a permission that AppAuthorizationProvider never creates, in
        // which case it could never be granted to anybody and every call would be refused.
        Resolve<IPermissionManager>().GetPermissionOrNull(AppPermissions.Ess).ShouldNotBeNull();
    }

    // ---------------------------------------------------------------- row filtering

    [Fact]
    public async Task Assets_Should_Offer_Only_Active_Ones()
    {
        var active = CreateAsset("Active laptop", AssetStatus.Active);
        var retired = CreateAsset("Retired laptop", AssetStatus.Inactive);

        var assets = await _lookups.GetAssets();

        assets.Select(a => a.Id).ShouldContain(active);
        assets.Select(a => a.Id).ShouldNotContain(retired, "A retired asset was offered as requestable.");
    }

    [Fact]
    public async Task Soft_Deleted_Rows_Should_Never_Be_Offered()
    {
        // Relies on ABP's global ISoftDelete filter rather than an explicit predicate, so this asserts the
        // service does not disable it -- which the My* services legitimately do, for ApprovedOnly.
        var live = CreateBank("Live bank");
        var deleted = CreateBank("Closed bank", deleted: true);

        var banks = await _lookups.GetBanks();

        banks.Select(b => b.Id).ShouldContain(live);
        banks.Select(b => b.Id).ShouldNotContain(deleted, "A deleted bank was offered.");
    }

    [Fact]
    public async Task Letter_Types_Should_Exclude_Document_Types_That_Are_Not_Letters()
    {
        // The rule DocumentTypesAppService.GetAll applies via OnlyLetterTypes. A passport is a document HR
        // holds; it is not something that can be "requested as a letter".
        var letter = CreateDocumentType("Salary certificate", isLetterType: true);
        var passport = CreateDocumentType("Passport", isLetterType: false);

        var letterTypes = await _lookups.GetLetterTypes();

        letterTypes.Select(t => t.Id).ShouldContain(letter);
        letterTypes.Select(t => t.Id).ShouldNotContain(passport,
            "A passport was offered on the letter request form.");
    }

    [Fact]
    public async Task The_Document_Type_List_Should_Not_Be_Narrowed_To_Letters()
    {
        // The counterpart to the test above -- the two lists are different questions, and the document
        // screen must still see everything.
        var letter = CreateDocumentType("Salary certificate", isLetterType: true);
        var passport = CreateDocumentType("Passport", isLetterType: false);

        var all = (await _lookups.GetDocumentTypes()).Select(t => t.Id).ToList();

        all.ShouldContain(letter);
        all.ShouldContain(passport);
    }

    [Fact]
    public async Task Lookups_Should_Return_Id_And_Name_Rather_Than_The_Full_Admin_Record()
    {
        // The shape matters as much as the permission: an employee reading the bank list must not receive
        // SWIFT codes and company codes along with it.
        CreateBank("Shape test bank");

        var bank = (await _lookups.GetBanks()).First();

        bank.GetType().ShouldBe(typeof(SimpleLookupDto));
        typeof(SimpleLookupDto).GetProperties().Select(p => p.Name)
            .OrderBy(n => n)
            .ShouldBe(new[] { "Code", "Id", "NameAr", "NameEn" });
    }

    // ---------------------------------------------------------------- the colleague picker

    [Fact]
    public async Task Delegation_Candidates_Should_Exclude_The_Signed_In_Employee()
    {
        // You cannot delegate to yourself -- MyDelegationAppService refuses it and so does the handler. The
        // option should never be offered in the first place.
        var meId = CreateEmployee("DEL100ME", userId: AbpSession.UserId);
        var colleagueId = CreateEmployee("DEL100COL");

        var candidates = await _lookups.GetDelegationCandidates();

        candidates.Select(c => c.Id).ShouldNotContain(meId, "The employee was offered themselves as a delegate.");
        candidates.Select(c => c.Id).ShouldContain(colleagueId);
    }

    [Fact]
    public async Task Delegation_Candidates_Should_Exclude_Inactive_Employees()
    {
        CreateEmployee("DEL200ME", userId: AbpSession.UserId);
        var leaver = CreateEmployee("DEL200LEAVER", EmployeeStatus.Inactive);
        var colleague = CreateEmployee("DEL200COL");

        var candidates = await _lookups.GetDelegationCandidates();

        candidates.Select(c => c.Id).ShouldNotContain(leaver, "A former employee was offered as a delegate.");
        candidates.Select(c => c.Id).ShouldContain(colleague);
    }

    [Fact]
    public async Task Delegation_Candidates_Should_Not_Be_Narrowed_To_Nothing()
    {
        // The scoping decision, asserted. Colleagues are deliberately NOT filtered by company or department
        // -- Employee carries no such column, and the date-bound org assignment that does would silently
        // drop anybody between assignments. A picker with no candidates is the same bug in a new coat.
        CreateEmployee("DEL300ME", userId: AbpSession.UserId);

        var first = CreateEmployee("DEL300A");
        var second = CreateEmployee("DEL300B");

        var candidates = await _lookups.GetDelegationCandidates();

        candidates.Select(c => c.Id).ShouldContain(first);
        candidates.Select(c => c.Id).ShouldContain(second);
    }

    [Fact]
    public async Task Delegation_Candidates_Should_Expose_Only_A_Name_And_A_Staff_Number()
    {
        // An employee picking who covers for them gets a name and a staff number. Not birth dates, not
        // contact details, not nationality -- which is what EmployeeDto would have handed over.
        CreateEmployee("DEL400ME", userId: AbpSession.UserId);
        CreateEmployee("DEL400COL");

        var candidate = (await _lookups.GetDelegationCandidates()).First();

        candidate.GetType().GetProperties().Select(p => p.Name)
            .OrderBy(n => n)
            .ShouldBe(new[] { "Code", "FullNameAr", "FullNameEn", "Id" });
    }

    [Fact]
    public async Task Delegation_Candidates_Should_Be_Returned_For_A_User_With_No_Employee_Record()
    {
        // An unlinked user (an administrator account, typically) has nobody to exclude. Returning the list
        // rather than throwing keeps the failure where it belongs -- on the submit, which refuses a
        // delegation from a user with no employee row.
        var colleague = CreateEmployee("DEL500COL");

        var candidates = await _lookups.GetDelegationCandidates();

        candidates.Select(c => c.Id).ShouldContain(colleague);
    }

    // ---------------------------------------------------------------- ordering and tenancy

    [Fact]
    public async Task Lookups_Should_Come_Back_Ordered_By_English_Name()
    {
        // Matching GetTravelTypes and GetClearanceTypes. The admin lists sort by Code, which is right for a
        // maintenance grid and useless in a dropdown somebody is scanning for a word.
        CreateBank("Zebra Bank");
        CreateBank("Alpha Bank");

        var names = (await _lookups.GetBanks()).Select(b => b.NameEn).ToList();

        names.ShouldBe(names.OrderBy(n => n, StringComparer.Ordinal).ToList(), true);
    }

    [Fact]
    public async Task Lookups_Should_Not_Cross_Tenants()
    {
        // ABP's MustHaveTenant filter does this, so what is really asserted is that the service does not
        // disable it.
        var mine = CreateBank("My tenant bank");

        var otherTenantBankId = 0;

        UsingDbContext(context =>
        {
            var bank = context.Set<Bank>().Add(new Bank
            {
                TenantId = TenantId + 999,
                Code = Unique(),
                NameEn = "Other tenant bank",
                NameAr = "Other tenant bank"
            }).Entity;

            context.SaveChanges();
            otherTenantBankId = bank.Id;
        });

        var banks = await _lookups.GetBanks();

        banks.Select(b => b.Id).ShouldContain(mine);
        banks.Select(b => b.Id).ShouldNotContain(otherTenantBankId, "A bank from another tenant was returned.");
    }

    // ---------------------------------------------------------------- the remaining lists

    [Fact]
    public async Task Every_Reference_List_Should_Return_What_Was_Configured()
    {
        // One row per list, so that a list wired to the wrong repository -- easy to do in a service with
        // sixteen of them -- fails here rather than in front of an employee.
        var absenceTypeId = 0;
        var attendanceTypeId = 0;
        var currencyId = 0;
        var expenseTypeId = 0;
        var hrServiceTypeId = 0;
        var hotelId = 0;
        var loanTypeId = 0;
        var overtimeTypeId = 0;
        var ticketTypeId = 0;
        var transTypeId = 0;
        var airlineId = 0;
        var destinationId = 0;

        UsingDbContext(context =>
        {
            var absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            { TenantId = TenantId, Code = Unique(), NameEn = "Annual leave", NameAr = "Annual leave" }).Entity;

            var attendanceType = context.Set<AttendanceType>().Add(new AttendanceType
            { TenantId = TenantId, Code = Unique(), NameEn = "Forgot to punch", NameAr = "Forgot to punch" }).Entity;

            var currency = context.Set<Currency>().Add(new Currency
            { TenantId = TenantId, Code = Unique(), NameEn = "Kuwaiti Dinar", NameAr = "Kuwaiti Dinar" }).Entity;

            var expenseType = context.Set<ExpenseType>().Add(new ExpenseType
            { TenantId = TenantId, Code = Unique(), NameEn = "Airfare", NameAr = "Airfare" }).Entity;

            var hrServiceType = context.Set<HRServiceType>().Add(new HRServiceType
            { TenantId = TenantId, Code = Unique(), NameEn = "Salary certificate", NameAr = "Salary certificate" }).Entity;

            var hotel = context.Set<Hotel>().Add(new Hotel
            { TenantId = TenantId, Code = Unique(), NameEn = "Airport Hotel", NameAr = "Airport Hotel" }).Entity;

            var loanType = context.Set<LoanType>().Add(new LoanType
            { TenantId = TenantId, Code = Unique(), NameEn = "Housing loan", NameAr = "Housing loan" }).Entity;

            var overtimeType = context.Set<OvertimeType>().Add(new OvertimeType
            { TenantId = TenantId, Code = Unique(), NameEn = "Weekday overtime", NameAr = "Weekday overtime" }).Entity;

            var ticketType = context.Set<TicketType>().Add(new TicketType
            { TenantId = TenantId, Code = Unique(), NameEn = "Annual ticket", NameAr = "Annual ticket" }).Entity;

            var transType = context.Set<TransType>().Add(new TransType
            { TenantId = TenantId, Code = Unique(), NameEn = "Check in", NameAr = "Check in" }).Entity;

            var airline = context.Set<TravelAirline>().Add(new TravelAirline
            { TenantId = TenantId, Code = Unique(), NameEn = "Kuwait Airways", NameAr = "Kuwait Airways" }).Entity;

            var destination = context.Set<TravelDestination>().Add(new TravelDestination
            { TenantId = TenantId, Code = Unique(), NameEn = "London", NameAr = "London" }).Entity;

            // Ids are only assigned on save, so they are read after it rather than off the tracked entry.
            context.SaveChanges();

            absenceTypeId = absenceType.Id;
            attendanceTypeId = attendanceType.Id;
            currencyId = currency.Id;
            expenseTypeId = expenseType.Id;
            hrServiceTypeId = hrServiceType.Id;
            hotelId = hotel.Id;
            loanTypeId = loanType.Id;
            overtimeTypeId = overtimeType.Id;
            ticketTypeId = ticketType.Id;
            transTypeId = transType.Id;
            airlineId = airline.Id;
            destinationId = destination.Id;
        });

        (await _lookups.GetAbsenceTypes()).Select(x => x.Id).ShouldContain(absenceTypeId);
        (await _lookups.GetAttendanceTypes()).Select(x => x.Id).ShouldContain(attendanceTypeId);
        (await _lookups.GetCurrencies()).Select(x => x.Id).ShouldContain(currencyId);
        (await _lookups.GetExpenseTypes()).Select(x => x.Id).ShouldContain(expenseTypeId);
        (await _lookups.GetHRServiceTypes()).Select(x => x.Id).ShouldContain(hrServiceTypeId);
        (await _lookups.GetHotels()).Select(x => x.Id).ShouldContain(hotelId);
        (await _lookups.GetLoanTypes()).Select(x => x.Id).ShouldContain(loanTypeId);
        (await _lookups.GetOvertimeTypes()).Select(x => x.Id).ShouldContain(overtimeTypeId);
        (await _lookups.GetTicketTypes()).Select(x => x.Id).ShouldContain(ticketTypeId);
        (await _lookups.GetPunchTypes()).Select(x => x.Id).ShouldContain(transTypeId);
        (await _lookups.GetTravelAirlines()).Select(x => x.Id).ShouldContain(airlineId);
        (await _lookups.GetTravelDestinations()).Select(x => x.Id).ShouldContain(destinationId);
    }
}
