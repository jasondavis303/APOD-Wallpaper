using System.Text.Json.Serialization;

namespace APOD_Wallpaper;

internal class APODResponse
{
    //[JsonPropertyName("copyright")]
    //public string Copyright { get; set; }

    //[JsonPropertyName("date")]
    //public DateTimeOffset Date { get; set; }

    //[JsonPropertyName("explanation")]
    //public string Explanation { get; set; }

    [JsonPropertyName("hdurl")]
    public string Hdurl { get; set; }

    //[JsonPropertyName("media_type")]
    //public string MediaType { get; set; }

    //[JsonPropertyName("service_version")]
    //public string ServiceVersion { get; set; }

    //[JsonPropertyName("title")]
    //public string Title { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}