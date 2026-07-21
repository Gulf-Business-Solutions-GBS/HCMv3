using System;
using System.Collections.Generic;
using System.Dynamic;
using Abp.Localization;
using HCMv3.Authorization.Users.Importing.Dto;
using System.Linq;
using System.Threading.Tasks;
using Abp.Collections.Extensions;
using HCMv3.DataExporting.Excel.MiniExcel;
using HCMv3.DataImporting.Excel;

namespace HCMv3.Authorization.Users.Importing;

public class UserListExcelDataReader(ILocalizationManager localizationManager)
    : MiniExcelExcelImporterBase<ImportUserDto>(localizationManager), IExcelDataReader<ImportUserDto>
{
    public async Task<List<ImportUserDto>> GetEntitiesFromExcel(byte[] fileBytes)
    {
        return await ProcessExcelFile(fileBytes, ProcessExcelRow);
    }

    private ImportUserDto ProcessExcelRow(dynamic row)
    {
        if (IsRowEmpty(row))
        {
            return null;
        }

        var user = new ImportUserDto();

        try
        {
            user.UserName = GetRequiredValueFromRowOrNull(row, nameof(user.UserName));
            user.Name = GetRequiredValueFromRowOrNull(row, nameof(user.Name));
            user.Surname = GetRequiredValueFromRowOrNull(row, nameof(user.Surname));
            user.EmailAddress = GetRequiredValueFromRowOrNull(row, nameof(user.EmailAddress));
            user.PhoneNumber = GetOptionalValueFromRowOrNull<string>(row, nameof(user.PhoneNumber));
            user.Password = GetRequiredValueFromRowOrNull(row, nameof(user.Password));
            user.Roles = GetAssignedRoleNamesFromRow(row);
        }
        catch (Exception exception)
        {
            user.Exception = exception.Message;
        }

        return user;
    }

    private string[] GetAssignedRoleNamesFromRow(dynamic row)
    {
        var cellValue = (row as ExpandoObject).GetOrDefault(nameof(ImportUserDto.Roles))?.ToString();
        if (cellValue == null || string.IsNullOrWhiteSpace(cellValue))
        {
            return Array.Empty<string>();
        }

        var roles = cellValue.Split(',');
        return roles.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim())
            .ToArray();
    }

    private bool IsRowEmpty(dynamic row)
    {
        var username = (row as ExpandoObject).GetOrDefault(nameof(User.UserName))?.ToString();
        return string.IsNullOrWhiteSpace(username);
    }
}