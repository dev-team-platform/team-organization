using ClosedXML.Excel;
using TeamOrganization.Application.Interfaces.Services.Exports;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Constants.Exports;
using TeamOrganization.Domain.Enums.Users;

namespace TeamOrganization.Infrastructure.Services.ExportJobs;

public class GenerateUserListViewReportService : IGenerateReportsService<GetAllUsersResponseModel>
{
    public Task<Stream> GenerateAsync(
        GetAllUsersResponseModel data,
        TimeZoneInfo timeZoneId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(timeZoneId);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Users");
        var headers = new[]
        {
            "Employee Code",
            "Username",
            "Email",
            "First Name",
            "Last Name",
            "Display Name",
            "Role",
            "Status",
            "Gender",
            "Birth Date",
            "Last Login At",
            "Created At"
        };

        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        var row = 2;
        {
            foreach (var user in data.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                sheet.Cell(row, 1).Value = user.EmployeeCode;
                sheet.Cell(row, 2).Value = user.Username;
                sheet.Cell(row, 3).Value = user.Email;
                sheet.Cell(row, 4).Value = user.FirstName;
                sheet.Cell(row, 5).Value = user.LastName ?? "";
                sheet.Cell(row, 6).Value = user.DisplayName;
                sheet.Cell(row, 7).Value = user.RoleName ?? "";
                sheet.Cell(row, 8).Value = user.Status.ToString();
                sheet.Cell(row, 9).Value = user.Gender.ToString();
                if (user.BirthDate is { } birthDate)
                {
                    sheet.Cell(row, 10).Value = birthDate.ToDateTime(TimeOnly.MinValue);
                    sheet.Cell(row, 10).Style.DateFormat.Format = ExportDateTimeFormats.DateOnly;
                }
                if (user.LastLoginAt is { } lastLoginAt)
                {
                    sheet.Cell(row, 11).Value = TimeZoneInfo.ConvertTime(lastLoginAt, timeZoneId).DateTime;
                    sheet.Cell(row, 11).Style.DateFormat.Format = ExportDateTimeFormats.DateTime;
                }
                sheet.Cell(row, 12).Value = TimeZoneInfo.ConvertTime(user.CreatedAt, timeZoneId).DateTime;
                sheet.Cell(row, 12).Style.DateFormat.Format = ExportDateTimeFormats.DateTime;
                if (user.Status == UserStatus.Inactive)
                {
                    sheet.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor =
                        XLColor.FromHtml(ExportColors.UserInactiveHighlight);
                }
                row++;
            }
        }

        sheet.SheetView.FreezeRows(1);
        var header = sheet.Range(1, 1, 1, headers.Length);
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml(ExportColors.Header);
        var table = sheet.Range(1, 1, row - 1, headers.Length);
        table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        sheet.Columns(1, headers.Length).AdjustToContents();
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return Task.FromResult<Stream>(stream);
    }
}
