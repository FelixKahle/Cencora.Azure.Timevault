// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

namespace Cencora.Azure.Timevault
{
    /// <summary>
    /// Represents a service for interacting with the Timevault API.
    /// </summary>
    public interface ITimevaultService
    {
        /// <summary>
        /// Searches for the IANA timezone code based on the provided location.
        /// </summary>
        /// <param name="location">The location to search for.</param>
        /// <returns>The IANA timezone code.</returns>
        Task<string?> SearchTimevaultIanaTimezoneCodeAsync(Location location);

        /// <summary>
        /// Searches for Timevault documents based on the provided location.
        /// </summary>
        /// <param name="location">The location to search for.</param>
        /// <returns>The I
        Task<IList<TimevaultDocument>> SearchTimevaultAsync(Location location);

        /// <summary>
        /// Searches for Timevault documents based on the provided IANA timezone code.
        /// </summary>
        /// <param name="ianaCode">The IANA timezone code to search for.</param>
        /// <returns>A list of Timevault documents.</returns>
        Task<IList<TimevaultDocument>> SearchTimevaultAsync(string ianaCode);

        /// <summary>
        /// Gets the IANA timezone code based on the provided location.
        /// </summary>
        /// <param name="location">The location to get the IANA timezone code for.</param>
        /// <returns>The IANA timezone code.</returns>
        Task<string?> GetIanaTimezoneAsync(Location location);

        /// <summary>
        /// Retrieves the IANA timezones for a batch of locations asynchronously.
        /// </summary>
        /// <param name="locations">The collection of locations for which to retrieve the IANA timezones.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary where the keys are the locations and the values are the corresponding IANA timezones.</returns>
        Task<Dictionary<Location, string?>> GetIanaTimezoneBatchAsync(IEnumerable<Location> locations);

        /// <summary>
        /// Determines if an update to the IANA timezone code is required for the specified Timevault document.
        /// </summary>
        /// <param name="document">The Timevault document to check.</param>
        /// <returns>True if an update to the IANA timezone code is required, otherwise false.</returns>
        bool RequiredIanaTimezoneCodeUpdate(TimevaultDocument document);
    }
}