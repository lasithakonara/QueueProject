using SharedLibrary;

namespace SupportRequestProcessor.Services
{
    public interface ISupportRequestProcessorService
    {
        /// <summary>
        /// Retrives the maximum queue length allowed in the RabbitMQ queue
        /// SupportRequestAcceptor would need this value to decide whether or not to accept any new suppport requests
        /// </summary>
        /// <returns></returns>
        ProcessorDetails GetMaxQueueLength();

        /// <summary>
        /// Flags support reqeusts which should be removed from the system as they have stopped polling and lost connection
        /// The method will either try to remove the currently handling session from the agent or add to a dictionary which
        /// has all the reqeusts that needs to be skipped from processing when chat agents become available
        /// </summary>
        /// <param name="requestId"></param>
        void FlagSupportRequestForDeletion(string requestId);
    }
}