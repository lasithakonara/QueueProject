using static SharedLibrary.Constants;

namespace SharedLibrary
{
    public class SupportResponse
    {
        public string RequestId { get; set; }
        public SupportReqeustStatus Status { get; set; }
    }
}
