using SharedLibrary;
using SharedModels;
using System.Threading.Tasks;

namespace SupportReqeustAcceptor.Services
{
    public interface ISupportRequestService
    {
        Task<SupportResponse> AddSupportReqeustToQueue(SupportRequest supportRequest);
    }
}
