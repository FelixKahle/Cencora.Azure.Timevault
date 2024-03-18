// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using System.Net;
using Azure;
using Azure.Maps.Search;
using Azure.Maps.Search.Models;
using Azure.Maps.Timezone;
using Azure.Maps.Timezone.Models;
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
        /// The maps search client used to search for locationes and coordinates.
        /// </summary>
        private readonly MapsSearchClient _mapsSearchClient;

        /// <summary>
        /// The maps timezone client used to search for timezones.
        /// </summary>
        private readonly MapsTimezoneClient _mapsTimezoneClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimevaultService"/> class with the specified logger, settings, Cosmos DB client, and maps search client.
        /// </summary>
        /// <param name="logger">The logger used to log messages.</param>
        /// <param name="settings">The settings used by the Timevault service.</param>
        /// <param name="cosmosClient">The Cosmos DB client used to interact with the Timevault database.</param>
        /// <param name="mapsSearchClient">The maps search client used to search for locationes and coordinates.</param>
        /// <param name="mapsTimezoneClient">The maps timezone client used to search for timezones.</param>
        public TimevaultService(
            ILogger<TimevaultService> logger,
            TimevaultFunctionSettings settings,
            CosmosClient cosmosClient,
            MapsSearchClient mapsSearchClient,
            MapsTimezoneClient mapsTimezoneClient)
        {
            _logger = logger;
            _settings = settings;
            _cosmosClient = cosmosClient;
            _mapsSearchClient = mapsSearchClient;
            _mapsTimezoneClient = mapsTimezoneClient;
        }

        /// <summary>
        /// Searches for the IANA timezone code based on the given location.
        /// </summary>
        /// <param name="location">The location to search for.</param>
        /// <returns>The IANA timezone code.</returns>
        public async Task<string?> SearchTimevaultIanaTimezoneCodeAsync(Location location)
        {
            IList<TimevaultDocument> documents = await SearchTimevaultAsync(location);

            if (!documents.Any())
            {
                _logger.LogWarning($"No Timevault documents found for location: {location}");
                return null;
            }

            if (documents.Count > 1)
            {
                _logger.LogWarning($"1 Timevault document expected for location: {location}, but {documents.Count} found. Using the first document found.");
            }

            return documents.FirstOrDefault()?.IanaCode ?? null;
        }

        /// <summary>
        /// Searches for Timevault documents based on the specified location.
        /// </summary>
        /// <param name="location">The location to search for.</param>
        /// <returns>A list of Timevault documents matching the specified location.</returns>
        public async Task<IList<TimevaultDocument>> SearchTimevaultAsync(Location location)
        {
            List<TimevaultDocument> result = new List<TimevaultDocument>();

            try
            {
                Database database = _cosmosClient.GetDatabase(_settings.TimevaultCosmosDBDatabaseName);
                Container container = database.GetContainer(_settings.TimevaultCosmosDBContainerName);

                string queryString = BuildlocationQueryString(location);
                if (string.IsNullOrEmpty(queryString))
                {
                    _logger.LogWarning($"No query string generated for location: {location}");
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
        /// Retrieves the IANA timezone code for a given location.
        /// </summary>
        /// <param name="location">The location for which to retrieve the timezone code.</param>
        /// <returns>The IANA timezone code for the specified location.</returns>
        public async Task<string?> GetIanaTimezoneAsync(Location location)
        {
            IList<TimevaultDocument> documents = await SearchTimevaultAsync(location);

            if (documents.Any())
            {
                TimevaultDocument foundDocument = documents.First();
                if (documents.Count > 1)
                {
                    _logger.LogWarning($"1 Timevault document expected for location: {location}, but {documents.Count} found. Using the first document found.");
                }
                await TryUpdateTimevaultDocumentIanaCode(foundDocument);
                return foundDocument.IanaCode;
            }
            else
            {
                _logger.LogInformation($"No Timevault document found for location: {location}. Creating new document...");

                GeoCoordinate? coordinate = await MapsSearchlocation(location);
                if (coordinate == null)
                {
                    _logger.LogWarning($"No coordinates found for location: {location}");
                    return null;
                }

                string? ianaCode = await MapsSearchTimezoneAsync(coordinate.Value);
                if (ianaCode == null)
                {
                    _logger.LogWarning($"No IANA timezone code found for coordinate: {coordinate}");
                    return null;
                }

                TimevaultDocument newDocument = new TimevaultDocument(ianaCode, location, coordinate.Value, DateTime.UtcNow);
                await UpsertTimevaultDocumentAsync(newDocument);
                return ianaCode;
            }
        }

        /// <summary>
        /// Retrieves the IANA timezone codes for a batch of locations asynchronously.
        /// </summary>
        /// <param name="locations">The collection of locations to retrieve the IANA timezone codes for.</param>
        /// <returns>A dictionary containing the IANA timezone code for each location. If a location is not found or an error occurs, the value will be null.</returns>
        public async Task<Dictionary<Location, string?>> GetIanaTimezoneBatchAsync(IEnumerable<Location> locations)
        {
            // Stores the result of the IANA timezone code search for each location.
            Dictionary<Location, string?> result = new Dictionary<Location, string?>();

            // Make sure that the locations are unique.
            locations = locations.Distinct();

            // All locations that have not been found in the Timevault database.
            // These locations will be searched for and added to the database.
            List<Location> locationsToSearch = new List<Location>();

            foreach (Location location in locations)
            {
                IList<TimevaultDocument> documents = await SearchTimevaultAsync(location);
                if (documents.Any())
                {
                    TimevaultDocument foundDocument = documents.First();
                    if (documents.Count > 1)
                    {
                        _logger.LogWarning($"1 Timevault document expected for location: {location}, but {documents.Count} found. Using the first document found.");
                    }
                    await TryUpdateTimevaultDocumentIanaCode(foundDocument);
                    result.Add(location, foundDocument.IanaCode);
                }
                else
                {
                    locationsToSearch.Add(location);
                }
            }

            if (locationsToSearch.Any())
            {
                Dictionary<Location, GeoCoordinate?> locationGeoCoordinates = await MapsSearchlocationes(locationsToSearch);
                foreach (var locationGeoCoordinatePair in locationGeoCoordinates)
                {
                    if (locationGeoCoordinatePair.Value == null)
                    {
                        result.Add(locationGeoCoordinatePair.Key, null);
                        continue;
                    }

                    string? ianaCode = await MapsSearchTimezoneAsync(locationGeoCoordinatePair.Value.Value);
                    if (string.IsNullOrEmpty(ianaCode))
                    {
                        result.Add(locationGeoCoordinatePair.Key, null);
                        continue;
                    }

                    TimevaultDocument newDocument = new TimevaultDocument(ianaCode, locationGeoCoordinatePair.Key, locationGeoCoordinatePair.Value.Value, DateTime.UtcNow);
                    await UpsertTimevaultDocumentAsync(newDocument);
                    result.Add(locationGeoCoordinatePair.Key, ianaCode);
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if an update to the IANA timezone code is required for the specified Timevault document.
        /// </summary>
        /// <param name="document">The Timevault document to check.</param>
        /// <returns>True if an update to the IANA timezone code is required, otherwise false.</returns>
        public bool RequiredIanaTimezoneCodeUpdate(TimevaultDocument document)
        {
            DateTime now = DateTime.UtcNow;
            DateTime lastUpdated = document.LastIanaCodeUpdateTimestamp;
            return (now - lastUpdated) > TimeSpan.FromMinutes(_settings.IanaCodeUpdateIntervalInMinutes);
        }

        /// <summary>
        /// Updates the IANA code of a Timevault document based on its coordinate.
        /// </summary>
        /// <param name="document">The Timevault document to update.</param>
        /// <returns>The updated Timevault document.</returns>
        public async Task<TimevaultDocument> UpdateTimevaultDocumentIanaCode(TimevaultDocument document)
        {
            string? ianaCode = await MapsSearchTimezoneAsync(document.Coordinate);
            if (ianaCode == null)
            {
                _logger.LogWarning($"No IANA timezone code found for coordinate: {document.Coordinate}");
                return document;
            }

            document.IanaCode = ianaCode;
            document.LastIanaCodeUpdateTimestamp = DateTime.UtcNow;
            await UpsertTimevaultDocumentAsync(document);

            return document;
        }

        /// <summary>
        /// Tries to update the IANA timezone code of a Timevault document.
        /// </summary>
        /// <param name="document">The Timevault document to update.</param>
        /// <returns>
        /// The updated Timevault document if the IANA timezone code needs to be updated,
        /// otherwise returns the original document.
        /// </returns>
        public async Task<TimevaultDocument> TryUpdateTimevaultDocumentIanaCode(TimevaultDocument document)
        {
            if (RequiredIanaTimezoneCodeUpdate(document))
            {
                return await UpdateTimevaultDocumentIanaCode(document);
            }
            return document;
        }

        /// <summary>
        /// Searches for a location using the provided query string.
        /// </summary>
        /// <param name="location">The location to search for.</param>
        /// <returns>The geographic coordinates of the searched location, or null if not found.</returns>
        private async Task<GeoCoordinate?> MapsSearchlocation(Location location)
        {
            string queryString = location.MapsQueryString();
            return await MapsSearchlocation(queryString);
        }

        /// <summary>
        /// Searches for an location using the specified query string and returns the corresponding GeoCoordinate.
        /// </summary>
        /// <param name="queryString">The query string used to search for the location.</param>
        /// <returns>The GeoCoordinate of the best matching location, or null if no results were found or an error occurred.</returns>
        private async Task<GeoCoordinate?> MapsSearchlocation(string queryString)
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
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching for location: {queryString}, {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Searches for the geo-coordinates of the given locations using the Maps service.
        /// </summary>
        /// <param name="locationes">The locations to search for.</param>
        /// <returns>A dictionary containing the locations and their corresponding geo-coordinates.</returns>
        private async Task<Dictionary<Location, GeoCoordinate?>> MapsSearchlocationes(IEnumerable<Location> locationes)
        {
            var queryStringMap = locationes.Distinct().ToDictionary(a => a, a => a.MapsQueryString());
            Dictionary<string, GeoCoordinate?> stringGeoPositions = await MapsSearchlocationes(queryStringMap.Values);

            return queryStringMap.ToDictionary(pair => pair.Key, pair => stringGeoPositions[pair.Value]);
        }

        /// <summary>
        /// Searches for locationes using the provided query strings and returns a dictionary of location and corresponding GeoCoordinate.
        /// </summary>
        /// <param name="queryStrings">The collection of query strings to search for locationes.</param>
        /// <returns>A dictionary containing the location as the key and the corresponding GeoCoordinate as the value.</returns>
        private async Task<Dictionary<string, GeoCoordinate?>> MapsSearchlocationes(IEnumerable<string> queryStrings)
        {
            IEnumerable<SearchAddressQuery> queries = queryStrings.Distinct().Select(x => new SearchAddressQuery(x));
            var locationGeoCoordinates = new Dictionary<string, GeoCoordinate?>(queries.Count());

            try
            {
                SearchAddressBatchOperation batchResult = await _mapsSearchClient.SearchAddressBatchAsync(WaitUntil.Completed, queries);
                foreach (var queryResult in batchResult.Value.Results)
                {
                    var bestResult = queryResult.Results.OrderByDescending(r => r.Score).First();
                    if (bestResult != null)
                    {
                        GeoCoordinate geoCoordinate = new GeoCoordinate(bestResult.Position.Latitude, bestResult.Position.Longitude);
                        locationGeoCoordinates.Add(queryResult.Query, geoCoordinate);
                    }
                    else
                    {
                        _logger.LogWarning($"No GeoCoordinate found for query: {queryResult.Query}");
                        locationGeoCoordinates.Add(queryResult.Query, null);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching for locationes: {ex.Message}");
            }

            return locationGeoCoordinates;
        }

        /// <summary>
        /// Searches for the timezone based on the given coordinates.
        /// </summary>
        /// <param name="coordinate">The coordinates to search for.</param>
        /// <returns>The timezone ID if found, or null if no timezone is found.</returns>
        private async Task<string?> MapsSearchTimezoneAsync(GeoCoordinate coordinate)
        {
            try
            {
                IEnumerable<double> coordinates = new[] { coordinate.Latitude, coordinate.Longitude };
                TimezoneResult result = await _mapsTimezoneClient.GetTimezoneByCoordinatesAsync(coordinates);

                if (result == null || !result.TimeZones.Any())
                {
                    return null;
                }

                if (result.TimeZones.Count() > 1)
                {
                    _logger.LogWarning($"1 timezone expected for coordinate: {coordinate}, but {result.TimeZones.Count()} found. Using the first timezone found.");
                }

                return result.TimeZones.FirstOrDefault()?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching for timezone: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Adds a <see cref="TimevaultDocument"/> to the Cosmos DB database.
        /// </summary>
        /// <param name="document">The <see cref="TimevaultDocument"/> to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="document"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <see cref="TimevaultDocument.Id"/> or <see cref="TimevaultDocument.IanaCode"/> is <c>null</c> or empty.</exception>
        private async Task UpsertTimevaultDocumentAsync(TimevaultDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (string.IsNullOrEmpty(document.Id))
            {
                throw new ArgumentException("The document identifier cannot be null or empty.", nameof(document.Id));
            }

            if (string.IsNullOrEmpty(document.IanaCode))
            {
                throw new ArgumentException("The IANA timezone code cannot be null or empty.", nameof(document.IanaCode));
            }

            PartitionKey partitionKey = new PartitionKey(document.IanaCode);

            try
            {
                Database database = _cosmosClient.GetDatabase(_settings.TimevaultCosmosDBDatabaseName);
                Container container = database.GetContainer(_settings.TimevaultCosmosDBContainerName);
                ItemResponse<TimevaultDocument> response = await container.UpsertItemAsync(document, partitionKey);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                // This is not really an error but should never occur anyway.
                _logger.LogWarning($"Conflict adding Timevault document: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding Timevault document: {ex.Message}");
            }
        }

        /// <summary>
        /// Builds a query string based on the provided location object.
        /// The query string is used to filter data based on the city, state, postal code, and country of the location.
        /// </summary>
        /// <param name="location">The location object containing the city, state, postal code, and country.</param>
        /// <returns>The constructed query string.</returns>
        private string BuildlocationQueryString(Location location)
        {
            var queryParts = new List<string>(4);
            if (!string.IsNullOrEmpty(location.City) && !string.IsNullOrWhiteSpace(location.City))
            {
                queryParts.Add($"c.location.city = '{location.City}'");
            }
            if (!string.IsNullOrEmpty(location.State) && !string.IsNullOrWhiteSpace(location.State))
            {
                queryParts.Add($"c.location.state = '{location.State}'");
            }
            if (!string.IsNullOrEmpty(location.PostalCode) && !string.IsNullOrWhiteSpace(location.PostalCode))
            {
                queryParts.Add($"c.location.postalCode = '{location.PostalCode}'");
            }
            if (!string.IsNullOrEmpty(location.Country) && !string.IsNullOrWhiteSpace(location.Country))
            {
                queryParts.Add($"c.location.country = '{location.Country}'");
            }
            return queryParts.Any() ? $"SELECT * FROM c WHERE {string.Join(" AND ", queryParts)}" : string.Empty;
        }
    }
}