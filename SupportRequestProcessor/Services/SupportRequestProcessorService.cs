using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharedLibrary;
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
        }
    }
}
