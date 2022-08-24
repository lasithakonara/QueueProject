using SharedLibrary;

namespace SupportRequestProcessor.Services
{
    public interface ISupportRequestProcessorService
    {
        ProcessorDetails GetMaxQueueLength();
    }
}