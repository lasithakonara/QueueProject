using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharedLibrary;
using SharedModels;
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
        public SupportRequestService(IMessagePublisher messageProducer, ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
        {
            _messagePublisher = messageProducer;
            _logger = loggerFactory.CreateLogger("SupportRequestService");
            _httpClientFactory = httpClientFactory;
        }

        public async Task<SupportResponse> AddSupportReqeustToQueue(SupportRequest supportRequest)
        {
            _logger.LogInformation("New incoming support request : " + JsonConvert.SerializeObject(supportRequest));
            var currentQueueSize = _messagePublisher.GetMessageCount("sessions");
             _logger.LogInformation("Queue length : " + currentQueueSize);

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://localhost:44366/api/Chat/api/v1/maxQueueLength")
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
                supportResponse.Status = Constants.SupportReqeustStatus.OK;
            }
            else
            {
                supportResponse.Status = Constants.SupportReqeustStatus.NOK;
            }

            return supportResponse;
        }
    }
}
