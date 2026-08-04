using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Services;

namespace TeamOrganization.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<IUserCommandService, UserCommandService>();

        return services;
    }
}
