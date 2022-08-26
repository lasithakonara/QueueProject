using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SupportReqeustAcceptor.Common;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SupportReqeustAcceptor.Services
{
    public class SupportRequestMonitor : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        public SupportRequestMonitor(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger("SupportRequestMonitorService");
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        /// <summary>
        /// Monitors the chat sessions and mark them inactive if 3 poll reqeusts are failed
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(true)
            {
                foreach (KeyValuePair<string, DateTime> supportRequest in CommonVariables.SupportRequests)
                {
                    var currentTime = DateTime.UtcNow;
                    var lastPollingTime = supportRequest.Value.AddSeconds(_configuration.GetValue<int>("MaximumAllowedMissedPollingCount"));
                    if (currentTime > lastPollingTime)
                    {
                        _logger.LogError("Found support reqeust for which we did not receive 3 poll requests ");
                        await NotifySupportReqeustProcessorOnNewInactiveSessionAsync(supportRequest.Key);
                    }
                }

                await Task.Delay(500);
            }
        }

        /// <summary>
        /// Notifies Support Reqeust Processor Module on inactive sessions, it will then remove the session 
        /// from the agent or add to a dictionary to flag for delete
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        private async Task NotifySupportReqeustProcessorOnNewInactiveSessionAsync(string requestId)
        {
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(_configuration.GetValue<string>("SupportRequestProsessorUrl") +"api/v1/FlagSupportRequestForDeletion?requestId=" + requestId)
            };

            var httpClient = _httpClientFactory.CreateClient("SupportRequestClient");

            var response = await httpClient.SendAsync(httpRequestMessage);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully notified SupportRequestProcessor on reqeust deletion : " + requestId);
                CommonVariables.SupportRequests.Remove(requestId);
            }
            else
            {
                _logger.LogError("Error while receiving response from SupportRequestProcessor for reqeust deletion : "+ requestId);
            }
        }


    }
}
