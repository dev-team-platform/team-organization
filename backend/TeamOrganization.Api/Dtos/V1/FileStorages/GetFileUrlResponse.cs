using TeamOrganization.Application.Models.FileStorages;

namespace TeamOrganization.Api.Dtos.V1.FileStorages;

public class GetFileUrlResponse
{
    public string Url { get; set; } = null!;

    public static GetFileUrlResponse FromModel(GetFileUrlResponseModel model) => new()
    {
        Url = model.Url
    };
}
