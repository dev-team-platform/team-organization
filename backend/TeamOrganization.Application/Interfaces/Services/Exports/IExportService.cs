using TeamOrganization.Application.Models.Exports;
using TeamOrganization.Application.Models.Users;

namespace TeamOrganization.Application.Interfaces.Services.Exports;

public interface IExportService
{
    Task<ExportResponseModel> ExportUsersAsync(GetAllUsersRequestModel model, CancellationToken cancellationToken = default);
}
