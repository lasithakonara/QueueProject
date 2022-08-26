using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharedLibrary;
using SharedModels;
using SupportReqeustAcceptor.Common;
using SupportReqeustAcceptor.RabbitMQ;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SupportReqeustAcceptor.Services
{
    public class SupportRequestService : ISupportRequestService
    {
        private readonly IMessagePublisher _messagePublisher;
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public SupportRequestService(IMessagePublisher messageProducer, ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _messagePublisher = messageProducer;
            _logger = loggerFactory.CreateLogger("SupportRequestService");
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        /// <summary>
        /// Adds new support reqeusts to queue
        /// </summary>
        /// <param name="supportRequest"></param>
        /// <returns></returns>
        public async Task<SupportResponse> AddSupportReqeustToQueue(SupportRequest supportRequest)
        {
            _logger.LogInformation("New incoming support request : " + JsonConvert.SerializeObject(supportRequest));
            var currentQueueSize = _messagePublisher.GetMessageCount("sessions");
             _logger.LogInformation("Queue length : " + currentQueueSize);

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(_configuration.GetValue<string>("SupportRequestProsessorUrl")+"api/v1/MaxQueueLength")
            };

            var httpClient = _httpClientFactory.CreateClient("SupportRequestClient");

            var response = await httpClient.SendAsync(httpRequestMessage);
            ProcessorDetails processorDetails = null;
            if (response.IsSuccessStatusCode)
            {
                var readAsStringAsync = await response.Content.ReadAsStringAsync();

                processorDetails = JsonConvert.DeserializeObject<ProcessorDetails>(readAsStringAsync);
            }
            else
            {
                _logger.LogError("Error while receiving response from Support Request Processor");
            }

            SupportResponse supportResponse = new SupportResponse();

            if (currentQueueSize < processorDetails.QueueCapacity)
            {
                //Generating a unique key to reference later
                var reqeustId = Guid.NewGuid().ToString();
                supportRequest.RequestId = reqeustId;

                _messagePublisher.SendMessage(supportRequest);

                supportResponse.RequestId = reqeustId;
                supportResponse.Status = Constants.SUPPORT_REQUEST_STATUS_OK;

                CommonVariables.SupportRequests.Add(reqeustId, DateTime.UtcNow);
            }
            else
            {
                _logger.LogError("Cannot add more support reqeusts to the session queue as the queue is full!");
                supportResponse.Status = Constants.SUPPORT_REQUEST_STATUS_NOK;
            }

            return supportResponse;
        }

        /// <summary>
        /// Update poll requests against each chat session, this will later be used by another 
        /// background process to mark them as inactive if 3 polling reqeusts failes
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns>Operation success or false status</returns>
        public bool UpdatePollRequestAgainstSupportRequest(string requestId)
        {
            
            if(CommonVariables.SupportRequests.ContainsKey(requestId))
            {
                CommonVariables.SupportRequests[requestId] = DateTime.UtcNow;
                _logger.LogInformation("Updating polling request for support request ID : "+ requestId + " TimeStamp : "+ CommonVariables.SupportRequests[requestId]);
                return true;
            }
            else
            {
                _logger.LogError("No such request ID : " + requestId + " found");
                return false;
            }
        }
    }
}
