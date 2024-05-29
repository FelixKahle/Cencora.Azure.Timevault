// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using Azure.Maps.Search.Models;
using Azure.Maps.Timezone.Models;

namespace Cencora.Azure.Timevault
{
    /// <summary>
    /// Represents a service for interacting with the Timevault API.
    /// </summary>
    public interface ITimevaultService
    {
        /// <summary>
        /// Retrieves the IANA timezone code by location asynchronously.
        /// </summary>
        /// <param name="location">The location to retrieve the IANA timezone code for.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the API response with the IANA timezone code.</returns>
        Task<ApiResponse<string>> GetIanaCodeByLocationAsync(Location location);

        /// <summary>
        /// Retrieves the IANA code for each location in a batch asynchronously.
        /// </summary>
        /// <param name="locations">The collection of locations for which to retrieve the IANA codes.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary where the key is the location and the value is the API response containing the IANA code.</returns>
        Task<IDictionary<Location, ApiResponse<string>>> GetIanaCodeByLocationBatchAsync(IEnumerable<Location> locations);

        /// <summary>
        /// Searches for Timevault documents by location asynchronously.
        /// </summary>
        /// <param name="location">The location to search for Timevault documents.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the API response with the list of Timevault documents.</returns>
        Task<ApiResponse<IList<TimevaultDocument>>> SearchTimevaultAsync(Location location);

        /// <summary>
        /// Queries Timevault documents asynchronously.
        /// </summary>
        /// <param name="query">The query string to search for Timevault documents.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the API response with the list of Timevault documents.</returns>
        Task<ApiResponse<IList<TimevaultDocument>>> QueryTimevaultAsync(string query);

        /// <summary>
        /// Determines if a Timevault document requires an update.
        /// </summary>
        /// <param name="document">The Timevault document to check for update.</param>
        /// <returns>True if the Timevault document requires an update, otherwise false.</returns>
        bool TimevaultDocumentRequiresUpdate(TimevaultDocument document);

        /// <summary>
        /// Attempts to update the IANA timezone code of a Timevault document asynchronously.
        /// </summary>
        /// <param name="document">The Timevault document to update the IANA timezone code for.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated Timevault document.</returns>
        Task<TimevaultDocument> AttemptUpdateIanaTimezoneCodeAsync(TimevaultDocument document);

        /// <summary>
        /// Retrieves the timezone information by coordinate asynchronously using the Maps API.
        /// </summary>
        /// <param name="coordinate">The geographic coordinate to retrieve the timezone information for.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the API response with the timezone information.</returns>
        Task<ApiResponse<TimezoneResult>> MapsGetTimezoneByCoordinateAsync(GeoCoordinate coordinate);

        /// <summary>
        /// Retrieves the IANA timezone code by coordinate asynchronously using the Maps API.
        /// </summary>
        /// <param name="coordinate">The geographic coordinate to retrieve the IANA timezone code for.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the API response with the IANA timezone code.</returns>
        Task<ApiResponse<string>> MapsGetIanaTimezoneCodeByCoordinateAsync(GeoCoordinate coordinate);

        /// <summary>
        /// Searches for addresses asynchronously using the Maps API.
        /// </summary>
        /// <param name="query">The query string to search for addresses.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the API response with the search results.</returns>
        Task<ApiResponse<SearchAddressResult>> MapsSearchAddressAsync(string query);

        /// <summary>
        /// Searches for locations asynchronously using the Maps API.
        /// </summary>
        /// <param name="location">The location to search for.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the API response with the search results.</returns>
        Task<ApiResponse<SearchAddressResult>> MapsSearchLocationAsync(Location location);

        /// <summary>
        /// Searches for geographic coordinates asynchronously using the Maps API.
        /// </summary>
        /// <param name="query">The query string to search for geographic coordinates.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the API response with the search results.</returns>
        Task<ApiResponse<GeoCoordinate>> MapsSearchCoordinateAsync(string query);

        /// <summary>
        /// Searches for geographic coordinates asynchronously using the Maps API.
        /// </summary>
        /// <param name="location">The location to search for geographic coordinates.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the API response with the search results.</returns>
        Task<ApiResponse<GeoCoordinate>> MapsSearchCoordinateAsync(Location location);
    }
}