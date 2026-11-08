namespace TeamOrganization.Domain.Constants.FileStorages;

public static class FileContentType
{
    // Documents
    public const string Pdf = "application/pdf";
    public const string Word = "application/msword";
    public const string WordX = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public const string Excel = "application/vnd.ms-excel";
    public const string ExcelX = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string PowerPoint = "application/vnd.ms-powerpoint";
    public const string PowerPointX = "application/vnd.openxmlformats-officedocument.presentationml.presentation";

    // Text & Data
    public const string PlainText = "text/plain";
    public const string Csv = "text/csv";
    public const string Json = "application/json";
    public const string Xml = "application/xml";
    public const string Html = "text/html";

    // Images
    public const string Jpeg = "image/jpeg";
    public const string Png = "image/png";
    public const string Gif = "image/gif";
    public const string Webp = "image/webp";
    public const string Svg = "image/svg+xml";
    public const string Avif = "image/avif";
    public const string Ico = "image/x-icon";

    // Archives
    public const string Zip = "application/zip";
    public const string Gzip = "application/gzip";
    public const string SevenZip = "application/x-7z-compressed";
    public const string Rar = "application/vnd.rar";

    // Audio
    public const string Mp3 = "audio/mpeg";
    public const string Wav = "audio/wav";
    public const string Ogg = "audio/ogg";
    public const string Aac = "audio/aac";

    // Video
    public const string Mp4 = "video/mp4";
    public const string Webm = "video/webm";
    public const string Mov = "video/quicktime";

    // Binary
    public const string Binary = "application/octet-stream";
}
