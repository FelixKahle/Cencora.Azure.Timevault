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

            // We now search for the Timevault documents for each location.
            // Searching is done asynchronously to improve the performance,
            // however we limit the number of concurrent requests to 20 to prevent overloading the Timevault database.
            // By default it should have a RU limit of 5000.
            // Note that 20 was some sort of stomach feeling, as we do not have any performance data to back this up.
            var searchResults = await RunWithLimitedConcurrencyAsync(
                distinctLocations,
                FetchTimevaultAsync,
                20
            );

            // Create a list to store the locations that need to be searched.
            // These are all the locations that did not have a Timevault document.
            var locationsToSearch = new List<Location>();

            // This list will hold every document that need to be updated  in the database.
            var updateDocuments = new List<(Location Location, TimevaultDocument Document)>();

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

                    updateDocuments.Add((location, document));
                }
                else
                {
                    _logger.LogInformation($"No Timevault document found for location {location}. Searching for timezone using the Maps API.");
                    locationsToSearch.Add(location);
                }
            }

            // We now update the documents that we found in the Timevault database.
            // Some of them require an update, some do not.
            // Note that we do not await the task here, as we want to update the documents in parallel to improve the performance.
            // While we update the documents, we start searching for the locations that did not have a Timevault document.
            var updatedDocumentsTask = RunWithLimitedConcurrencyAsync(
                updateDocuments,
                async keyValuePair => await UpdateDocumentAsync(keyValuePair.Location, keyValuePair.Document),
                20
            );

            // Only search for locations if we have any to search.
            if (locationsToSearch.Any())
            {
                // If we have locations to search, we search for the coordinates using the Maps API.
                // We search by utilizing the batch search functionality of the Maps API,
                // which allows us to search for multiple locations in a single request.
                // This will decrease the number of requests to the Maps API and improve the performance,
                // as we just need to wait for one request to complete.
                var coordinates = await MapsSearchCoordinateBatchAsync(locationsToSearch);

                // First of all loop through the coordinates and check if we have any errors.
                // Any error can be added to the final results and we do not need to continue with the timezone search for these locations.
                foreach (var (location, coordinateResult) in coordinates)
                {
                    if (!coordinateResult.IsSuccess)
                    {
                        finalResults.TryAdd(location, ApiResponse<string>.Error(coordinateResult.ErrorMessage, coordinateResult.StatusCode));
                    }
                }

                // We now filter the coordinates to only include the successful ones.
                var successfulCoordinates = coordinates
                    .Where(cr => cr.Value.IsSuccess)
                    .ToDictionary(cr => cr.Key, cr => cr.Value);

                // These are the fetched timezones.
                // We fetch these asynchronously to improve the performance,
                // however we limit the number of concurrent requests to 10 to prevent overloading the Maps API.
                var fetchedTimezones = await RunWithLimitedConcurrencyAsync(
                    successfulCoordinates,
                    async keyValuePair => 
                    {
                        // It is completly fine here to access the Value property directly, as we know that the dictionary
                        // only contains successful results.
                        GeoCoordinate coordinate = keyValuePair.Value.Value;
                        Location location = keyValuePair.Key;
                        return await FetchTimezoneCodeAsync(location, coordinate);
                    },
                    10
                );

                // These are all documents that need to be uploaed to the database.
                var documentToUpsert = new List<TimevaultDocument>();

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
                    documentToUpsert.Add(newDocument);
                }

                // We now upsert the new documents to the Timevault database.
                await RunWithLimitedConcurrencyAsync(
                    documentToUpsert,
                    UpsertTimevaultAsync,
                    20
                );
            }

            // Wait for the updated documents to complete.
            var updatedDocuments = await updatedDocumentsTask;

            // Finally, we update the final results with the updated documents.
            foreach (var (location, updatedDocument) in updatedDocuments)
            {
                finalResults.Add(location, ApiResponse<string>.Success(updatedDocument.IanaCode));
            }

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
        /// <remarks>
        /// Currently unused, as it has been replaced by the batch search functionality.
        /// </remarks>
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
        /// <remarks>
        /// Timezone API does not support batch processing, so we need to fetch the timezone code for each location individually.
        /// </remarks>
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
        /// Executes a collection of asynchronous operations with limited concurrency.
        /// </summary>
        /// <typeparam name="T">The type of the items in the collection.</typeparam>
        /// <typeparam name="TResult">The type of the results returned by the operations.</typeparam>
        /// <param name="items">The collection of items to process.</param>
        /// <param name="operation">The asynchronous operation to perform on each item.</param>
        /// <param name="maxConcurrency">The maximum number of concurrent operations.</param>
        /// <returns>A task representing the asynchronous operation. The task will complete when all operations have finished and return the results.</returns>
        public async Task<IEnumerable<TResult>> RunWithLimitedConcurrencyAsync<T, TResult>(
            IEnumerable<T> items,
            Func<T, Task<TResult>> operation,
            int maxConcurrency
        )
        {
            // Ensure there's a positive concurrency limit
            if (maxConcurrency <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Concurrency limit must be positive.");
            }

            // Initialize the semaphore with the desired max concurrency
            var semaphore = new SemaphoreSlim(maxConcurrency);
            var tasks = new List<Task<TResult>>();

            // Launch tasks with limited concurrency
            foreach (var item in items)
            {
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        return await operation(item);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            // Wait for all tasks to complete and return the results
            return await Task.WhenAll(tasks);
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
                string errorMessage = searchAddressResult.ErrorMessage ?? $"An error occurred while searching for coordinate with query {query}";
                return ApiResponse<GeoCoordinate>.Error(errorMessage, searchAddressResult.StatusCode);
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
        /// Searches for addresses in batch based on the provided query strings.
        /// </summary>
        /// <param name="queryStrings">The collection of query strings.</param>
        /// <returns>An asynchronous task that represents the operation and contains the search results.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="queryStrings"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="queryStrings"/> is empty.</exception>
        public async Task<ApiResponse<SearchAddressBatchOperation>> MapsSearchAddressBatchAsync(IEnumerable<string> queryStrings)
        {
            if (queryStrings == null)
            {
                throw new ArgumentNullException(nameof(queryStrings));
            }

            if (!queryStrings.Any())
            {
                throw new ArgumentException("The queries collection cannot be empty.", nameof(queryStrings));
            }

            _logger.LogInformation($"{nameof(MapsSearchAddressAsync)}: Searching for addresses in batch with {queryStrings.Count()} queries.");

            try
            {
                var queries = queryStrings.Distinct().Select(q => new SearchAddressQuery(q));

                // The batch search operation only supports up to 100 queries.
                // When we are below or equal to 100 queries, we can use the synchronous method, as this will be faster
                // than the asynchronous method.
                // When we are above 100 queries, we need to use the asynchronous method, as this will be faster than due to the 
                // large number of queries.
                if (queries.Count() <= 100)
                {
                    SearchAddressBatchOperation batchResponse = _mapsSearchClient.SearchAddressBatch(WaitUntil.Completed, queries);
                    return ApiResponse<SearchAddressBatchOperation>.Success(batchResponse);
                }
                else
                {
                    SearchAddressBatchOperation batchResponse = await _mapsSearchClient.SearchAddressBatchAsync(WaitUntil.Completed, queries);
                    return ApiResponse<SearchAddressBatchOperation>.Success(batchResponse);
                }
            }
            catch (RequestFailedException exception)
            {
                _logger.LogError(exception, $"An error occurred while searching for addresses in batch: {exception.Message}");
                return exception.ToApiResponse<SearchAddressBatchOperation>();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"An error occurred while searching for addresses in batch: {exception.Message}");
                throw;
            }
        }

        /// <summary>
        /// Searches for addresses using the Maps service asynchronously.
        /// </summary>
        /// <param name="locations">The collection of locations to search for.</param>
        /// <returns>An asynchronous task that represents the operation. The task result contains the search address batch operation response.</returns>
        public async Task<ApiResponse<SearchAddressBatchOperation>> MapsSearchAddressBatchAsync(IEnumerable<Location> locations)
        {
            var queryStrings = locations.Distinct().Select(l => l.MapsQueryString());
            return await MapsSearchAddressBatchAsync(queryStrings);
        }

        /// <summary>
        /// Maps a collection of query strings to a dictionary of search results using the Maps API.
        /// </summary>
        /// <param name="queryStrings">The collection of query strings to search for.</param>
        /// <returns>A dictionary where the key is the query string and the value is the search result.</returns>
        public async Task<IDictionary<string, ApiResponse<SearchAddressBatchItemResponse>>> MapsSearchAddressBatchToDictionaryAsync(IEnumerable<string> queryStrings)
        {
            // This will hold the original query strings and the transformed query strings.
            // https://learn.microsoft.com/en-us/rest/api/maps/search/post-search-address-batch?view=rest-maps-1.0&tabs=HTTP
            // Azure Maps will transform the query strings to lowercase and remove any commas.
            // For example, "One, Microsoft Way, Redmond, WA 98052" will be transformed to "one microsoft way redmond wa 98052".
            // Therefore we need to transform the query strings to lowercase and remove any commas as well and remeber the original query strings.
            // Using this Dictionary we can match the original query strings with the search results.
            Dictionary<string, string> queries = queryStrings.Distinct().ToDictionary(q => q, q => q.Replace(",", string.Empty).ToLower());

            // Query the Maps API for the coordinates of the provided query strings in batch.
            ApiResponse<SearchAddressBatchOperation> searchResult = await MapsSearchAddressBatchAsync(queries.Keys);

            // If an error occurred while searching for the coordinates, return an error response for each query string.
            // As the whole operation failed, each query string will have the same error response.
            // The error response contains the error message and status code.
            // We have the error message and status code from the search result, so we can use them here.
            if (!searchResult.IsSuccess)
            {
                return queries.ToDictionary(q => q.Key, q => ApiResponse<SearchAddressBatchItemResponse>.Error(
                    searchResult.ErrorMessage,
                    searchResult.StatusCode
                ));
            }

            // Its safe to access the value here, as we checked for the error response above.
            SearchAddressBatchOperation batchOperation = searchResult.Value;

            // If no search results were found for the query strings, return an error response for each query string.
            // We cannot use the error response from the search result, as it succeeded, but did not find any results.
            // Again, each query string will have the same error response, as not a single search result was found.
            // Here we do not have the error message and status code, so we use a default message and status code.
            if (!batchOperation.HasValue || batchOperation.Value == null || batchOperation.Value.Results == null || !batchOperation.Value.Results.Any())
            {
                string message = $"No search results found for {queryStrings.Distinct().Count()} queries in batch.";
                _logger.LogWarning(message);

                return queries.ToDictionary(q => q.Key, q => ApiResponse<SearchAddressBatchItemResponse>.Error(
                    message,
                    StatusCodes.Status404NotFound
                ));
            }

            // We have found search results for the query strings.
            // We now can match the query strings with the search results.
            // We return a dictionary where the key is the query string and the value is the search result.
            var results = queries
                .Select(kvp =>
                {
                    // Get the original and transformed query strings.
                    string originalQuery = kvp.Key;
                    string transformedQuery = kvp.Value;

                    // Attempt to find a matching result using the transformed query.
                    var match = batchOperation.Value.Results.FirstOrDefault(r => r.Query == transformedQuery);

                    // Should never happen, but its good to have a safeguard here.
                    if (match == null)
                    {
                        string message = $"No result found for query {originalQuery}";
                        _logger.LogWarning(message);

                        return (Query: originalQuery, ApiResponse: ApiResponse<SearchAddressBatchItemResponse>.Error(message, StatusCodes.Status404NotFound));
                    }

                    return (Query: originalQuery, ApiResponse: ApiResponse<SearchAddressBatchItemResponse>.Success(match));
                })
                .ToDictionary(x => x.Query, x => x.ApiResponse);
            return results;
        }

        /// <summary>
        /// Maps a collection of locations to a dictionary of search address batch item responses.
        /// </summary>
        /// <param name="locations">The collection of locations to map.</param>
        /// <returns>A dictionary containing the mapped locations and their corresponding search address batch item responses.</returns>
        public async Task<IDictionary<Location, ApiResponse<SearchAddressBatchItemResponse>>> MapsSearchAddressBatchToDictionaryAsync(IEnumerable<Location> locations)
        {
            Dictionary<Location, string> locationQueries = locations.Distinct().ToDictionary(l => l, l => l.MapsQueryString());
            IDictionary<string, ApiResponse<SearchAddressBatchItemResponse>> queryResults = await MapsSearchAddressBatchToDictionaryAsync(locationQueries.Values);

            // We can safely assume that every query has a corresponding API response,
            // regardless of whether the response is an error or a success.
            var results = locationQueries
                .Join(
                    queryResults,
                    locationQuery => locationQuery.Value,
                    result => result.Key,
                    (locationQuery, result) => new { Location = locationQuery.Key, Response = result.Value }
                )
                .ToDictionary(x => x.Location, x => x.Response);
            return results;
        }

        /// <summary>
        /// Searches for coordinates in batch based on the provided query strings.
        /// </summary>
        /// <param name="queryStrings">The collection of query strings to search for.</param>
        /// <returns>An asynchronous task that represents the operation. The task result contains the search results.</returns>
        public async Task<IDictionary<string, ApiResponse<GeoCoordinate>>> MapsSearchCoordinateBatchAsync(IEnumerable<string> queryStrings)
        {
            IDictionary<string, ApiResponse<SearchAddressBatchItemResponse>> searchResults = await MapsSearchAddressBatchToDictionaryAsync(queryStrings);

            var results = searchResults
                .Select(kvp =>
                {
                    string query = kvp.Key;
                    ApiResponse<SearchAddressBatchItemResponse> searchResult = kvp.Value;

                    // We have an error response, return an error response for the query.
                    // The message and status code are already set in the error response,
                    // so we can use them here.
                    if (!searchResult.IsSuccess)
                    {
                        return (Query: query, ApiResponse: ApiResponse<GeoCoordinate>.Error(searchResult.ErrorMessage, searchResult.StatusCode));
                    }

                    // Safe to access the value here, as we checked for the error response above.
                    SearchAddressBatchItemResponse result = searchResult.Value;

                    // Check if we have any results for the query.
                    // If not we return an error response for the query.
                    // As we do not have a error message and status code here, we use a default message and status code.
                    var results = result.Results;
                    if (results == null || !results.Any())
                    {
                        string message = $"No coordinate found for query {query}";
                        _logger.LogWarning(message);
                        return (Query: query, ApiResponse: ApiResponse<GeoCoordinate>.Error(message, StatusCodes.Status404NotFound));
                    }

                    var bestResult = results.OrderByDescending(r => r.Score).First();
                    var coordinate = new GeoCoordinate(bestResult.Position.Latitude, bestResult.Position.Longitude);
                    return (Query: query, ApiResponse: ApiResponse<GeoCoordinate>.Success(coordinate));
                })
                .ToDictionary(x => x.Query, x => x.ApiResponse);
            return results;
        }

        /// <summary>
        /// Searches for the coordinates of multiple locations in batch using the Maps API.
        /// </summary>
        /// <param name="locations">The collection of locations to search for.</param>
        /// <returns>A dictionary containing the location and the corresponding API response with the coordinates.</returns>
        public async Task<IDictionary<Location, ApiResponse<GeoCoordinate>>> MapsSearchCoordinateBatchAsync(IEnumerable<Location> locations)
        {
            Dictionary<Location, string> locationQueries = locations.Distinct().ToDictionary(l => l, l => l.MapsQueryString());
            IDictionary<string, ApiResponse<GeoCoordinate>> queryResults = await MapsSearchCoordinateBatchAsync(locationQueries.Values);
            return locationQueries
                .Join(
                    queryResults,
                    locationQuery => locationQuery.Value,
                    result => result.Key,
                    (locationQuery, result) => new { Location = locationQuery.Key, Response = result.Value }
                )
                .ToDictionary(x => x.Location, x => x.Response);
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