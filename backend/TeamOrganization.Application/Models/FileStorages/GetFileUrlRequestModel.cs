namespace TeamOrganization.Application.Models.FileStorages;

public class GetFileUrlRequestModel
{
    public Guid? FileId { get; init; }
    public string? ReferenceId { get; init; }
}
