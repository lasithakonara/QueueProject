using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using SharedLibrary;
using SharedModels;
using SupportRequestProcessor.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SupportRequestProcessor.Common.Constants;

namespace SupportRequestProcessor.Services
{
    public class AgentChatCoordinatorService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        Dictionary<int, Agent>  JuniorAgents = new Dictionary<int, Agent>();
        Dictionary<int, Agent> MidLevelAgents = new Dictionary<int, Agent>();
        Dictionary<int, Agent> SeniorAgents = new Dictionary<int, Agent>();
        Dictionary<int, Agent> TeamLeadAgents = new Dictionary<int, Agent>();

        int nextRoundRobinJuniorAgent = 0;
        int nextRoundRobinMidLevelAgent = 0;
        int nextRoundRobinSeniorAgent = 0;
        int nextRoundRobinTeamLeadAgent = 0;

        string currentShift = String.Empty;
        int remainingChatCapacity = 0;

        public AgentChatCoordinatorService(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger("AgentChatCoordinatorService");
        }

        public string GetCurrentShift()
        {
            string latestShift = String.Empty;
            var currentHourOfDay = DateTime.Now.Hour;

            if (currentHourOfDay >= 6 && currentHourOfDay < 14) // Shift 1 (Team A will handle the chat sessions)
            {
                latestShift = SHIFT_1;
            }
            else if (currentHourOfDay >= 14 && currentHourOfDay < 22) // Shift 2 (Team B will handle the chat sessions)
            {
                latestShift = SHIFT_2;
            }
            else if ((currentHourOfDay >= 22 && currentHourOfDay < 24) || (currentHourOfDay >= 0 && currentHourOfDay < 6)) // Shift 3 - Night Shift (Team C will handle the chat sessions)
            {
                latestShift = SHIFT_3;
            }

            return latestShift;
        }

        /*public ProcessorDetails GetMaxQueueLength()
        {
            var currentHourOfDay = DateTime.Now.Hour;

            var efficiencyOfJunior = _configuration.GetValue<double>("StaffEfficiency:Junior");
            var efficiencyOfMidLevel = _configuration.GetValue<double>("StaffEfficiency:MidLevel");
            var efficiencyOfSenior = _configuration.GetValue<double>("StaffEfficiency:Senior");
            var efficiencyOfTeamLead = _configuration.GetValue<double>("StaffEfficiency:TeamLead");

            string staffSet = String.Empty;

            if (currentHourOfDay >= 6 && currentHourOfDay < 14) // Shift 1 (Team A will handle the chat sessions)
            {
                staffSet = "Shift-1-Staff";
            }
            else if (currentHourOfDay >= 14 && currentHourOfDay < 22) // Shift 2 (Team B will handle the chat sessions)
            {
                staffSet = "Shift-2-Staff";
            }
            else if (currentHourOfDay >= 22 && currentHourOfDay < 6) // Shift 3 - Night Shift (Team C will handle the chat sessions)
            {
                staffSet = "Shift-3-Staff";
            }

            var maxConcurrency = _configuration.GetValue<int>("MaximumConcurrency");

            var numberOfJuniorStaffMembers = _configuration.GetValue<int>(staffSet + ":Junior");
            var numberOfMidLevelStaffMembers = _configuration.GetValue<int>(staffSet + ":MidLevel");
            var numberOfSeniorLevelStaffMembers = _configuration.GetValue<int>(staffSet + ":Senior");
            var numberOfTeamLeadStaffMembers = _configuration.GetValue<int>(staffSet + ":TeamLead");

            //If its not an night shift (Here we have assumed that the night shift is from 10PM to 6AM)we consider the overflow staff 
            // when calculating the capacity
            if (currentHourOfDay >= 6 && currentHourOfDay < 22)
            {
                // Overflow staff has the experience level of Junior staff
                numberOfJuniorStaffMembers += _configuration.GetValue<int>("Overflow-Staff:Junior");
            }

            int chatSessionCapacity = (int)((Math.Floor(numberOfJuniorStaffMembers * efficiencyOfJunior * maxConcurrency)
                                    + Math.Floor(numberOfMidLevelStaffMembers * efficiencyOfMidLevel * maxConcurrency)
                                    + Math.Floor(numberOfSeniorLevelStaffMembers * efficiencyOfSenior * maxConcurrency)
                                    + Math.Floor(numberOfTeamLeadStaffMembers * efficiencyOfTeamLead * maxConcurrency)));

            double mulitiplierFactorForMaximumQueueLength = _configuration.GetValue<double>("MulitiplierFactorForMaximumQueueLength");

            ProcessorDetails processorDetails = new ProcessorDetails();

            processorDetails.QueueCapacity = (int)(chatSessionCapacity * mulitiplierFactorForMaximumQueueLength);

            _logger.LogInformation("Returning session capacity " + processorDetails.QueueCapacity);

            return processorDetails;
        }*/

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //await ListenForIntegrationEvents(stoppingToken);

