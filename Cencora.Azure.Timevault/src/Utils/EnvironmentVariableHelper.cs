// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

namespace Cencora.Azure.Timevault
{
    /// <summary>
    /// Provides helper methods for working with environment variables.
    /// </summary>
    public static class EnvironmentVariableHelper
    {
        /// <summary>
        /// Retrieves the value of the specified environment variable as an integer.
        /// If the environment variable is not found or cannot be parsed as an integer, the default value is returned.
        /// </summary>
        /// <param name="name">The name of the environment variable.</param>
        /// <param name="defaultValue">The default value to return if the environment variable is not found or cannot be parsed.</param>
        /// <returns>The value of the environment variable as an integer, or the default value if the environment variable is not found or cannot be parsed.</returns>
        public static int GetInt32(string name, int? defaultValue = null)
        {
            string? value = System.Environment.GetEnvironmentVariable(name);
            if (value == null)
            {
                return defaultValue ?? throw new ArgumentException(nameof(defaultValue));
            }

            if (int.TryParse(value, out int result))
            {
                return result;
            }

            return defaultValue ?? throw new ArgumentException(nameof(defaultValue));
        }
    }
}