// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using System.Globalization;
using Azure;
using Azure.Maps.Search;
using Azure.Maps.Search.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Cencora.Azure.Timevault
{
    /// <summary>
    /// Represents a service for interacting with the Timevault functionality.
    /// </summary>
    public class TimevaultService : ITimevaultService
    {
        /// <summary>
        /// The logger used to log messages.
        /// </summary>
        private readonly ILogger<TimevaultService> _logger;

        /// <summary>
        /// The settings used by the Timevault service.
        /// </summary>
        private readonly TimevaultFunctionSettings _settings;

        /// <summary>
        /// The Cosmos DB client used to interact with the Timevault database.
        /// </summary>
        private readonly CosmosClient _cosmosClient;

        /// <summary>
        /// The maps search client used to search for addresses and locations.
        /// </summary>
        private readonly MapsSearchClient _mapsSearchClient;

        public TimevaultService(ILogger<TimevaultService> logger, TimevaultFunctionSettings settings, CosmosClient cosmosClient, MapsSearchClient mapsSearchClient)
        {
            _logger = logger;
            _settings = settings;
            _cosmosClient = cosmosClient;
            _mapsSearchClient = mapsSearchClient;
        }

        /// <summary>
        /// Searches for the IANA timezone code asynchronously based on the provided address.
        /// </summary>
        /// <remarks>
        /// If multiple Timevault documents are found for the address, the first document found is used.
        /// </remarks>
        /// <param name="address">The address to search for.</param>
        /// <returns>The IANA timezone code.</returns>
        public async Task<string> SearchTimevaultIanaTimezoneCodeAsync(Address address)
        {
            IList<TimevaultDocument> documents = await SearchTimevaultAsync(address);

            if (!documents.Any())
            {
                _logger.LogWarning($"No Timevault documents found for address: {address}");
                return string.Empty;
            }

            if (documents.Count > 1)
            {
                _logger.LogWarning($"1 Timevault document expected for address: {address}, but {documents.Count} found. Using the first document found.");
            }

            return documents.FirstOrDefault()?.IanaCode ?? string.Empty;
        }

        /// <summary>
        /// Searches for the IANA timezone code based on the given location.
        /// </summary>
        /// <param name="location">The location coordinates.</param>
        /// <returns>The IANA timezone code.</returns>
        public async Task<string> SearchTimevaultIanaTimezoneCodeAsync(GeoCoordinate location)
        {
            IList<TimevaultDocument> documents = await SearchTimevaultAsync(location);

            if (!documents.Any())
            {
                _logger.LogWarning($"No Timevault documents found for location: {location}");
                return string.Empty;
            }

            if (documents.Count > 1)
            {
                _logger.LogWarning($"1 Timevault document expected for location: {location}, but {documents.Count} found. Using the first document found.");
            }

            return documents.FirstOrDefault()?.IanaCode ?? string.Empty;
        }

        /// <summary>
        /// Searches the Timevault for the IANA timezone code based on the given address and location.
        /// </summary>
        /// <param name="address">The address to search for.</param>
        /// <param name="location">The location coordinates to search for.</param>
        /// <returns>The IANA timezone code found in the Timevault, or an empty string if no code is found.</returns>
        public async Task<string> SearchTimevaultIanaTimezoneCodeAsync(Address address, GeoCoordinate location)
        {
            IList<TimevaultDocument> documents = await SearchTimevaultAsync(address, location);

            if (!documents.Any())
            {
                _logger.LogWarning($"No Timevault documents found for address: {address}");
                return string.Empty;
            }

            if (documents.Count > 1)
            {
                _logger.LogWarning($"1 Timevault document expected for address: {address}, but {documents.Count} found. Using the first document found.");
            }

            return documents.FirstOrDefault()?.IanaCode ?? string.Empty;
        }

        /// <summary>
        /// Searches for Timevault documents based on the provided address.
        /// </summary>
        /// <param name="address">The address to search for.</param>
        /// <returns>A list of Timevault documents that match the provided address.</returns>
        public async Task<IList<TimevaultDocument>> SearchTimevaultAsync(Address address)
        {
            List<TimevaultDocument> result = new List<TimevaultDocument>();

            try
            {
                Database database = _cosmosClient.GetDatabase(_settings.TimevaultCosmosDBDatabaseName);
                Container container = database.GetContainer(_settings.TimevaultCosmosDBContainerName);

                string queryString = BuildAddressQueryString(address);
                if (string.IsNullOrEmpty(queryString))
                {
                    _logger.LogWarning($"No query string generated for address: {address}");
                    return result;
                }

                QueryDefinition queryDefinition = new QueryDefinition(queryString);
                FeedIterator<TimevaultDocument> iterator = container.GetItemQueryIterator<TimevaultDocument>(queryDefinition);
                while (iterator.HasMoreResults)
                {
                    FeedResponse<TimevaultDocument> response = await iterator.ReadNextAsync();
                    result.AddRange(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching for Timevault documents: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Searches for Timevault documents based on the specified location.
        /// </summary>
        /// <param name="location">The location to search for.</param>
        /// <returns>A list of Timevault documents matching the specified location.</returns>
        public async Task<IList<TimevaultDocument>> SearchTimevaultAsync(GeoCoordinate location)
        {
            IFormatProvider formatProvider = CultureInfo.InvariantCulture;
            List<TimevaultDocument> result = new List<TimevaultDocument>();

            try
            {
                Database database = _cosmosClient.GetDatabase(_settings.TimevaultCosmosDBDatabaseName);
                Container container = database.GetContainer(_settings.TimevaultCosmosDBContainerName);

                string latitudeString = location.Latitude.ToString(formatProvider);
                string longitudeString = location.Longitude.ToString(formatProvider);
                string queryString = BuildGeoCoordinateQueryString(location, formatProvider);

                QueryDefinition queryDefinition = new QueryDefinition(queryString);
                FeedIterator<TimevaultDocument> iterator = container.GetItemQueryIterator<TimevaultDocument>(queryDefinition);
                while (iterator.HasMoreResults)
                {
                    FeedResponse<TimevaultDocument> response = await iterator.ReadNextAsync();
                    result.AddRange(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching for Timevault documents: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Searches for Timevault documents based on the provided address and location.
        /// </summary>
        /// <param name="address">The address to search for.</param>
        /// <param name="location">The location to search for.</param>
        /// <returns>A list of Timevault documents that match the search criteria.</returns>
        public async Task<IList<TimevaultDocument>> SearchTimevaultAsync(Address address, GeoCoordinate location)
        {
            IFormatProvider formatProvider = CultureInfo.InvariantCulture;
            List<TimevaultDocument> result = new List<TimevaultDocument>();

            try
            {
                Database database = _cosmosClient.GetDatabase(_settings.TimevaultCosmosDBDatabaseName);
                Container container = database.GetContainer(_settings.TimevaultCosmosDBContainerName);

                string queryString = BuildQueryString(address, location, formatProvider);
                if (string.IsNullOrEmpty(queryString))
                {
                    _logger.LogWarning($"No query string generated for address: {address} and location: {location}");
                    return result;
                }

                QueryDefinition queryDefinition = new QueryDefinition(queryString);
                FeedIterator<TimevaultDocument> iterator = container.GetItemQueryIterator<TimevaultDocument>(queryDefinition);
                while (iterator.HasMoreResults)
                {
                    FeedResponse<TimevaultDocument> response = await iterator.ReadNextAsync();
                    result.AddRange(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching for Timevault documents: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Searches for Timevault documents based on the provided IANA code.
        /// </summary>
        /// <param name="ianaCode">The IANA code to search for.</param>
        /// <returns>A list of Timevault documents matching the provided IANA code.</returns>
        public async Task<IList<TimevaultDocument>> SearchTimevaultAsync(string ianaCode)
        {
            List<TimevaultDocument> result = new List<TimevaultDocument>();

            try
            {
                Database database = _cosmosClient.GetDatabase(_settings.TimevaultCosmosDBDatabaseName);
                Container container = database.GetContainer(_settings.TimevaultCosmosDBContainerName);

                string queryString = $"SELECT * FROM c WHERE c.ianaCode = '{ianaCode}'";

                QueryDefinition queryDefinition = new QueryDefinition(queryString);
                FeedIterator<TimevaultDocument> iterator = container.GetItemQueryIterator<TimevaultDocument>(queryDefinition);
                while (iterator.HasMoreResults)
                {
                    FeedResponse<TimevaultDocument> response = await iterator.ReadNextAsync();
                    result.AddRange(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching for Timevault documents: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Retrieves the IANA timezone code for the given address asynchronously.
        /// </summary>
        /// <remarks>
        /// If a Timevault document is found for the address, the IANA timezone code is retrieved from the document.
        /// If no document is found, the maps services are queried to retrieve the IANA timezone code.
        /// The retrieved IANA timezone code is then added to the Timevault database for future use.
        /// </remarks>
        /// <param name="address">The address for which to retrieve the IANA timezone code.</param>
        /// <returns>The IANA timezone code for the given address.</returns>
        public async Task<string> GetIanaTimezoneAsync(Address address)
        {
            IList<TimevaultDocument> documents = await SearchTimevaultAsync(address);

            // We found a document for the address, so there is no need to query the maps services.
            if (documents.Any())
            {
                if (documents.Count > 1)
                {
                    _logger.LogWarning($"1 Timevault document expected for address: {address}, but {documents.Count} found. Using the first document found.");
                }
                return documents.FirstOrDefault()?.IanaCode ?? string.Empty;
            }

            // Query the maps services to first find the geographic coordinates of the address.
            GeoCoordinate? location = await MapsSearchAddress(address);

            // TODO: Query the Timezone API to get the IANA timezone code, and add the document to the Timevault database.

            throw new NotImplementedException("Implement Maps API call to get IANA timezone code.");
        }

        /// <summary>
        /// Retrieves the IANA timezone code for the given location asynchronously.
        /// </summary>
        /// <remarks>
        /// If a Timevault document is found for the location, the IANA timezone code is retrieved from the document.
        /// If no document is found, the maps services are queried to retrieve the IANA timezone code.
        /// The retrieved IANA timezone code is then added to the Timevault database for future use.
        /// </remarks>
        /// <param name="location">The location for which to retrieve the IANA timezone code.</param>
        /// <returns>The IANA timezone code for the given location.</returns>
        public Task<string> GetIanaTimezoneAsync(GeoCoordinate location)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Retrieves the IANA timezone code for the given address and location asynchronously.
        /// </summary>
        /// <remarks>
        /// If a Timevault document is found for the address and location, the IANA timezone code is retrieved from the document.
        /// If no document is found, the maps services are queried to retrieve the IANA timezone code.
        /// The retrieved IANA timezone code is then added to the Timevault database for future use.
        /// </remarks>
        /// <param name="address">The address for which to retrieve the IANA timezone code.</param>
        /// <param name="location">The location for which to retrieve the IANA timezone code.</param>
        /// <returns>The IANA timezone code for the given address and location.</returns>
        public Task<string> GetIanaTimezoneAsync(Address address, GeoCoordinate location)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Searches for a geographic coordinate based on the provided address.
        /// </summary>
        /// <param name="address">The address to search for.</param>
        /// <returns>The geographic coordinate corresponding to the address, or null if not found.</returns>
        private async Task<GeoCoordinate?> MapsSearchAddress(Address address)
        {
            string queryString = address.MapsQueryString();
            return await MapsSearchAddress(queryString);
        }

        /// <summary>
        /// Searches for an address using the specified query string and returns the corresponding GeoCoordinate.
        /// </summary>
        /// <param name="queryString">The query string used to search for the address.</param>
        /// <returns>The GeoCoordinate of the best matching address, or null if no results were found or an error occurred.</returns>
        private async Task<GeoCoordinate?> MapsSearchAddress(string queryString)
        {
            try
            {
                SearchAddressResult result = await _mapsSearchClient.SearchAddressAsync(queryString);
                var bestResult = result.Results.OrderByDescending(r => r.Score).FirstOrDefault();

                if (bestResult != null)
                {
                    return new GeoCoordinate(bestResult.Position.Latitude, bestResult.Position.Longitude);
                }
                else
                {
                    _logger.LogWarning($"No results found for query: {queryString}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching for address: {queryString}, {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Searches for addresses in the maps service and retrieves their corresponding geographic coordinates.
        /// </summary>
        /// <param name="addresses">The collection of addresses to search for.</param>
        /// <returns>A dictionary containing the addresses as keys and their corresponding geographic coordinates as values.</returns>
        private async Task<Dictionary<Address, GeoCoordinate?>> MapsSearchAddresses(IEnumerable<Address> addresses)
        {
            var queryStringMap = addresses.Distinct().ToDictionary(a => a, a => a.MapsQueryString());
            Dictionary<string, GeoCoordinate?> stringGeoPositions = await MapsSearchAddresses(queryStringMap.Values);

            return queryStringMap.ToDictionary(pair => pair.Key, pair => stringGeoPositions[pair.Value]);
        }

        /// <summary>
        /// Searches for addresses using the provided query strings and returns a dictionary of address and corresponding GeoCoordinate.
        /// </summary>
        /// <param name="queryStrings">The collection of query strings to search for addresses.</param>
        /// <returns>A dictionary containing the address as the key and the corresponding GeoCoordinate as the value.</returns>
        private async Task<Dictionary<string, GeoCoordinate?>> MapsSearchAddresses(IEnumerable<string> queryStrings)
        {
            IEnumerable<SearchAddressQuery> queries = queryStrings.Distinct().Select(x => new SearchAddressQuery(x));
            var addressGeoCoordinates = new Dictionary<string, GeoCoordinate?>(queries.Count());

            try
            {
                SearchAddressBatchOperation batchResult = await _mapsSearchClient.SearchAddressBatchAsync(WaitUntil.Completed, queries);
                foreach (var queryResult in batchResult.Value.Results)
                {
                    var bestResult = queryResult.Results.OrderByDescending(r => r.Score).First();
                    if (bestResult != null)
                    {
                        GeoCoordinate geoCoordinate = new GeoCoordinate(bestResult.Position.Latitude, bestResult.Position.Longitude);
                        addressGeoCoordinates.Add(queryResult.Query, geoCoordinate);
                    }
                    else
                    {
                        _logger.LogWarning($"No GeoCoordinate found for query: {queryResult.Query}");
                        addressGeoCoordinates.Add(queryResult.Query, null);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching for addresses: {ex.Message}");
            }

            return addressGeoCoordinates;
        }

        /// <summary>
        /// Adds a <see cref="TimevaultDocument"/> to the Cosmos DB database.
        /// </summary>
        /// <param name="document">The <see cref="TimevaultDocument"/> to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task AddTimevaultDocumentAsync(TimevaultDocument document)
        {
            try
            {
                Database database = _cosmosClient.GetDatabase(_settings.TimevaultCosmosDBDatabaseName);
                Container container = database.GetContainer(_settings.TimevaultCosmosDBContainerName);
                ItemResponse<TimevaultDocument> response = await container.UpsertItemAsync(document);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding Timevault document: {ex.Message}");
            }
        }

        /// <summary>
        /// Builds a query string for retrieving data based on a given GeoCoordinate location.
        /// </summary>
        /// <param name="location">The GeoCoordinate location.</param>
        /// <param name="formatProvider">The format provider used to convert the latitude and longitude values to strings.</param>
        /// <returns>A query string for retrieving data based on the specified GeoCoordinate location.</returns>
        private string BuildGeoCoordinateQueryString(GeoCoordinate location, IFormatProvider formatProvider)
        {
            string latitudeString = location.Latitude.ToString(formatProvider);
            string longitudeString = location.Longitude.ToString(formatProvider);
            return $"SELECT * FROM c WHERE c.location.latitude = {latitudeString} AND c.location.longitude = {longitudeString}";
        }

        /// <summary>
        /// Builds a query string based on the provided address object.
        /// The query string is used to filter documents based on the address properties.
        /// </summary>
        /// <param name="address">The address object used to build the query string.</param>
        /// <returns>The generated query string.</returns>
        private string BuildAddressQueryString(Address address)
        {
            var queryParts = new List<string>(5);
            if (!string.IsNullOrEmpty(address.Street) && !string.IsNullOrWhiteSpace(address.Street))
            {
                queryParts.Add($"c.address.street = '{address.Street}'");
            }
            if (!string.IsNullOrEmpty(address.City) && !string.IsNullOrWhiteSpace(address.City))
            {
                queryParts.Add($"c.address.city = '{address.City}'");
            }
            if (!string.IsNullOrEmpty(address.State) && !string.IsNullOrWhiteSpace(address.State))
            {
                queryParts.Add($"c.address.state = '{address.State}'");
            }
            if (!string.IsNullOrEmpty(address.PostalCode) && !string.IsNullOrWhiteSpace(address.PostalCode))
            {
                queryParts.Add($"c.address.postalCode = '{address.PostalCode}'");
            }
            if (!string.IsNullOrEmpty(address.Country) && !string.IsNullOrWhiteSpace(address.Country))
            {
                queryParts.Add($"c.address.country = '{address.Country}'");
            }
            return queryParts.Any() ? $"SELECT * FROM c WHERE {string.Join(" AND ", queryParts)}" : string.Empty;
        }

        /// <summary>
        /// Builds a query string based on the provided address, location, and format provider.
        /// </summary>
        /// <param name="address">The address object containing street, city, state, postal code, and country.</param>
        /// <param name="location">The geographic coordinates of the location.</param>
        /// <param name="formatProvider">The format provider used to format the latitude and longitude values.</param>
        /// <returns>A query string that can be used to filter data based on the provided address and location.</returns>
        private string BuildQueryString(Address address, GeoCoordinate location, IFormatProvider formatProvider)
        {
            var queryParts = new List<string>(5);
            if (!string.IsNullOrEmpty(address.Street) && !string.IsNullOrWhiteSpace(address.Street))
            {
                queryParts.Add($"c.address.street = '{address.Street}'");
            }
            if (!string.IsNullOrEmpty(address.City) && !string.IsNullOrWhiteSpace(address.City))
            {
                queryParts.Add($"c.address.city = '{address.City}'");
            }
            if (!string.IsNullOrEmpty(address.State) && !string.IsNullOrWhiteSpace(address.State))
            {
                queryParts.Add($"c.address.state = '{address.State}'");
            }
            if (!string.IsNullOrEmpty(address.PostalCode) && !string.IsNullOrWhiteSpace(address.PostalCode))
            {
                queryParts.Add($"c.address.postalCode = '{address.PostalCode}'");
            }
            if (!string.IsNullOrEmpty(address.Country) && !string.IsNullOrWhiteSpace(address.Country))
            {
                queryParts.Add($"c.address.country = '{address.Country}'");
            }
            
            queryParts.Add($"c.location.latitude = {location.Latitude.ToString(formatProvider)}");
            queryParts.Add($"c.location.longitude = {location.Longitude.ToString(formatProvider)}");

            return queryParts.Any() ? $"SELECT * FROM c WHERE {string.Join(" AND ", queryParts)}" : string.Empty;
        }
    }
}