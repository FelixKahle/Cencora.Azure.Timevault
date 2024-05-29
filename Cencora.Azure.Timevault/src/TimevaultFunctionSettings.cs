// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

namespace Cencora.Azure.Timevault
{
    /// <summary>
    /// Represents the settings for the Timevault function.
    /// </summary>
    public class TimevaultFunctionSettings
    {
        /// <summary>
        /// Gets or sets the name of the Timevault Cosmos DB database.
        /// This is the primary database within Cosmos DB where Timevault-related data is stored.
        /// The database name is required to establish a connection and perform operations against the Cosmos DB.
        /// </summary>
        public required string TimevaultCosmosDBDatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the name of the Timevault Cosmos DB container.
        /// Containers within a Cosmos DB database hold the data items. Each container maps to a set of related data,
        /// often reflecting a specific entity or aggregation of data, like documents or items, required by Timevault.
        /// </summary>
        public required string TimevaultCosmosDBContainerName { get; set; }

        /// <summary>
        /// Represents the interval in minutes for updating the IANA (Internet Assigned Numbers Authority) time zone codes.
        /// This interval defines how frequently the application checks for and applies updates to IANA codes,
        /// ensuring time zone information remains accurate and up-to-date.
        /// </summary>
        public required int IanaCodeUpdateIntervalInMinutes { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of concurrent Cosmos DB requests.
        /// This setting limits the number of concurrent requests made to the Cosmos DB service to prevent overloading
        /// and potential throttling by the service. It's applied at the task level, meaning each task (e.g., a query execution
        /// or document upsert) can make up to this number of concurrent requests, but the limit isn't shared across multiple tasks.
        /// 
        /// Example: If the limit is set to 10, two running tasks can each make up to 10 concurrent requests to Cosmos DB,
        /// potentially resulting in a total of 20 concurrent requests from the application.
        /// </summary>
        public required int MaxConcurrentTaskCosmosDBRequests { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of concurrent time zone requests allowed.
        /// This limit controls the parallelism level for time zone-related operations, ensuring that
        /// excessive concurrent requests do not overwhelm the underlying systems or services responsible for
        /// processing these time zone requests.
        /// </summary>
        public required int MaxConcurrentTimezoneRequests { get; set; }
    }
}