using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SharedLibrary;
using SupportRequestProcessor.Services;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SupportRequestProcessor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {

        private readonly ILogger _logger;
        private readonly ISupportRequestProcessorService _supportRequestProcessorService;

        public ChatController(ISupportRequestProcessorService supportRequestProcessorService, ILoggerFactory loggerFactory)
        {
            _supportRequestProcessorService = supportRequestProcessorService;
            _logger = loggerFactory.CreateLogger("ChatController");
        }

        // GET: api/<ChatController>
        [HttpGet, Route("api/v1/maxQueueLength")]
        public ProcessorDetails GetMaxQueueLength()
        {
            ProcessorDetails processorDetails = _supportRequestProcessorService.GetMaxQueueLength();
            _logger.LogInformation("Returning chat session capacity : "+ processorDetails.QueueCapacity);
            return processorDetails;
        }

       
    }
}
