// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using Polly;

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

        /// <summary>
        /// Gets or sets a value indicating whether to use jitter.
        /// </summary>
        public required bool UseJitter { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for the operation.
        /// </summary>
        public required int MaxRetryAttempts { get; set; }

        /// <summary>
        /// Gets or sets the base retry interval in milliseconds for retrying an operation.
        /// </summary>
        public required int RetryDelayMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets the type of delay backoff.
        /// </summary>
        public required DelayBackoffType BackoffType { get; set; }

        /// <summary>
        /// Gets or sets the maximum delay in milliseconds for retrying an operation.
        /// </summary>
        public required int MaxRetryDelayInMilliseconds { get; set; }
    }
}