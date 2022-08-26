using SharedModels;
using System.Collections.Generic;
using static SupportRequestProcessor.Common.Constants;

namespace SupportRequestProcessor.Common
{
    public class Agent
    {
        public Seniority Seniority { get; set; }
        public int RemainingCapacity { get; set; }
        public Dictionary<string,SupportRequest> SupportRequests { get; set; } = new Dictionary<string, SupportRequest>();
    }
}
