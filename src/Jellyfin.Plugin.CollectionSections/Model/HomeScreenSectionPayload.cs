using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.CollectionSections.Model
{
    
    
    
    public class HomeScreenSectionPayload
    {
        
        
        
        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }

        
        
        
        
        [JsonPropertyName("additionalData")]
        public string? AdditionalData { get; set; }

        [JsonPropertyName("userConfiguration")]
        public Dictionary<string, object>? UserConfiguration { get; set; }
    }
}
