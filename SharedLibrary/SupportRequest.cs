
using System.Text.Json.Serialization;

namespace SharedModels
{
    public class SupportRequest
    {
        [JsonIgnore]
        public string RequestId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EMail { get; set; }
        public string ContactNumber { get; set; }

        [JsonIgnore]
        public bool IsActive { get; set; }
    }
}
