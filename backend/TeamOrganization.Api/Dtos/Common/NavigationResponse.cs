using TeamOrganization.Application.Models.Common;

namespace TeamOrganization.Api.Dtos.Common;

public class NavigationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    public static NavigationResponse? FromModel(NavigationResponseModel? model)
    {
        return model == null ? null : new NavigationResponse
        {
            Id = model.Id,
            Name = model.Name
        };
    }
}