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
        /// </summary>
        public required string TimevaultCosmosDBDatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the name of the Timevault Cosmos DB container.
        /// </summary>
        public required string TimevaultCosmosDBContainerName { get; set; }

        /// <summary>
        /// Represents the interval in minutes for updating the IANA codes.
        /// </summary>
        public required int IanaCodeUpdateIntervalInMinutes { get; set; }
    }
}