            await CoordinateChatsWithAgents();
        }

       /* private async Task ListenForIntegrationEvents(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ListenForIntegrationEvents ");
            var factory = new ConnectionFactory
            {
                HostName = "localhost"
            };
            var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDeclare("sessions", exclusive: false);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation("New message received: " + message);

                //channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
            };
                     

            channel.BasicConsume(queue: "sessions", autoAck: true, consumer: consumer);

            Console.ReadLine();

        }*/

        private async Task CoordinateChatsWithAgents()
        {
            _logger.LogInformation("ListenForIntegrationEvents ");
            var factory = new ConnectionFactory
            {
                HostName = "localhost"
            };
            var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDeclare("sessions", exclusive: false);

            while (true)
            {

                if (!EvaluateWhetherChatAgentsAvailbleToHandleTheSupportRequest())
                {
                    _logger.LogInformation("New support request cannot be accomodated as all agents are busy");
                    await Task.Delay(1000);
                    continue;
                }

                bool autoAck = false;
                BasicGetResult result = channel.BasicGet("sessions", autoAck);
                if (result == null)
                {
                    _logger.LogInformation("No Message this time ");
                }
                else
                {
                    IBasicProperties props = result.BasicProperties;
                    ReadOnlyMemory<byte> body = result.Body;

                    var message = Encoding.UTF8.GetString(body.Span);

                    _logger.LogInformation("New message received: " + message);

                    SupportRequest supportRequest = JsonConvert.DeserializeObject<SupportRequest>(message);

                    AssignSupportRequestToChatAgent(supportRequest);

                    // acknowledge receipt of the message
                    channel.BasicAck(result.DeliveryTag, false);
                }

                await Task.Delay(1000);
            }
        }

        /// <summary>
        /// Helps to assign each support chat requests to different agents
        /// Below priority is maintained when to ensures that the higher seniority are more available to assist the lower
        /// Prioirty 1 - Junior agents
        /// Prioirty 2 - Mid-Level agents
        /// Prioirty 3 - Senior agents
        /// Prioirty 4 - Team lead agents
        /// </summary>
        /// <param name="supportRequest"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void AssignSupportRequestToChatAgent(SupportRequest supportRequest)
        {
            //Try to assign the support request to Junior Agent
            for (int i=0;i< JuniorAgents.Count; i++)
            {
                if (JuniorAgents.TryGetValue(nextRoundRobinJuniorAgent, out var agent))
                {
                    if(agent.RemainingCapacity > 0)
                    {
                        agent.SupportRequests.Add(supportRequest);
                        agent.RemainingCapacity -= 1;
                        _logger.LogInformation("New support request "+ supportRequest.RequestId + " assigned to Junior Agent at position : " + nextRoundRobinJuniorAgent + " Remaining chat cpaccity for the agent : "+ agent.RemainingCapacity);
                        remainingChatCapacity--;
                        if (nextRoundRobinJuniorAgent < JuniorAgents.Count - 1)
                        {
                            nextRoundRobinJuniorAgent++;
                        }
                        else
                        {
                            nextRoundRobinJuniorAgent = 0;
                        }
                        return;
                    }
                    else
                    {
                        if(nextRoundRobinJuniorAgent < JuniorAgents.Count-1)
                        {
                            nextRoundRobinJuniorAgent++;
                        }
                        else
                        {
                            nextRoundRobinJuniorAgent = 0;
                        }
                    }
                }
            }

            //Try to assign the support request to Mid-Level Agent
            for (int i = 0; i < MidLevelAgents.Count; i++)
            {
                if (MidLevelAgents.TryGetValue(nextRoundRobinMidLevelAgent, out var agent))
                {
                    if (agent.RemainingCapacity > 0)
                    {
                        agent.SupportRequests.Add(supportRequest);
                        agent.RemainingCapacity -= 1;
                        _logger.LogInformation("New support request " + supportRequest.RequestId + " assigned to Mid-Level Agent at position : " + nextRoundRobinMidLevelAgent + " Remaining chat cpaccity for the agent : " + agent.RemainingCapacity);
                        remainingChatCapacity--;
                        if (nextRoundRobinMidLevelAgent < MidLevelAgents.Count - 1)
                        {
                            nextRoundRobinMidLevelAgent++;
                        }
                        else
                        {
                            nextRoundRobinMidLevelAgent = 0;
                        }
                        return;
                    }
                    else
                    {
                        if (nextRoundRobinMidLevelAgent < MidLevelAgents.Count - 1)
                        {
                            nextRoundRobinMidLevelAgent++;
                        }
                        else
                        {
                            nextRoundRobinMidLevelAgent = 0;
                        }
                    }
                }
            }

            //Try to assign the support request to Senior Agent
            for (int i = 0; i < SeniorAgents.Count; i++)
            {
                if (SeniorAgents.TryGetValue(nextRoundRobinSeniorAgent, out var agent))
                {
                    if (agent.RemainingCapacity > 0)
                    {
                        agent.SupportRequests.Add(supportRequest);
                        agent.RemainingCapacity -= 1;
                        _logger.LogInformation("New support request " + supportRequest.RequestId + " assigned to Senior Agent at position : " + nextRoundRobinSeniorAgent + " Remaining chat cpaccity for the agent : " + agent.RemainingCapacity);
                        remainingChatCapacity--;
                        if (nextRoundRobinSeniorAgent < SeniorAgents.Count - 1)
                        {
                            nextRoundRobinSeniorAgent++;
                        }
                        else
                        {
                            nextRoundRobinSeniorAgent = 0;
                        }
                        return;
                    }
                    else
                    {
                        if (nextRoundRobinSeniorAgent < SeniorAgents.Count - 1)
                        {
                            nextRoundRobinSeniorAgent++;
                        }
                        else
                        {
                            nextRoundRobinSeniorAgent = 0;
                        }
                    }
                }
            }

            //Try to assign the support request to Lead Agent
            for (int i = 0; i < TeamLeadAgents.Count; i++)
            {
                if (TeamLeadAgents.TryGetValue(nextRoundRobinTeamLeadAgent, out var agent))
                {
                    if (agent.RemainingCapacity > 0)
                    {
                        agent.SupportRequests.Add(supportRequest);
                        agent.RemainingCapacity -= 1;
                        _logger.LogInformation("New support request " + supportRequest.RequestId + " assigned to Lead Agent at position : " + nextRoundRobinTeamLeadAgent + " Remaining chat cpaccity for the agent : " + agent.RemainingCapacity);
                        remainingChatCapacity--;
                        if (nextRoundRobinTeamLeadAgent < TeamLeadAgents.Count - 1)
                        {
                            nextRoundRobinTeamLeadAgent++;
                        }
                        else
                        {
                            nextRoundRobinTeamLeadAgent = 0;
                        }
                        return;
                    }
                    else
                    {
                        if (nextRoundRobinTeamLeadAgent < TeamLeadAgents.Count - 1)
                        {
                            nextRoundRobinTeamLeadAgent++;
                        }
                        else
                        {
                            nextRoundRobinTeamLeadAgent = 0;
                        }
                    }
                }
            }


        }

        /// <summary>
        /// Evaluates the chat agent availability befoe accpeting each support reqeust
        /// This method needs to evaluate each time becasue the current shift may end now
        /// In which case we have to re-evaluate the capacity.
        /// When a shift ends, the existing crew will process the current chat sessions and quit
        /// No more addditional chats are assigned to them.
        /// </summary>
        /// <returns></returns>
        private bool EvaluateWhetherChatAgentsAvailbleToHandleTheSupportRequest()
        {
            string latestShift = GetCurrentShift();

            if(!String.Equals(latestShift,currentShift))
            {
                currentShift = latestShift;
                remainingChatCapacity = 0;

                nextRoundRobinJuniorAgent = 0;
                nextRoundRobinMidLevelAgent = 0;
                nextRoundRobinSeniorAgent = 0;
                nextRoundRobinTeamLeadAgent = 0;

                ReadConfigFIleAndRearrangeTeamCompositionAndCapacity();
            }

            if(remainingChatCapacity > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// When a shift changes this method helps to recalculate the capacities.
        /// </summary>
        private void ReadConfigFIleAndRearrangeTeamCompositionAndCapacity()
        {
            var numberOfJuniorStaffMembers = _configuration.GetValue<int>(currentShift + ":Junior");
            var numberOfMidLevelStaffMembers = _configuration.GetValue<int>(currentShift + ":MidLevel");
            var numberOfSeniorLevelStaffMembers = _configuration.GetValue<int>(currentShift + ":Senior");
            var numberOfTeamLeadStaffMembers = _configuration.GetValue<int>(currentShift + ":TeamLead");

            var efficiencyOfJunior = _configuration.GetValue<double>("StaffEfficiency:Junior");
            var efficiencyOfMidLevel = _configuration.GetValue<double>("StaffEfficiency:MidLevel");
            var efficiencyOfSenior = _configuration.GetValue<double>("StaffEfficiency:Senior");
            var efficiencyOfTeamLead = _configuration.GetValue<double>("StaffEfficiency:TeamLead");

            var maxConcurrency = _configuration.GetValue<int>("MaximumConcurrency");

            for (int i = 0; i < numberOfJuniorStaffMembers; i++)
            {
                var chatCapacity = (int)Math.Floor(efficiencyOfJunior * maxConcurrency);
                Agent agent = new Agent()
                {
                    Seniority = Seniority.Junior,
                    RemainingCapacity = chatCapacity
                };

                JuniorAgents.Add(i,agent);
                remainingChatCapacity += chatCapacity;
            }

            for (int i = 0; i < numberOfMidLevelStaffMembers; i++)
            {
                var chatCapacity = (int)Math.Floor(efficiencyOfMidLevel * maxConcurrency);
                Agent agent = new Agent()
                {
                    Seniority = Seniority.Junior,
                    RemainingCapacity = chatCapacity
                };

                MidLevelAgents.Add(i,agent);
                remainingChatCapacity += chatCapacity;
            }

            for (int i = 0; i < numberOfSeniorLevelStaffMembers; i++)
            {
                var chatCapacity = (int)Math.Floor(efficiencyOfSenior * maxConcurrency);
                Agent agent = new Agent()
                {
                    Seniority = Seniority.Junior,
                    RemainingCapacity = chatCapacity
                };

                SeniorAgents.Add(i,agent);
                remainingChatCapacity += chatCapacity;
            }

            for (int i = 0; i < numberOfTeamLeadStaffMembers; i++)
            {
                var chatCapacity = (int)Math.Floor(efficiencyOfTeamLead * maxConcurrency);
                Agent agent = new Agent()
                {
                    Seniority = Seniority.Junior,
                    RemainingCapacity = chatCapacity
                };

                TeamLeadAgents.Add(i,agent);
                remainingChatCapacity += chatCapacity;
            }

        }
    }
}
