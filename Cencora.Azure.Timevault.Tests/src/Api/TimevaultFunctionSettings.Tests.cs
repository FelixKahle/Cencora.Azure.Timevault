// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

namespace Cencora.Azure.Timevault.Tests
{
    /// <summary>
    /// Provides extension methods for the <see cref="TimevaultFunctionSettings"/> class.
    /// </summary>
    internal static class TimevaultFunctionSettingsExtensions
    {
        /// <summary>
        /// Represents the settings for the Timevault function.
        /// </summary>
        public static TimevaultFunctionSettings TestDefault()
        {
            return new TimevaultFunctionSettings
            {
                TimevaultCosmosDBDatabaseName = "Timevault",
                TimevaultCosmosDBContainerName = "Documents",
                IanaCodeUpdateIntervalInDays = 30
            };
        }
    }
}