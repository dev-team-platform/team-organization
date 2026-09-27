using System.Text.Json;

namespace TeamOrganization.Domain.Utils;

public static class JsonUtils
{
    public static readonly JsonSerializerOptions WebSerializerOptions = new(JsonSerializerDefaults.Web);
}