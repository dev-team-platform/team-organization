using System.ComponentModel.DataAnnotations;
using TeamOrganization.Application.Models.FileStorages;

namespace TeamOrganization.Api.Dtos.V1.FileStorages;

public class GetFileUrlRequest : IValidatableObject
{
    public Guid? FileId { get; init; }
    public string? ReferenceId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((!FileId.HasValue || FileId == Guid.Empty) && string.IsNullOrWhiteSpace(ReferenceId))
        {
            yield return new ValidationResult("Provide a file ID or reference ID.", [nameof(FileId), nameof(ReferenceId)]);
        }
    }

    public static GetFileUrlRequestModel ToModel(GetFileUrlRequest request)
    {
        return new GetFileUrlRequestModel
        {
            FileId = request.FileId,
            ReferenceId = request.ReferenceId
        };
    }
}