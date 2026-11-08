using TeamOrganization.Application.Models.Exports;

namespace TeamOrganization.Api.Dtos.V1.Exports;

public class ExportResponse
{
    public string FileName { get; init; } = null!;

    public static ExportResponse FromModel(ExportResponseModel response)
    {
        return new ExportResponse
        {
            FileName = response.FileName
        };
    }
}