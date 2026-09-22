using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.Storage;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Storage;

/// <summary>
/// The screens still send and receive photos and signatures as bytes in their DTOs; only where they are kept
/// changed. These tests go through the real services to prove that round trip: saved to disk, never to the
/// database, read back unchanged.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EntityImageRoundTrip_Tests : AppTestBase
{
    private readonly IEntityImageStore _images;

    public EntityImageRoundTrip_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _images = Resolve<IEntityImageStore>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateEmployee()
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

            var code = "RT" + Guid.NewGuid().ToString("N")[..6];
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
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    private Employee LoadEmployee(int id)
    {
        Employee employee = null;
        UsingDbContext(context => employee = context.Set<Employee>().First(e => e.Id == id));

        return employee;
    }

    // ---------------------------------------------------------------- employee photo

    [Fact]
    public async Task Saving_A_Photo_Should_Put_It_On_Disk_And_Read_It_Back()
    {
        var service = Resolve<IEmployeesAppService>();
        var id = CreateEmployee();

        var dto = (await service.GetEmployeeForEdit(new EntityDto(id))).Employee;
        dto.Photo = new byte[] { 1, 2 };
        await service.CreateOrEdit(dto);

        var employee = LoadEmployee(id);
        employee.PhotoPath.ShouldStartWith($"tenant-{TenantId}/employee-photos/{id}-");
        (await _images.ReadOrNullAsync(TenantId, employee.PhotoPath)).ShouldBe(new byte[] { 1, 2 });

        // And the screen gets it back as bytes, exactly as before the move.
        var reloaded = (await service.GetEmployeeForEdit(new EntityDto(id))).Employee;
        reloaded.Photo.ShouldBe(new byte[] { 1, 2 });
    }

    [Fact]
    public async Task Saving_Again_Unchanged_Should_Keep_The_Same_File()
    {
        var service = Resolve<IEmployeesAppService>();
        var id = CreateEmployee();
        var seeded = (await service.GetEmployeeForEdit(new EntityDto(id))).Employee;
        seeded.Photo = new byte[] { 3 };
        await service.CreateOrEdit(seeded);
        var firstPath = LoadEmployee(id).PhotoPath;

        await service.CreateOrEdit((await service.GetEmployeeForEdit(new EntityDto(id))).Employee);

        LoadEmployee(id).PhotoPath.ShouldBe(firstPath);
    }

    [Fact]
    public async Task Clearing_The_Photo_Should_Delete_The_File()
    {
        var service = Resolve<IEmployeesAppService>();
        var id = CreateEmployee();
        var seeded = (await service.GetEmployeeForEdit(new EntityDto(id))).Employee;
        seeded.Photo = new byte[] { 4 };
        await service.CreateOrEdit(seeded);
        var path = LoadEmployee(id).PhotoPath;

        var dto = (await service.GetEmployeeForEdit(new EntityDto(id))).Employee;
        dto.Photo = null;
        await service.CreateOrEdit(dto);

        LoadEmployee(id).PhotoPath.ShouldBeNull();
        (await _images.ReadOrNullAsync(TenantId, path)).ShouldBeNull();
    }

    // ---------------------------------------------------------------- signature

    [Fact]
    public async Task A_Signature_Should_Be_Stored_On_Disk_And_Read_Back()
    {
        var service = Resolve<IEmployeeSignaturesAppService>();
        var employeeId = CreateEmployee();

        await service.CreateOrEdit(new CreateOrEditEmployeeSignatureDto { EmployeeId = employeeId, Signature = new byte[] { 9, 8 } });

        EmployeeSignature row = null;
        UsingDbContext(context => row = context.Set<EmployeeSignature>().First(s => s.EmployeeId == employeeId));
        row.SignaturePath.ShouldStartWith($"tenant-{TenantId}/employee-signatures/");

        var list = await service.GetAll(new GetAllEmployeeSignaturesInput { EmployeeId = employeeId, MaxResultCount = 10 });
        list.Items.Single().Signature.ShouldBe(new byte[] { 9, 8 });

        var edit = await service.GetEmployeeSignatureForEdit(new EntityDto(row.Id));
        edit.EmployeeSignature.Signature.ShouldBe(new byte[] { 9, 8 });
    }
}
