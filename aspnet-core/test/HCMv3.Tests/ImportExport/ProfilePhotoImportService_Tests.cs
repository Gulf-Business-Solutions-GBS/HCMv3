using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.ImportExport;
using HCMv3.SystemSecurity;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ImportExport;

/// <summary>
/// Tests for <see cref="ProfilePhotoImportService"/> -- ported from legacy
/// <c>uscBulkImport.ascx.cs</c>'s <c>UploadData()</c>.
///
/// <para>Focused on the two things that control actually validated (employee exists, image within the
/// configured size limit) and on the fact that a rejected image must not overwrite an existing photo.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class ProfilePhotoImportService_Tests : AppTestBase
{
    private const int TestTenantId = 1;

    private readonly IProfilePhotoImportService _service;

    public ProfilePhotoImportService_Tests()
    {
        _service = Resolve<IProfilePhotoImportService>();
    }

    [Theory]
    // Legacy regex: @".jpg|.jepg|.png|.gif$" -- note "jepg", a typo for "jpeg".
    [InlineData("E001.jpg", true)]
    [InlineData("E001.JPG", true)]
    [InlineData("E001.png", true)]
    [InlineData("E001.gif", true)]
    [InlineData("E001.jepg", true)]
    // Legacy's typo meant a correctly-spelled .jpeg was silently skipped. Accepted here.
    [InlineData("E001.jpeg", true)]
    [InlineData("E001.pdf", false)]
    [InlineData("E001", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsSupportedImage_Should_Match_Legacys_Extension_Set(string fileName, bool expected)
    {
        _service.IsSupportedImage(fileName).ShouldBe(expected);
    }

    [Fact]
    public async Task ImportAsync_Should_Store_The_Photo_Against_The_Employee_Whose_Code_Matches_The_File_Name()
    {
        var employeeId = await CreateEmployeeAsync("PH-001");
        var image = new byte[] { 1, 2, 3, 4 };

        var result = await _service.ImportAsync(new ProfilePhotoImportInput
        {
            TenantId = TestTenantId,
            Files = new List<ProfilePhotoFile>
            {
                new ProfilePhotoFile { FileName = "PH-001.jpg", Content = image }
            }
        });

        result.TotalRecords.ShouldBe(1);
        result.Successfull.ShouldBe(1);
        result.Failed.ShouldBe(0);

        await UsingDbContextAsync(async ctx =>
        {
            var employee = await ctx.Employees.FirstAsync(e => e.Id == employeeId);
            employee.Photo.ShouldBe(image);

            var log = await ctx.BulkUploadLogs.OrderByDescending(l => l.Id).FirstAsync();
            log.LogType.ShouldBe(BulkUploadLogType.ProfilePhoto);
            log.Status.ShouldBe(0);
            log.Message.ShouldBe("Successfull");
            log.EmployeeId.ShouldBe(employeeId);
        });
    }

    [Fact]
    public async Task ImportAsync_Should_Report_An_Unknown_Employee_Code()
    {
        var result = await _service.ImportAsync(new ProfilePhotoImportInput
        {
            TenantId = TestTenantId,
            Files = new List<ProfilePhotoFile>
            {
                new ProfilePhotoFile { FileName = "NO-SUCH-EMPLOYEE.jpg", Content = new byte[] { 1 } }
            }
        });

        result.Failed.ShouldBe(1);

        await UsingDbContextAsync(async ctx =>
        {
            var log = await ctx.BulkUploadLogs.OrderByDescending(l => l.Id).FirstAsync();
            log.Status.ShouldBe(1);
            // Legacy message, verbatim.
            log.Message.ShouldBe("Employee not found for Code : NO-SUCH-EMPLOYEE");
            log.EmployeeId.ShouldBeNull();
        });
    }

    [Fact]
    public async Task ImportAsync_Should_Reject_An_Oversized_Image_Without_Touching_The_Existing_Photo()
    {
        var existing = new byte[] { 9, 9, 9 };
        var employeeId = await CreateEmployeeAsync("PH-BIG", existing);

        // No ManagePersonnelSettings row exists in the test database, so the legacy default applies:
        // maxSize = 100 KB (uscBulkImport.ascx.cs line 186).
        var oversized = new byte[1024 * 101];

        var result = await _service.ImportAsync(new ProfilePhotoImportInput
        {
            TenantId = TestTenantId,
            Files = new List<ProfilePhotoFile>
            {
                new ProfilePhotoFile { FileName = "PH-BIG.jpg", Content = oversized }
            }
        });

        result.Failed.ShouldBe(1);
        result.Successfull.ShouldBe(0);

        await UsingDbContextAsync(async ctx =>
        {
            var employee = await ctx.Employees.FirstAsync(e => e.Id == employeeId);
            // The critical assertion: a rejected image must not clobber what was already there.
            employee.Photo.ShouldBe(existing);

            var log = await ctx.BulkUploadLogs.OrderByDescending(l => l.Id).FirstAsync();
            log.Status.ShouldBe(1);
            log.Message.ShouldBe("Image Size is bigger than allowed limit for Employee Code : PH-BIG");
        });
    }

    [Fact]
    public async Task ImportAsync_Should_Ignore_Files_That_Are_Not_Images()
    {
        await CreateEmployeeAsync("PH-DOC");

        var result = await _service.ImportAsync(new ProfilePhotoImportInput
        {
            TenantId = TestTenantId,
            Files = new List<ProfilePhotoFile>
            {
                new ProfilePhotoFile { FileName = "PH-DOC.pdf", Content = new byte[] { 1 } }
            }
        });

        // Legacy's in-loop regex simply skipped them; they were never counted as failures.
        result.TotalRecords.ShouldBe(0);
        result.Successfull.ShouldBe(0);
        result.Failed.ShouldBe(0);
    }

    private async Task<int> CreateEmployeeAsync(string code, byte[] photo = null)
    {
        return await UsingDbContextAsync(async ctx =>
        {
            // Employee.NationalityId is a NON-nullable FK, so a nationality has to exist first.
            var nationality = new HCMv3.GeneralMasterInfo.Nationality
            {
                TenantId = TestTenantId,
                Code = "NAT-" + code,
                NameEn = "NAT-" + code,
                NameAr = "NAT-" + code
            };

            ctx.Nationalities.Add(nationality);
            await ctx.SaveChangesAsync();

            var employee = new HCMv3.PersonnelAdministration.Employee
            {
                TenantId = TestTenantId,
                NationalityId = nationality.Id,
                Code = code,
                FirstNameEn = code,
                LastNameEn = code,
                FirstNameAr = code,
                LastNameAr = code,
                Photo = photo
            };

            ctx.Employees.Add(employee);
            await ctx.SaveChangesAsync();
            return employee.Id;
        });
    }
}
