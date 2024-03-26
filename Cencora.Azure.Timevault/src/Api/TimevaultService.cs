// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using Azure;
using Azure.Maps.Search;
using Azure.Maps.Search.Models;
using Azure.Maps.Timezone;
using Azure.Maps.Timezone.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Cencora.Azure.Timevault
{
    /// <summary>
    /// Represents a service for interacting with the Timevault database.
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
        /// Retrieves the IANA timezone code for a given location asynchronously.
        /// </summary>
        /// <param name="location">The location for which to retrieve the IANA timezone code.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the IANA timezone code.</returns>
        /// <remarks>
        /// This method first searches for a Timevault document for the given location. If a document is found, it checks if the document requires an update and attempts to update it. If no document is found, it searches for the IANA timezone code using the Maps API and creates a new Timevault document for the location.
        /// </remarks>
        public async Task<ApiResponse<string>> GetIanaCodeByLocationAsync(Location location)
        {
            _logger.LogInformation($"{nameof(GetIanaCodeByLocationAsync)}: Retrieving IANA timezone code for location {location}.");

            ApiResponse<IList<TimevaultDocument>> searchResult = await SearchTimevaultAsync(location);
            if (searchResult && searchResult.Value.Any())
            {
                IList<TimevaultDocument> documents = searchResult.Value;

                if (documents.Count > 1)
                {
                    _logger.LogWarning($"Expected one Timevault document for location {location}, but {documents.Count} found. Using the first document found.");
                }

                TimevaultDocument document = documents.First();

                _logger.LogInformation($"Timevault document with id {document.Id} found for location {location}.");

                // Check if the document requires an update and if so, attempt to update it.
                document = await AttemptUpdateIanaTimezoneCodeAsync(document);
                return ApiResponse<string>.Success(document.IanaCode);
            }
            else
            {
                _logger.LogInformation($"No Timevault document found for location {location}. Searching for timezone using the Maps API.");

                // Search for the GeoCoordinate using the Maps API.
                ApiResponse<GeoCoordinate> coordinateResult = await MapsSearchCoordinateAsync(location);
                if (!coordinateResult)
                {
                    return ApiResponse<string>.Error(coordinateResult.ErrorMessage, coordinateResult.StatusCode);
                }
                GeoCoordinate coordinate = coordinateResult.Value;

                // Search for the IANA timezone code using the Maps API.
                ApiResponse<string> timezoneResult = await MapsGetIanaTimezoneCodeByCoordinateAsync(coordinate);
                if (!timezoneResult)
                {
                    return ApiResponse<string>.Error(timezoneResult.ErrorMessage, timezoneResult.StatusCode);
                }
                string ianaCode = timezoneResult.Value;

                // Create a new Timevault document and upsert it.
                TimevaultDocument document = new TimevaultDocument(ianaCode, location, coordinate, DateTime.UtcNow);
                await UpsertTimevaultAsync(document);

                return ApiResponse<string>.Success(ianaCode);
            }
        }

        /// <summary>
        /// Retrieves the IANA timezone code for a batch of locations asynchronously.
        /// </summary>
        /// <param name="locations">The collection of locations to retrieve the IANA timezone code for.</param>
        /// <returns>A dictionary containing the location and the corresponding API response with the IANA timezone code.</returns>
        public async Task<IDictionary<Location, ApiResponse<string>>> GetIanaCodeByLocationBatchAsync(IEnumerable<Location> locations)
        {
            _logger.LogInformation($"{nameof(GetIanaCodeByLocationBatchAsync)}: Retrieving IANA timezone code for {locations.Count()} locations in batch.");

            // Make sure we only have distinct locations.
            // This will decrease the number of requests to the Timevault database and the Maps API.
            var distinctLocations = locations.Distinct().ToList();

            // Create a dictionary to store the final results.
            // We can allocate the dictionary with the distinct locations count, as we expect one result per location
            // improving the performance of the dictionary.
            var finalResults = new Dictionary<Location, ApiResponse<string>>(distinctLocations.Count());

            // Create a list to store the search tasks.
            // Then we can wait for all the search tasks to complete.
            var searchTasks = distinctLocations.Select(FetchTimevaultAsync).ToList();
            var searchResults = await Task.WhenAll(searchTasks);

            // Create a list to store the locations that need to be searched.
            // These are all the locations that did not have a Timevault document.
            var locationsToSearch = new List<Location>();

            // Create a list to store the update tasks.
            // These are the tasks that update the Timevault documents.
            var updateTasks = new List<Task<(Location, TimevaultDocument)>>();

            // We now loop through the search results.
            // If we found a Timevault document, we add it to the update tasks.
            // If we did not find a Timevault document, we add the location to the locations to search.
            foreach (var (location, searchResult) in searchResults)
            {
                if (searchResult && searchResult.Value.Any())
                {
                    _logger.LogInformation($"Timevault document found for location {location}.");

                    IList<TimevaultDocument> documents = searchResult.Value;
                    if (documents.Count > 1)
                    {
                        _logger.LogWarning($"Expected one Timevault document for location {location}, but {documents.Count} found. Using the first document found.");
                    }

                    TimevaultDocument document = documents.First();

                    updateTasks.Add(UpdateDocumentAsync(location, document));
                }
                else
                {
                    _logger.LogInformation($"No Timevault document found for location {location}. Searching for timezone using the Maps API.");
                    locationsToSearch.Add(location);
                }
            }

            // Now we search for the locations that did not have a Timevault document.
            var coordinateResults = await Task.WhenAll(locationsToSearch.Select(FetchCoordinateAsync));

            // We now have the coordinates for all the locations that did not have a Timevault document.
            // We can now search for the timezone code for each location.
            // This list will store the tasks that fetch the timezone code for each location.
            var timezoneFetchTasks = new List<Task<(Location, GeoCoordinate, ApiResponse<string>)>>();

            // We now loop through the coordinate results.
            // If we found a coordinate, we add it to the timezone fetch tasks.
            // If we did not find a coordinate, we add an error response to the final results,
            // as we cannot fetch the timezone code without a coordinate.
            foreach (var (location, coordinateResult) in coordinateResults)
            {
                if (!coordinateResult.IsSuccess)
                {
                    finalResults.TryAdd(location, ApiResponse<string>.Error(coordinateResult.ErrorMessage, coordinateResult.StatusCode));
                    continue;
                }

                timezoneFetchTasks.Add(FetchTimezoneCodeAsync(location, coordinateResult.Value));
            }

            // Now we fetch the timezone code for each location.
            var fetchedTimezones = await Task.WhenAll(timezoneFetchTasks);

            // This list will store the tasks that upsert the Timevault documents.
            // We will upsert the Timevault documents for each location that we successfully fetched the timezone code for.
            var documentUpsertTasks = new List<Task>();

            // We now loop through the fetched timezones.
            // If we found a timezone code, we add it to the final results and create a new Timevault document.
            // If we did not find a timezone code, we add an error response to the final results.
            foreach (var (location, coordinate, timezoneResponse) in fetchedTimezones)
            {
                if (!timezoneResponse.IsSuccess)
                {
                    finalResults.TryAdd(location, timezoneResponse);
                    continue;
                }

                // Update final results with the fetched timezone
                finalResults.TryAdd(location, timezoneResponse);

                // Create a new Timevault document and queue it for upsert
                TimevaultDocument newDocument = new TimevaultDocument(timezoneResponse.Value, location, coordinate, DateTime.UtcNow);
                documentUpsertTasks.Add(UpsertTimevaultAsync(newDocument));
            }

            // As we do not need the found we can wait for the update tasks to complete here.
            var updatedDocuments = await Task.WhenAll(updateTasks);
            foreach (var (location, updatedDocument) in updatedDocuments)
            {
                finalResults.Add(location, ApiResponse<string>.Success(updatedDocument.IanaCode));
            }

            // Wait for all the upsert tasks to complete
            await Task.WhenAll(documentUpsertTasks);
            
            // Finally, we return the final results
            return finalResults;
        }

        /// <summary>
        /// Helper method to fetch the Timevault documents asynchronously.
        /// </summary>
        /// <param name="location">The location to fetch the Timevault documents for.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the location and the API response with the Timevault documents.</returns>
        private async Task<(Location, ApiResponse<IList<TimevaultDocument>>)> FetchTimevaultAsync(Location location)
        {
            return (location, await SearchTimevaultAsync(location));
        }

        /// <summary>
        /// Helper method to update a document asynchronously.
        /// </summary>
        /// <param name="location">The location to update the document for.</param>
        /// <param name="document">The document to update.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated location and document.</returns>
        private async Task<(Location, TimevaultDocument)> UpdateDocumentAsync(Location location, TimevaultDocument document)
        {
            // This method asynchronously updates the document and returns the updated document along with its location
            var updatedDocument = await AttemptUpdateIanaTimezoneCodeAsync(document);
            return (location, updatedDocument);
        }

        /// <summary>
        /// Fetches the coordinate for a given location asynchronously.
        /// </summary>
        /// <param name="location">The location for which to fetch the coordinate.</param>
        /// <returns>A tuple containing the location and the coordinate response.</returns>
        private async Task<(Location, ApiResponse<GeoCoordinate>)> FetchCoordinateAsync(Location location)
        {
            // Logic to fetch the coordinate for a given location
            ApiResponse<GeoCoordinate> coordinateResponse = await MapsSearchCoordinateAsync(location);
            return (location, coordinateResponse);
        }

        /// <summary>
        /// Helper method to fetch the timezone code for a given location asynchronously.
        /// </summary>
        /// <param name="location">The location to fetch the timezone code for.</param>
        /// <param name="coordinate">The coordinate to fetch the timezone code for.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the location and the API response with the timezone code.</returns>
        private async Task<(Location, GeoCoordinate, ApiResponse<string>)> FetchTimezoneCodeAsync(Location location, GeoCoordinate coordinate)
        {
            ApiResponse<string> timezoneCodeResponse = await MapsGetIanaTimezoneCodeByCoordinateAsync(coordinate);
            return (location, coordinate, timezoneCodeResponse);
        }

        /// <summary>
        /// Searches the Timevault for documents based on the specified location.
        /// </summary>
        /// <param name="location">The location to search for.</param>
        /// <returns>An asynchronous task that represents the operation. The task result contains the response containing a list of Timevault documents.</returns>
        public async Task<ApiResponse<IList<TimevaultDocument>>> SearchTimevaultAsync(Location location)
        {
            string query = BuildLocationQueryString(location);
            return await QueryTimevaultAsync(query);
        }

        /// <summary>
        /// Queries the Timevault database asynchronously based on the provided query string.
        /// </summary>
        /// <param name="query">The query string to filter the documents in the Timevault database.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the list of <see cref="TimevaultDocument"/> objects that match the query.</returns>
        public async Task<ApiResponse<IList<TimevaultDocument>>> QueryTimevaultAsync(string query)
        {
            _logger.LogInformation($"{nameof(QueryTimevaultAsync)}: Querying Timevault database with query: {query}");

            if (string.IsNullOrEmpty(query) || string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("The query cannot be null, empty, or whitespace.", nameof(query));
            }

            try
            {
                List<TimevaultDocument> result = new List<TimevaultDocument>();
                Database database = _cosmosClient.GetDatabase(_settings.TimevaultCosmosDBDatabaseName);
                Container container = database.GetContainer(_settings.TimevaultCosmosDBContainerName);
                QueryDefinition queryDefinition = new QueryDefinition(query);
                FeedIterator<TimevaultDocument> iterator = container.GetItemQueryIterator<TimevaultDocument>(queryDefinition);

                while (iterator.HasMoreResults)
                {
                    FeedResponse<TimevaultDocument> response = await iterator.ReadNextAsync();
                    result.AddRange(response);
                }

                return ApiResponse<IList<TimevaultDocument>>.Success(result);
            }
            catch (CosmosException exception)
            {
                _logger.LogError(exception, $"An error occurred while querying the Timevault database with query {query}: {exception.Message}");
                return exception.ToApiResponse<IList<TimevaultDocument>>();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"An error occurred while querying the Timevault database with query {query}: {exception.Message}");
                throw;
            }
        }

        /// <summary>
        /// Determines whether a Timevault document requires an update based on the last IANA code update timestamp.
        /// </summary>
        /// <param name="document">The Timevault document to check.</param>
        /// <returns>True if the document requires an update, otherwise false.</returns>
        public bool TimevaultDocumentRequiresUpdate(TimevaultDocument document)
        {
            DateTime now = DateTime.UtcNow;
            DateTime lastUpdated = document.LastIanaCodeUpdateTimestamp;
            return (now - lastUpdated) >= TimeSpan.FromMinutes(_settings.IanaCodeUpdateIntervalInMinutes);
        }

        /// <summary>
        /// Attempts to update the IANA timezone code for a given Timevault document.
        /// </summary>
        /// <param name="document">The Timevault document to update.</param>
        /// <returns>The updated Timevault document.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="document"/> is null.</exception>
        public async Task<TimevaultDocument> AttemptUpdateIanaTimezoneCodeAsync(TimevaultDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            _logger.LogInformation($"{nameof(AttemptUpdateIanaTimezoneCodeAsync)}: Attempting to update IANA timezone code for Timevault document {document.Id}.");

            if (!TimevaultDocumentRequiresUpdate(document))
            {
                _logger.LogInformation($"Timevault document {document.Id} does not require an update. The IANA timezone code is up to date.");
                return document;
            }

            _logger.LogInformation($"Timevault document {document.Id} exeeded the update interval. Attempting to update the IANA timezone code.");

            GeoCoordinate coordinate = document.Coordinate;
            // Search for the IANA timezone code using the Maps API.
            // If we cannot find the timezone code, we return the document as is.
            // TODO: Maybe we want to update the timestamp here as well, as we tried to update the document,
            // and likely it will fail again in the next interval.
            ApiResponse<string> timezoneResult = await MapsGetIanaTimezoneCodeByCoordinateAsync(coordinate);
            if (!timezoneResult)
            {
                return document;
            }

            // Update the timestamp regardless of the IANA code change.
            // This way we can ensure that the document is not updated again until the next interval.
            document.LastIanaCodeUpdateTimestamp = DateTime.UtcNow;

            string timezone = timezoneResult.Value;
            if (timezone == document.IanaCode)
            {
                _logger.LogInformation($"Timevault document {document.Id} does not require an IANA timezone code update. Timestamp has been updated.");
                return document;
            }

            // If the timezone has changed, proceed with the update.
            _logger.LogInformation($"Timevault document {document.Id} requires an update. The IANA timezone code has changed from {document.IanaCode} to {timezone}.");
            document.IanaCode = timezone;
            await UpsertTimevaultAsync(document);
            return document;
        }

        /// <summary>
        /// Upserts a Timevault document asynchronously.
        /// </summary>
        /// <param name="document">The Timevault document to upsert.</param>
        /// <returns>An <see cref="ApiResponse"/> indicating the result of the upsert operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the document is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the document ID or IANA timezone code is null, empty, or whitespace.</exception>
        /// <exception cref="Exception">Thrown when an error occurs while upserting the document.</exception>
        public async Task<ApiResponse> UpsertTimevaultAsync(TimevaultDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (string.IsNullOrEmpty(document.Id) || string.IsNullOrWhiteSpace(document.Id))
            {
                throw new ArgumentException("The document ID cannot be null, empty, or whitespace.", nameof(document.Id));
            }

            if (string.IsNullOrEmpty(document.IanaCode) || string.IsNullOrWhiteSpace(document.IanaCode))
            {
                throw new ArgumentException("The IANA timezone code cannot be null, empty, or whitespace.", nameof(document.IanaCode));
            }

            _logger.LogInformation($"{nameof(UpsertTimevaultAsync)}: Upserting Timevault document: {document}");

            try
            {
                Database database = _cosmosClient.GetDatabase(_settings.TimevaultCosmosDBDatabaseName);
                Container container = database.GetContainer(_settings.TimevaultCosmosDBContainerName);
                await container.UpsertItemAsync(document, new PartitionKey(document.IanaCode));
                return ApiResponse.Success();
            }
            catch (CosmosException exception)
            {
                _logger.LogError(exception, $"An error occurred while upserting the Timevault document: {document}: {exception.Message}");
                return exception.ToApiResponse();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"An error occurred while upserting the Timevault document: {document}: {exception.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves the timezone information for a given set of coordinates.
        /// </summary>
        /// <param name="coordinate">The coordinates for which to retrieve the timezone information.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the timezone information.</returns>
        /// <exception cref="Exception">Thrown when an error occurs while searching for the timezone.</exception> 
        public async Task<ApiResponse<TimezoneResult>> MapsGetTimezoneByCoordinateAsync(GeoCoordinate coordinate)
        {
            _logger.LogInformation($"{nameof(MapsGetTimezoneByCoordinateAsync)}: Searching Azure Maps for timezone at coordinates {coordinate}.");

            IEnumerable<double> coordinates = new[] { coordinate.Latitude, coordinate.Longitude };
            try
            {
                TimezoneResult result = await _mapsTimezoneClient.GetTimezoneByCoordinatesAsync(coordinates);
                return ApiResponse<TimezoneResult>.Success(result);
            }
            catch (RequestFailedException exception)
            {
                _logger.LogError(exception, $"An error occurred while searching for timezone at coordinates {coordinate}: {exception.Message}");
                return exception.ToApiResponse<TimezoneResult>();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"An error occurred while searching for timezone at coordinates {coordinate}: {exception.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves the IANA timezone code based on the given coordinate.
        /// </summary>
        /// <param name="coordinate">The coordinate to retrieve the timezone for.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the IANA timezone code.</returns>
        public async Task<ApiResponse<string>> MapsGetIanaTimezoneCodeByCoordinateAsync(GeoCoordinate coordinate)
        {
            ApiResponse<TimezoneResult> timezoneResult = await MapsGetTimezoneByCoordinateAsync(coordinate);
            if (!timezoneResult)
            {
                return ApiResponse<string>.Error(timezoneResult.ErrorMessage, timezoneResult.StatusCode);
            }

            TimezoneResult result = timezoneResult.Value;
            if (!result.TimeZones.Any())
            {
                string message = $"No timezone found for coordinate: {coordinate}";
                _logger.LogWarning(message);
                return ApiResponse<string>.Error(message, StatusCodes.Status404NotFound);
            }

            if (result.TimeZones.Count() > 1)
            {
                _logger.LogWarning($"Expected one timezone for coordinate: {coordinate}, but found {result.TimeZones.Count()}. Using the first found.");
            }

            string timezone = result.TimeZones.First().Id;
            if (string.IsNullOrEmpty(timezone) || string.IsNullOrWhiteSpace(timezone))
            {
                string message = $"No valid timezone ID found for coordinate: {coordinate}";
                _logger.LogWarning(message);
                return ApiResponse<string>.Error(message, StatusCodes.Status404NotFound);
            }

            return ApiResponse<string>.Success(timezone);
        }

        /// <summary>
        /// Searches for an address asynchronously.
        /// </summary>
        /// <param name="query">The address query.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the search result.</returns>
        /// <exception cref="ArgumentException">Thrown when the query is null, empty, or whitespace.</exception>
        /// <exception cref="Exception">Thrown when an error occurs while searching for the address.</exception>
        public async Task<ApiResponse<SearchAddressResult>> MapsSearchAddressAsync(string query)
        {
            _logger.LogInformation($"{nameof(MapsSearchAddressAsync)}: Searching for address with query: {query}");

            if (string.IsNullOrEmpty(query) || string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("The query cannot be null, empty, or whitespace.", nameof(query));
            }

            try
            {
                SearchAddressResult searchAddressResult = await _mapsSearchClient.SearchAddressAsync(query);
                return ApiResponse<SearchAddressResult>.Success(searchAddressResult);
            }
            catch (RequestFailedException exception)
            {
                _logger.LogError(exception, $"An error occurred while searching for query {query}: {exception.Message}");
                return exception.ToApiResponse<SearchAddressResult>();
            }
            // We only catch RequestFailedException, all other erros do not belong to the service layer.
            // Therefore, we throw the exception to the caller.
            catch (Exception exception)
            {
                _logger.LogError(exception, $"An error occurred while searching for query {query}: {exception.Message}");
                throw;
            }
        }

        /// <summary>
        /// Searches for a location asynchronously based on the provided <paramref name="location"/>.
        /// </summary>
        /// <param name="location">The location to search for.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the search result.</returns>
        public async Task<ApiResponse<SearchAddressResult>> MapsSearchLocationAsync(Location location)
        {
            string query = location.MapsQueryString();
            return await MapsSearchAddressAsync(query);
        }

        /// <summary>
        /// Searches for a geographic coordinate based on the provided query.
        /// </summary>
        /// <param name="query">The search query.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the search result, or an error if the search fails.</returns>
        public async Task<ApiResponse<GeoCoordinate>> MapsSearchCoordinateAsync(string query)
        {
            ApiResponse<SearchAddressResult> searchAddressResult = await MapsSearchAddressAsync(query);
            if (!searchAddressResult)
            {
                return ApiResponse<GeoCoordinate>.Error(searchAddressResult.ErrorMessage, searchAddressResult.StatusCode);
            }

            SearchAddressResult addressResult = searchAddressResult.Value;
            if (addressResult.Results == null || !addressResult.Results.Any())
            {
                string message = $"No GeoCoordinate results found for query {query}";
                _logger.LogWarning(message);
                return ApiResponse<GeoCoordinate>.Error(message, StatusCodes.Status404NotFound);
            }

            SearchAddressResultItem bestResult = addressResult.Results.OrderByDescending(r => r.Score).First();
            GeoCoordinate coordinate = new GeoCoordinate(bestResult.Position.Latitude, bestResult.Position.Longitude);
            return ApiResponse<GeoCoordinate>.Success(coordinate);
        }

        /// <summary>
        /// Searches for a geographic coordinate using the provided location.
        /// </summary>
        /// <param name="location">The location to search for.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the search result.</returns>
        public async Task<ApiResponse<GeoCoordinate>> MapsSearchCoordinateAsync(Location location)
        {
            string query = location.MapsQueryString();
            return await MapsSearchCoordinateAsync(query);
        }

        /// <summary>
        /// Builds a query string based on the provided location object.
        /// The query string is used to filter data based on the city, state, postal code, and country of the location.
        /// </summary>
        /// <remarks>
        /// We may need to think about if we want to filter out empty or whitespace values.
        /// Because currently the filter approuch may yield multiple results if the location object contains empty or whitespace values.
        /// This is not a critical error, as we have timezone data to work with.
        /// However, do we really want this behavior?
        /// </remarks>
        /// <param name="location">The location object containing the city, state, postal code, and country.</param>
        /// <returns>The constructed query string.</returns>
        private string BuildLocationQueryString(Location location)
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