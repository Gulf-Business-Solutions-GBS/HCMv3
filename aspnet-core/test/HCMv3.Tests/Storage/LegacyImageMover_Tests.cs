using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Recruitment;
using HCMv3.Storage;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Storage;

/// <summary>
/// Moving photos and signatures out of the database. After a run the old columns must be empty, the bytes
/// must be in the tenant's own folder, and a second run must find nothing to do.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LegacyImageMover_Tests : AppTestBase
{
    private readonly ILegacyImageMover _mover;
    private readonly IEntityImageStore _images;

    public LegacyImageMover_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _mover = Resolve<ILegacyImageMover>();
        _images = Resolve<IEntityImageStore>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateEmployee(byte[] legacyPhoto, string photoPath = null, bool deleted = false)
    {
        Employee employee = null;
        UsingDbContext(context =>
        {
            var nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N")[..8],
                NameEn = "N",
                NameAr = "N"
            }).Entity;
            context.SaveChanges();

            var code = "MV" + Guid.NewGuid().ToString("N")[..6];
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = code,
                LastNameEn = "T",
                FirstNameAr = "ت",
                LastNameAr = "ت",
                FullNameEn = code,
                FullNameAr = "ت",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id,
                LegacyPhoto = legacyPhoto,
                PhotoPath = photoPath,
                IsDeleted = deleted
            }).Entity;
        });

        return employee.Id;
    }

    private Employee LoadEmployee(int id)
    {
        Employee employee = null;
        UsingDbContext(context => employee = context.Set<Employee>().IgnoreQueryFilters().First(e => e.Id == id));

        return employee;
    }

    private async Task<int> MoveAsync()
    {
        var moved = 0;
        await WithUnitOfWorkAsync(async () => moved = await _mover.MoveBatchAsync(100));

        return moved;
    }

    [Fact]
    public async Task Should_Move_An_Employee_Photo_Into_The_Tenants_Folder_And_Empty_The_Column()
    {
        var id = CreateEmployee(new byte[] { 1, 2, 3 });

        (await MoveAsync()).ShouldBeGreaterThanOrEqualTo(1);

        var employee = LoadEmployee(id);
        employee.LegacyPhoto.ShouldBeNull();
        employee.PhotoPath.ShouldStartWith($"tenant-{TenantId}/employee-photos/{id}-");
        (await _images.ReadOrNullAsync(TenantId, employee.PhotoPath)).ShouldBe(new byte[] { 1, 2, 3 });
    }

    [Fact]
    public async Task A_Second_Run_Should_Find_Nothing_To_Move()
    {
        CreateEmployee(new byte[] { 1 });
        await MoveAsync();

        (await MoveAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Not_Overwrite_A_Newer_Photo_Already_On_Disk()
    {
        var newer = await _images.ReplaceAsync(EntityImageKinds.EmployeePhoto, TenantId, 999, null, new byte[] { 7 });
        var id = CreateEmployee(new byte[] { 1 }, photoPath: newer);

        await MoveAsync();

        var employee = LoadEmployee(id);
        employee.PhotoPath.ShouldBe(newer);
        employee.LegacyPhoto.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Move_A_Soft_Deleted_Employees_Photo_Too()
    {
        var id = CreateEmployee(new byte[] { 5 }, deleted: true);

        await MoveAsync();

        LoadEmployee(id).PhotoPath.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Move_Applicant_Photos_And_Signatures()
    {
        var employeeId = CreateEmployee(null);
        Applicant applicant = null;
        EmployeeSignature signature = null;
        UsingDbContext(context =>
        {
            var nationalityId = context.Set<Nationality>().First().Id;
            applicant = context.Set<Applicant>().Add(new Applicant
            {
                TenantId = TenantId,
                Code = "AP" + Guid.NewGuid().ToString("N")[..6],
                FirstNameEn = "A",
                LastNameEn = "B",
                FirstNameAr = "أ",
                LastNameAr = "ب",
                NationalityId = nationalityId,
                BirthDate = new DateTime(1990, 1, 1),
                LegacyPhoto = new byte[] { 3 }
            }).Entity;
            signature = context.Set<EmployeeSignature>().Add(new EmployeeSignature
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                LegacySignature = new byte[] { 4 }
            }).Entity;
        });

        await MoveAsync();

        UsingDbContext(context =>
        {
            var movedApplicant = context.Set<Applicant>().First(a => a.Id == applicant.Id);
            movedApplicant.LegacyPhoto.ShouldBeNull();
            movedApplicant.PhotoPath.ShouldStartWith($"tenant-{TenantId}/applicant-photos/");

            var movedSignature = context.Set<EmployeeSignature>().First(s => s.Id == signature.Id);
            movedSignature.LegacySignature.ShouldBeNull();
            movedSignature.SignaturePath.ShouldStartWith($"tenant-{TenantId}/employee-signatures/");
        });
    }
}
