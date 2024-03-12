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
        /// Searches for the IANA timezone code based on the provided address.
        /// </summary>
        /// <param name="address">The address to search for.</param>
        /// <returns>The IANA timezone code.</returns>
        Task<string> SearchTimevaultIanaTimezoneCodeAsync(Address address);

        /// <summary>
        /// Searches for the IANA timezone code based on the provided location coordinates.
        /// </summary>
        /// <param name="location">The location coordinates to search for.</param>
        /// <returns>The IANA timezone code.</returns>
        Task<string> SearchTimevaultIanaTimezoneCodeAsync(GeoCoordinate location);

        /// <summary>
        /// Searches for the IANA timezone code based on the provided address and location coordinates.
        /// </summary>
        /// <param name="address">The address to search for.</param>
        /// <param name="location">The location coordinates to search for.</param>
        /// <returns>The IANA timezone code.</returns>
        Task<string> SearchTimevaultIanaTimezoneCodeAsync(Address address, GeoCoordinate location);

        /// <summary>
        /// Searches for Timevault documents based on the provided address.
        /// </summary>
        /// <param name="address">The address to search for.</param>
        /// <returns>A list of Timevault documents.</returns>
        Task<IList<TimevaultDocument>> SearchTimevaultAsync(Address address);

        /// <summary>
        /// Searches for Timevault documents based on the provided location coordinates.
        /// </summary>
        /// <param name="location">The location coordinates to search for.</param>
        /// <returns>A list of Timevault documents.</returns>
        Task<IList<TimevaultDocument>> SearchTimevaultAsync(GeoCoordinate location);

        /// <summary>
        /// Searches for Timevault documents based on the provided address and location coordinates.
        /// </summary>
        /// <param name="address">The address to search for.</param>
        /// <param name="location">The location coordinates to search for.</param>
        /// <returns>A list of Timevault documents.</returns>
        Task<IList<TimevaultDocument>> SearchTimevaultAsync(Address address, GeoCoordinate location);

        /// <summary>
        /// Searches for Timevault documents based on the provided IANA timezone code.
        /// </summary>
        /// <param name="ianaCode">The IANA timezone code to search for.</param>
        /// <returns>A list of Timevault documents.</returns>
        Task<IList<TimevaultDocument>> SearchTimevaultAsync(string ianaCode);

        /// <summary>
        /// Gets the IANA timezone code based on the provided address.
        /// </summary>
        /// <param name="address">The address to get the IANA timezone code for.</param>
        /// <returns>The IANA timezone code.</returns>
        Task<string> GetIanaTimezoneAsync(Address address);

        /// <summary>
        /// Gets the IANA timezone code based on the provided location coordinates.
        /// </summary>
        /// <param name="location">The location coordinates to get the IANA timezone code for.</param>
        /// <returns>The IANA timezone code.</returns>
        Task<string> GetIanaTimezoneAsync(GeoCoordinate location);

        /// <summary>
        /// Gets the IANA timezone code based on the provided address and location coordinates.
        /// </summary>
        /// <param name="address">The address to get the IANA timezone code for.</param>
        /// <param name="location">The location coordinates to get the IANA timezone code for.</param>
        /// <returns>The IANA timezone code.</returns>
        Task<string> GetIanaTimezoneAsync(Address address, GeoCoordinate location);
    }
}