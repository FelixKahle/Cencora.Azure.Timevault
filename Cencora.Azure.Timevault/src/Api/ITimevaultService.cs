// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using Azure.Maps.Search.Models;

namespace Cencora.Azure.Timevault
{
    /// <summary>
    /// Represents a service for interacting with the Timevault.
    /// </summary>
    public interface ITimevaultService
    {
        /// <summary>
        /// Retrieves the IANA code for a given location asynchronously.
        /// </summary>
        /// <param name="location">The location for which to retrieve the IANA code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation (optional).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the IANA code as a string.</returns>
        Task<string> GetIanaCodeByLocationAsync(Location location, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches the Timevault for timevault documents based on the specified location.
        /// </summary>
        /// <param name="location">The location to search for.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of timevault documents.</returns>
        Task<IList<TimevaultDocument>> SearchTimevaultAsync(Location location, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches the Timevault for timevault documents based on the specified IANA timezone code.
        /// </summary>
        /// <param name="ianaCode">The IANA timezone code to search for.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of timevault documents.</returns>
        Task<IList<TimevaultDocument>> SearchTimevaultAsync(string ianaCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Queries the Timevault for timevault documents based on the specified query.
        /// </summary>
        /// <param name="query">The query to search for.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of timevault documents.</returns>
        Task<IList<TimevaultDocument>> QueryTimevaultAsync(string query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines if an update to the IANA timezone code is required for the given document.
        /// </summary>
        /// <param name="document">The Timevault document to check.</param>
        /// <returns><c>true</c> if an update is required, <c>false</c> otherwise.</returns>
        bool RequiredIanaTimezoneCodeUpdate(TimevaultDocument document);

        /// <summary>
        /// Upserts a timevault document into the Timevault.
        /// </summary>
        /// <param name="document">The timevault document to upsert.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpsertTimevaultDocumentAsync(TimevaultDocument document, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the IANA timezone code for the given timevault document.
        /// </summary>
        /// <param name="document">The timevault document to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated timevault document.</returns>
        Task<TimevaultDocument> UpdateIanaTimezoneCodeAsync(TimevaultDocument document, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for a geographic coordinate (GeoCoordinate) based on the provided location.
        /// </summary>
        /// <param name="location">The location to search for.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the GeoCoordinate for the provided location.</returns>
        Task<GeoCoordinate> SearchMapsGeoCoordinateAsync(Location location, CancellationToken cancellationToken = default);

        /// <summary>
        /// Queries the maps search address API asynchronously.
        /// </summary>
        /// <param name="query">The search query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the search address result.</returns>
        Task<SearchAddressResult> QueryMapsSearchAddressAsync(string query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches the Maps service for the IANA timezone code based on the specified geographic coordinate.
        /// </summary>
        /// <param name="coordinate">The geographic coordinate to search for.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the IANA timezone code.</returns>
        Task<string> SearchMapsIanaTimezoneCodeAsync(GeoCoordinate coordinate, CancellationToken cancellationToken = default);
    }
}