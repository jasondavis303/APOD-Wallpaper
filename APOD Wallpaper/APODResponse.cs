using System.Text.Json.Serialization;

namespace APOD_Wallpaper;

internal class APODResponse
{
    [JsonPropertyName("hdurl")]
    public string Hdurl { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}