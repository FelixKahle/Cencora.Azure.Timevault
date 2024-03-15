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
        Task<string> SearchTimevaultIanaTimezoneCodeAsync(Location location);

        /// <summary>
        /// Searches for the IANA timezone code based on the provided coordinate coordinates.
        /// </summary>
        /// <param name="coordinate">The coordinate coordinates to search for.</param>
        /// <returns>The IANA timezone code.</returns>
        Task<string> SearchTimevaultIanaTimezoneCodeAsync(GeoCoordinate coordinate);

        /// <summary>
        /// Searches for Timevault documents based on the provided location.
        /// </summary>
        /// <param name="location">The location to search for.</param>
        /// <returns>The I
        Task<IList<TimevaultDocument>> SearchTimevaultAsync(Location location);

        /// <summary>
        /// Searches for Timevault documents based on the provided coordinates.
        /// </summary>
        /// <param name="coordinate">The coordinate coordinates to search for.</param>
        /// <returns>A list of Timevault documents.</returns>
        Task<IList<TimevaultDocument>> SearchTimevaultAsync(GeoCoordinate coordinate);

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
        Task<string> GetIanaTimezoneAsync(Location location);

        /// <summary>
        /// Gets the IANA timezone code based on the provided coordinate coordinates.
        /// </summary>
        /// <param name="coordinate">The coordinate coordinates to get the IANA timezone code for.</param>
        /// <returns>The IANA timezone code.</returns>
        Task<string> GetIanaTimezoneAsync(GeoCoordinate coordinate);
    }
}