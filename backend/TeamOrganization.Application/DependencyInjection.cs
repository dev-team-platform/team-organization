using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Interfaces.Services.FileStorages;
using TeamOrganization.Application.Services.FileStorages;
using TeamOrganization.Application.Services.Users;
using TeamOrganization.Application.Interfaces.Services.Exports;
using TeamOrganization.Application.Services.Exports;

namespace TeamOrganization.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<IUserCommandService, UserCommandService>();

        services.AddScoped<IFileStorageCommandService, FileStorageCommandService>();
        services.AddScoped<IFileStorageQueryService, FileStorageQueryService>();

        services.AddScoped<IExportService, ExportService>();

        return services;
    }
}
