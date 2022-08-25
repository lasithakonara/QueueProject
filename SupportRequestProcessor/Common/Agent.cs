using SharedModels;
using System.Collections.Generic;
using static SupportRequestProcessor.Common.Constants;

namespace SupportRequestProcessor.Common
{
    public class Agent
    {
        public Seniority Seniority { get; set; }
        public int RemainingCapacity { get; set; }
        public List<SupportRequest> SupportRequests { get; set; } = new List<SupportRequest>();
    }
}
