using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharedLibrary;
using SupportRequestProcessor.Common;
using System;

namespace SupportRequestProcessor.Services
{
    public class SupportRequestProcessorService : ISupportRequestProcessorService
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public SupportRequestProcessorService(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger("SupportRequestProcessorService");
        }

        /// <summary>
        /// Flags support reqeusts which should be removed from the system as they have stopped polling and lost connection
        /// The method will either try to remove the currently handling session from the agent or add to a dictionary which
        /// has all the reqeusts that needs to be skipped from processing when chat agents become available
        /// </summary>
        /// <param name="requestId"></param>
        public void FlagSupportRequestForDeletion(string requestId)
        {
            //First we must check whether this support reqeust is currently attended by an agent
            //If so we must end that session making room for a new session
            if (CommonVariables.SupportRequestsAssignments.ContainsKey(requestId))
            {
                if(CommonVariables.SupportRequestsAssignments[requestId].SupportRequests.ContainsKey(requestId))
                {
                    CommonVariables.SupportRequestsAssignments[requestId].SupportRequests.Remove(requestId);
                    CommonVariables.SupportRequestsAssignments[requestId].RemainingCapacity++;
                } 
                CommonVariables.SupportRequestsAssignments.Remove(requestId);
                _logger.LogInformation("Removed currently processing reqeust from agent as polling has been failed : " + requestId);
                return;
            }

            //If we cannot find the Request ID in the SupportRequestsAssignments we must add the request Id to SupportRequestsFlaggedForDeletion dictionary 
            var supportRequestsFlaggedForDeletion = CommonVariables.SupportRequestsFlaggedForDeletion;
            if (!supportRequestsFlaggedForDeletion.ContainsKey(requestId))
            {
                supportRequestsFlaggedForDeletion.Add(requestId, true);
                _logger.LogInformation("Adding reqeust Id to SupportRequestsFlaggedForDeletion : " + requestId);
            }
        }

        /// <summary>
        /// Retrives the maximum queue length allowed in the RabbitMQ queue
        /// SupportRequestAcceptor would need this value to decide whether or not to accept any new suppport requests
        /// </summary>
        /// <returns></returns>
        public ProcessorDetails GetMaxQueueLength()
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
            else if ((currentHourOfDay >= 22 && currentHourOfDay < 24)||(currentHourOfDay >=0 && currentHourOfDay<6)) // Shift 3 - Night Shift (Team C will handle the chat sessions)
            {
                staffSet = "Shift-3-Staff";
            }

            var maxConcurrency = _configuration.GetValue<int>("MaximumConcurrency");

            var numberOfJuniorStaffMembers = _configuration.GetValue<int>(staffSet + ":Junior");
            var numberOfMidLevelStaffMembers = _configuration.GetValue<int>(staffSet + ":MidLevel");
            var numberOfSeniorLevelStaffMembers = _configuration.GetValue<int>(staffSet + ":Senior");
            var numberOfTeamLeadStaffMembers = _configuration.GetValue<int>(staffSet + ":TeamLead");

            //If its not an night shift and if office hours - Shift 1 and 2 (Here we have assumed that the night shift is from 10PM to 6AM)we consider the overflow staff 
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
        }
    }
}
