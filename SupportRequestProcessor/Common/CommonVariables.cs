using System;
using System.Collections.Generic;

namespace SupportRequestProcessor.Common
{
    public class CommonVariables
    {
        /// <summary>
        /// Used to store support request IDs which could be later used to ignore incoming messages from queue
        /// </summary>
        public static Dictionary<string, bool> SupportRequestsFlaggedForDeletion = new Dictionary<string, bool>();

        /// <summary>
        /// Used to maintain the current chat assignements for each chat agent
        /// </summary>
        public static Dictionary<string, Agent> SupportRequestsAssignments = new Dictionary<string, Agent>();
    }
}
