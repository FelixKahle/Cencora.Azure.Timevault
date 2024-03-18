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
using Polly;
using Polly.Retry;

namespace Cencora.Azure.Timevault
{
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
        /// Retrieves the IANA code for a given location asynchronously.
        /// If the IANA code is not found in the Timevault, it searches for the IANA code using the Maps service.
        /// </summary>
        /// <param name="location">The location for which to retrieve the IANA code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
        /// <returns>The IANA code for the given location.</returns>
        public async Task<string> GetIanaCodeByLocationAsync(Location location, CancellationToken cancellationToken = default)
        {
            IList<TimevaultDocument> documents = await SearchTimevaultAsync(location, cancellationToken);
            
            if (documents != null && documents.Any())
            {
                _logger.LogInformation($"No Timevault document found for location: {location}. Creating new document...");
                
                if (documents.Count > 1)
                {
                    _logger.LogWarning($"Found multiple Timevault documents for location: {location}. Using the first document.");
                }

                TimevaultDocument document = documents.First();
                if (RequiredIanaTimezoneCodeUpdate(document))
                {
                    document = await UpdateIanaTimezoneCodeAsync(document, cancellationToken);
                }

                return document.IanaCode;
            }
            else
            {
                GeoCoordinate coordinate = await SearchMapsGeoCoordinateAsync(location, cancellationToken);
                string ianaCode = await SearchMapsIanaTimezoneCodeAsync(coordinate, cancellationToken);

                // Create a new Timevault document for the location and IANA code.
                TimevaultDocument document = new TimevaultDocument(ianaCode, location, coordinate, DateTime.UtcNow);
                await UpsertTimevaultDocumentAsync(document, cancellationToken);

                return ianaCode;
            }
        }

        /// <summary>
        /// Searches the Timevault asynchronously based on the specified location.
        /// </summary>
        /// <param name="location">The location to search for.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of Timevault documents that match the specified location.</returns>
        public async Task<IList<TimevaultDocument>> SearchTimevaultAsync(Location location, CancellationToken cancellationToken = default)
        {
            // Build the query string based on the location.
            string query = BuildLocationQueryString(location);
            return await QueryTimevaultAsync(query, cancellationToken);
        }

        /// <summary>
        /// Searches the Timevault for documents based on the specified IANA timezone code.
        /// </summary>
        /// <param name="ianaCode">The IANA timezone code to search for.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of Timevault documents matching the specified IANA timezone code.</returns>
        public async Task<IList<TimevaultDocument>> SearchTimevaultAsync(string ianaCode, CancellationToken cancellationToken = default)
        {
            // Build the query string based on the IANA timezone code.
            string query = $"SELECT * FROM c WHERE c.ianaCode = '{ianaCode}'";
            return await QueryTimevaultAsync(query, cancellationToken);
        }

