using SharedLibrary;
using SharedModels;
using System.Threading.Tasks;

namespace SupportReqeustAcceptor.Services
{
    public interface ISupportRequestService
    {
        /// <summary>
        /// Adds new support reqeusts to queue
        /// </summary>
        /// <param name="supportRequest"></param>
        /// <returns></returns>
        Task<SupportResponse> AddSupportReqeustToQueue(SupportRequest supportRequest);

        /// <summary>
        /// Update poll requests against each chat session, this will later be used by another 
        /// background process to mark them as inactive if 3 polling reqeusts failes
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns>Operation success or false status</returns>
        bool UpdatePollRequestAgainstSupportRequest(string requestId);
    }
}
