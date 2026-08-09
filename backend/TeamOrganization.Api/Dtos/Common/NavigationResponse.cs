using TeamOrganization.Application.Models.Common;

namespace TeamOrganization.Api.Dtos.Common;

public class NavigationResponse
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;

    public static NavigationResponse FromModel(NavigationResponseModel model)
    {
        return new NavigationResponse
        {
            Id = model.Id,
            Name = model.Name
        };
    }
}