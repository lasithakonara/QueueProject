using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SharedLibrary;
using SharedModels;
using SupportReqeustAcceptor.Services;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SupportReqeustAcceptor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupportRequestController : ControllerBase
    {
        private readonly ISupportRequestService _supportRequestService;
        private readonly ILogger _logger;
        public SupportRequestController(ISupportRequestService supportRequestService, ILoggerFactory loggerFactory)
        {
            _supportRequestService = supportRequestService;
            _logger = loggerFactory.CreateLogger("SupportRequestController");
        }

        // POST api/<SupportRequestController>
        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] SupportRequest supportRequest)
        {
            SupportResponse supportResponse = await _supportRequestService.AddSupportReqeustToQueue(supportRequest);

            return Ok(supportResponse);
        }

    }
}