        /// <summary>
        /// Queries the Timevault service asynchronously to retrieve a list of Timevault documents based on the specified query.
        /// </summary>
        /// <param name="query">The query string used to filter the Timevault documents.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of Timevault documents.</returns>
        /// <exception cref="ArgumentException">Thrown when the query is null, empty, or whitespace.</exception>
        public async Task<IList<TimevaultDocument>> QueryTimevaultAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(query) || string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("The query cannot be null, empty, or whitespace.", nameof(query));
            }

            var retryOptions = new RetryStrategyOptions
            {
                // https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.cosmosexception.statuscode?view=azure-dotnet#microsoft-azure-cosmos-cosmosexception-statuscode
                //
                // 429: Too Many Requests
                // 449: Retry With
                // For these status codes, we want to retry the request.
                //
                // All other errors should propagate to the caller.
                ShouldHandle = new PredicateBuilder()
                    .Handle<RequestFailedException>(ex => ex.Status == 429 || ex.Status == 449)
                    .Handle<CosmosException>(ex => ex.StatusCode == (HttpStatusCode)429 || ex.StatusCode == (HttpStatusCode)449),
                MaxRetryAttempts = _settings.MaxRetryAttempts,
                Delay = TimeSpan.FromMilliseconds(_settings.RetryDelayMilliseconds),
                UseJitter = _settings.UseJitter,
                MaxDelay = TimeSpan.FromMilliseconds(_settings.MaxRetryDelayInMilliseconds),
                OnRetry = args =>
                {
                    _logger.LogWarning($"Retry request for query: {query}. Attempt: {args.AttemptNumber}");
                    return default;
                }
            };

            var pipelineBuilder = new ResiliencePipelineBuilder().AddRetry(retryOptions);
            var pipeline = pipelineBuilder.Build();

            return await pipeline.ExecuteAsync(async token =>
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

                return result;
            }, cancellationToken);
        }

        /// <summary>
        /// Determines if an update to the IANA timezone code is required for the given document.
        /// </summary>
        /// <param name="document">The Timevault document to check.</param>
        /// <returns><c>true</c> if an update is required, <c>false</c> otherwise.</returns>
        public bool RequiredIanaTimezoneCodeUpdate(TimevaultDocument document)
        {
            DateTime now = DateTime.UtcNow;
            DateTime lastUpdated = document.LastIanaCodeUpdateTimestamp;
            return (now - lastUpdated) > TimeSpan.FromMinutes(_settings.IanaCodeUpdateIntervalInMinutes);
        }

        /// <summary>
        /// Updates the IANA timezone code for a given <see cref="TimevaultDocument"/> asynchronously.
        /// </summary>
        /// <param name="document">The <see cref="TimevaultDocument"/> to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated <see cref="TimevaultDocument"/>.</returns>
        public async Task<TimevaultDocument> UpdateIanaTimezoneCodeAsync(TimevaultDocument document, CancellationToken cancellationToken = default)
        {
            GeoCoordinate coordinate = document.Coordinate;
            string ianaCode = await SearchMapsIanaTimezoneCodeAsync(coordinate);

            // Update the IANA code if it has changed, and log the update for monitoring and debugging.
            // IANA code updates are infrequent, thus tracking these changes helps in evaluating the necessity to modify update intervals.
            if (document.IanaCode != ianaCode)
            {
                _logger.LogInformation($"Updating IANA timezone code for document with id: {document.Id}. Old IANA code: {document.IanaCode}. New IANA code: {ianaCode}");
                document.IanaCode = ianaCode;
            }
            else
            {
                _logger.LogInformation($"No update required for IANA timezone code for document with id: {document.Id}. IANA code: {document.IanaCode}");
            }

            // Update the timestamp for the last IANA code update to the current UTC time.
            // This occurs regardless of whether the IANA code was changed to ensure the document's metadata is always current.
            document.LastIanaCodeUpdateTimestamp = DateTime.UtcNow;

            // Upsert the updated document to the Timevault.
            await UpsertTimevaultDocumentAsync(document, cancellationToken);

            return document;
        }

        /// <summary>
        /// Upserts a timevault document asynchronously.
        /// </summary>
        /// <param name="document">The timevault document to upsert.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the document is null.</exception>
        public async Task UpsertTimevaultDocumentAsync(TimevaultDocument document, CancellationToken cancellationToken = default)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (string.IsNullOrEmpty(document.Id) || string.IsNullOrWhiteSpace(document.Id))
            {
                throw new ArgumentException("The document id cannot be null, empty, or whitespace.", nameof(document.Id));
            }

            if (string.IsNullOrEmpty(document.IanaCode) || string.IsNullOrWhiteSpace(document.IanaCode))
            {
                throw new ArgumentException("The document IANA code cannot be null, empty, or whitespace.", nameof(document.IanaCode));
            }

            Database database = _cosmosClient.GetDatabase(_settings.TimevaultCosmosDBDatabaseName);
            Container container = database.GetContainer(_settings.TimevaultCosmosDBContainerName);

            var retryOptions = new RetryStrategyOptions
            {
                // https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.cosmosexception.statuscode?view=azure-dotnet#microsoft-azure-cosmos-cosmosexception-statuscode
                //
                // 429: Too Many Requests
                // 449: Retry With
                // For these status codes, we want to retry the request.
                //
                // All other errors should propagate to the caller.
                ShouldHandle = new PredicateBuilder()
                    .Handle<RequestFailedException>(ex => ex.Status == 429 || ex.Status == 449)
                    .Handle<CosmosException>(ex => ex.StatusCode == (HttpStatusCode)429 || ex.StatusCode == (HttpStatusCode)449),
                MaxRetryAttempts = _settings.MaxRetryAttempts,
                Delay = TimeSpan.FromMilliseconds(_settings.RetryDelayMilliseconds),
                UseJitter = _settings.UseJitter,
                MaxDelay = TimeSpan.FromMilliseconds(_settings.MaxRetryDelayInMilliseconds),
                OnRetry = args =>
                {
                    _logger.LogWarning($"Retry upsert request for document with id: {document.Id}. Attempt: {args.AttemptNumber}");
                    return default;
                }
            };

            var pipelineBuilder = new ResiliencePipelineBuilder().AddRetry(retryOptions);
            var pipeline = pipelineBuilder.Build();

            await pipeline.ExecuteAsync(async token =>
            {
                await container.UpsertItemAsync(document, new PartitionKey(document.IanaCode));
            }, cancellationToken);
        }

        /// <summary>
        /// Searches for a geographic coordinate (GeoCoordinate) based on the provided location using the Maps service.
        /// </summary>
        /// <param name="location">The location to search for.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation (optional).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the GeoCoordinate of the location.</returns>
        /// <exception cref="InvalidOperationException">Thrown when failed to retrieve coordinate information for the location.</exception>
        public async Task<GeoCoordinate> SearchMapsGeoCoordinateAsync(Location location, CancellationToken cancellationToken = default)
        {
            string query = location.MapsQueryString();
            SearchAddressResult result = await QueryMapsSearchAddressAsync(query, cancellationToken);

            if (result == null || result.Results == null || !result.Results.Any())
            {
                throw new InvalidOperationException($"Failed to retrieve coordinate information for location: {location}");
            }

            // For a location, we expect to find only one result.
            // If we find more than one result, we log a warning and use the first result.
            if (result.Results.Count() > 1)
            {
                _logger.LogWarning($"Found multiple coordinate results for location: {location}. Using the first result.");
            }

            var bestResult = result.Results.OrderByDescending(r => r.Score).FirstOrDefault();
            if (bestResult == null)
            {
                throw new InvalidOperationException($"Failed to retrieve coordinate information for location: {location}");
            }

            return new GeoCoordinate(bestResult.Position.Latitude, bestResult.Position.Longitude);
        }

        /// <summary>
        /// Queries the Azure Maps search service to retrieve address information based on the specified query.
        /// </summary>
        /// <param name="query">The search query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the search address result.</returns>
        /// <exception cref="ArgumentException">Thrown when the query is null, empty, or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when failed to retrieve address information for the query.</exception>
        public async Task<SearchAddressResult> QueryMapsSearchAddressAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(query) || string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("The query cannot be null, empty, or whitespace.", nameof(query));
            }

            var retryOptions = new RetryStrategyOptions
            {
                // https://learn.microsoft.com/en-us/azure/azure-maps/azure-maps-qps-rate-limits
                //
                // 429 - Too Many Requests. It is safe to retry the request.
                ShouldHandle = new PredicateBuilder().Handle<RequestFailedException>(ex => ex.Status == 429),
                MaxRetryAttempts = _settings.MaxRetryAttempts,
                Delay = TimeSpan.FromMilliseconds(_settings.RetryDelayMilliseconds),
                UseJitter = _settings.UseJitter,
                MaxDelay = TimeSpan.FromMilliseconds(_settings.MaxRetryDelayInMilliseconds),
                OnRetry = args =>
                {
                    _logger.LogWarning($"Retry maps search address request for query: {query}. Attempt: {args.AttemptNumber}");
                    return default;
                }
            };

            var pipelineBuilder = new ResiliencePipelineBuilder().AddRetry(retryOptions);
            var pipeline = pipelineBuilder.Build();
            SearchAddressResult result = await pipeline.ExecuteAsync(
                async token => await _mapsSearchClient.SearchAddressAsync(query), 
                cancellationToken
            );

            if (result == null)
            {
                throw new InvalidOperationException($"Failed to retrieve address information for query: {query}");
            }

            return result;
        }

        
        /// <summary>
        /// Searches for the IANA timezone code corresponding to the given coordinate using the Azure Maps API.
        /// </summary>
        /// <param name="coordinate">The coordinate for which to search the timezone.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation (optional).</param>
        /// <returns>The IANA timezone code corresponding to the given coordinate.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the timezone information for the given coordinate could not be retrieved.</exception>
        public async Task<string> SearchMapsIanaTimezoneCodeAsync(GeoCoordinate coordinate, CancellationToken cancellationToken = default)
        {
            // The Azure Maps API expects the coordinates in the format [latitude, longitude].
            IEnumerable<double> coordinates = [coordinate.Latitude, coordinate.Longitude];

            var retryOptions = new RetryStrategyOptions
            {
                // https://learn.microsoft.com/en-us/azure/azure-maps/azure-maps-qps-rate-limits
                //
                // 429 - Too Many Requests. It is safe to retry the request.
                ShouldHandle = new PredicateBuilder().Handle<RequestFailedException>(ex => ex.Status == 429),
                MaxRetryAttempts = _settings.MaxRetryAttempts,
                Delay = TimeSpan.FromMilliseconds(_settings.RetryDelayMilliseconds),
                UseJitter = _settings.UseJitter,
                MaxDelay = TimeSpan.FromMilliseconds(_settings.MaxRetryDelayInMilliseconds),
                OnRetry = args =>
                {
                    _logger.LogWarning($"Retry maps timezone request for coordinate: {coordinate}. Attempt: {args.AttemptNumber}");
                    return default;
                }
            };

            var pipelineBuilder = new ResiliencePipelineBuilder().AddRetry(retryOptions);
            var pipeline = pipelineBuilder.Build();
            TimezoneResult timezoneResult = await pipeline.ExecuteAsync(
                async token => await _mapsTimezoneClient.GetTimezoneByCoordinatesAsync(coordinates), 
                cancellationToken
            );

            // If this is true, we did not find any timezone information for the given coordinate.
            if (timezoneResult == null || timezoneResult.TimeZones == null || !timezoneResult.TimeZones.Any())
            {
                throw new InvalidOperationException($"Failed to retrieve timezone information for coordinate: {coordinate}");
            }

            // For a coordinate, we expect to find only one timezone result.
            // If we find more than one result, we log a warning and use the first result.
            if (timezoneResult.TimeZones.Count() > 1)
            {
                _logger.LogWarning($"Found multiple timezone results for coordinate: {coordinate}. Using the first result.");
            }

            // It is safe to assume that we have at least one timezone result.
            // We already checked for this case above.
            var timezone = timezoneResult.TimeZones.First().Id;

            // Check if the timezone is valid.
            if (string.IsNullOrEmpty(timezone) || string.IsNullOrWhiteSpace(timezone))
            {
                throw new InvalidOperationException($"Failed to retrieve timezone information for coordinate: {coordinate}");
            }

            return timezone;
        }

        /// <summary>
        /// Builds a query string based on the provided location object.
        /// The query string is used to filter data based on the city, state, postal code, and country of the location.
        /// </summary>
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