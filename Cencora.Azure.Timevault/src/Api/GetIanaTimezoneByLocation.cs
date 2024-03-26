// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Cencora.Azure.Timevault
{
    /// <summary>
    /// Represents the response returned by the GetIanaTimezoneByLocation API.
    /// </summary>
    internal struct GetIanaTimezoneByLocationResponse : IEquatable<GetIanaTimezoneByLocationResponse>
    {
        /// <summary>
        /// Gets or sets the location associated with the response.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Location? Location { get; set; }

        /// <summary>
        /// Gets or sets the IANA timezone associated with the response.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? IanaTimezone { get; set; }

        /// <summary>
        /// Gets or sets the status code of the response.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the error message associated with the response.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets a value indicating whether the request was successful.
        /// </summary>
        [JsonIgnore]
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;

        /// <summary>
        /// Determines whether the current instance is equal to another <see cref="GetIanaTimezoneByLocationResponse"/> object.
        /// </summary>
        /// <param name="other">The <see cref="GetIanaTimezoneByLocationResponse"/> to compare with the current instance.</param>
        /// <returns><c>true</c> if the current instance is equal to the other object; otherwise, <c>false</c>.</returns>
        public bool Equals(GetIanaTimezoneByLocationResponse other)
        {
            return string.Equals(IanaTimezone, other.IanaTimezone)
                && string.Equals(ErrorMessage, other.ErrorMessage)
                && StatusCode == other.StatusCode
                && Location.Equals(other.Location);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is GetIanaTimezoneByLocationResponse response && Equals(response);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Location, IanaTimezone, StatusCode, ErrorMessage);
        }

        /// <summary>
        /// Determines whether two <see cref="GetIanaTimezoneByLocationResponse"/> objects are equal.
        /// </summary>
        /// <param name="left">The first <see cref="GetIanaTimezoneByLocationResponse"/> to compare.</param>
        /// <param name="right">The second <see cref="GetIanaTimezoneByLocationResponse"/> to compare.</param>
        /// <returns><c>true</c> if the two objects are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(GetIanaTimezoneByLocationResponse left, GetIanaTimezoneByLocationResponse right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="GetIanaTimezoneByLocationResponse"/> objects are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="GetIanaTimezoneByLocationResponse"/> to compare.</param>
        /// <param name="right">The second <see cref="GetIanaTimezoneByLocationResponse"/> to compare.</param>
        /// <returns><c>true</c> if the two objects are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(GetIanaTimezoneByLocationResponse left, GetIanaTimezoneByLocationResponse right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Creates a new <see cref="GetIanaTimezoneByLocationResponse"/> object representing a successful response.
        /// </summary>
        /// <param name="ianaTimezone">The timezone.</param>
        /// <param name="location">The location.</param>
        /// <returns>The new <see cref="GetIanaTimezoneByLocationResponse"/></returns>
        public static GetIanaTimezoneByLocationResponse Success(string ianaTimezone, Location location, int statusCode = StatusCodes.Status200OK)
        {
            return new GetIanaTimezoneByLocationResponse
            {
                IanaTimezone = ianaTimezone,
                Location = location,
                StatusCode = statusCode,
            };
        }

        /// <summary>
        /// Creates a new <see cref="GetIanaTimezoneByLocationResponse"/> object representing an error response.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="location">The location.</param>
        /// <param name="statusCode">The status code.</param>
        /// <returns>The new <see cref="GetIanaTimezoneByLocationResponse"/></returns>
        public static GetIanaTimezoneByLocationResponse Error(string? errorMessage, Location? location, int statusCode = StatusCodes.Status500InternalServerError)
        {
            return new GetIanaTimezoneByLocationResponse
            {
                ErrorMessage = errorMessage,
                StatusCode = statusCode,
                Location = location,
            };
        }
    }

    /// <summary>
    /// Represents a class that retrieves the timezone based on the provided location information.
    /// </summary>
    public class GetIanaTimezoneByLocation
    {
        /// <summary>
        /// The logger instance.
        /// </summary>
        private readonly ILogger<GetIanaTimezoneByLocation> _logger;

        /// <summary>
        /// The timevault service instance.
        /// </summary>
        private readonly ITimevaultService _timevaultService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetIanaTimezoneByLocation"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="timevaultService">The timevault service instance.</param>
        public GetIanaTimezoneByLocation(ILogger<GetIanaTimezoneByLocation> logger, ITimevaultService timevaultService)
        {
            _logger = logger;
            _timevaultService = timevaultService;
        }

        /// <summary>
        /// Retrieves the IANA timezone for a given location based on the provided parameters.
        /// </summary>
        /// <param name="req">The HTTP request containing the location parameters.</param>
        /// <returns>An <see cref="IActionResult"/> representing the result of the operation.</returns>
        /// <remarks>
        /// This function is an Azure Function triggered by an HTTP GET request. It expects the following query parameters:
        /// - city: The name of the city.
        /// - state: The name of the state.
        /// - postalCode: The postal code of the location.
        /// - country: The name of the country.
        /// 
        /// At least one of the location parameters (city, state, postalCode, country) must be provided.
        /// 
        /// The function tries to retrieve the IANA timezone for the provided location using the <see cref="_timevaultService"/>.
        /// If successful, it returns an <see cref="OkObjectResult"/> with the IANA timezone and the location.
        /// If the location is not found, it returns a <see cref="NotFoundObjectResult"/> with an error message and status code.
        /// If an error occurs during the process, it returns a <see cref="ObjectResult"/> with an error message and status code 500.
        /// </remarks>
        [Function("GetIanaTimezoneByLocation")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "timezone/byLocation")] HttpRequest req)
        {
            string cityString = req.Query["city"].ToString().ToLower();
            string stateString = req.Query["state"].ToString().ToLower();
            string postalCodeString = req.Query["postalCode"].ToString().ToLower();
            string countryString = req.Query["country"].ToString().ToLower();

            // Ensure that at least one of the location parameters is provided.
            if (string.IsNullOrEmpty(cityString)
                    && string.IsNullOrEmpty(stateString)
                    && string.IsNullOrEmpty(postalCodeString)
                    && string.IsNullOrEmpty(countryString))
            {
                _logger.LogWarning("At least one of the location parameters (city, state, postalCode, country) must be provided.");

                var errorResponse = GetIanaTimezoneByLocationResponse.Error(
                    "At least one of the location parameters (city, state, postalCode, country) must be provided.",
                    null,
                    StatusCodes.Status400BadRequest
                );
                return new BadRequestObjectResult(errorResponse);
            }

            // The strings can not be null, so we can create a new location object.
            // <see query.ToString() returns an empty string if the query parameter is not provided.
            Location location = new Location(cityString, stateString, countryString, postalCodeString);

            try
            {
                // Retrieve the IANA timezone for the provided location.
                ApiResponse<string> ianaTimezoneResponse = await _timevaultService.GetIanaCodeByLocationAsync(location);
                if (!ianaTimezoneResponse.IsSuccess)
                {
                    GetIanaTimezoneByLocationResponse errorResponse = GetIanaTimezoneByLocationResponse.Error(
                        ianaTimezoneResponse.ErrorMessage,
                        location,
                        ianaTimezoneResponse.StatusCode
                    );
                    return new ObjectResult(errorResponse)
                    {
                        StatusCode = ianaTimezoneResponse.StatusCode,
                    };
                }

                string ianaTimezone = ianaTimezoneResponse.Value;
                return new OkObjectResult(GetIanaTimezoneByLocationResponse.Success(ianaTimezone, location));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving the IANA timezone for the provided location {location}: {ex.Message}");

                return new ObjectResult(GetIanaTimezoneByLocationResponse.Error(
                    "An error occurred while retrieving the IANA timezone for the provided location.",
                    location
                ))
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                };
            }
        }
    }

    /// <summary>
    /// Represents a request item for retrieving the IANA timezone by location.
    /// </summary>
    internal struct GetIanaTimezoneByLocationBatchRequestItem
    {
        /// <summary>
        /// Gets or sets the city name.
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// Gets or sets the state name.
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Gets or sets the postal code.
        /// </summary>
        public string? PostalCode { get; set; }

        /// <summary>
        /// Gets or sets the country name.
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// Converts the request item to a <see cref="Location"/> object.
        /// </summary>
        /// <returns>The <see cref="Location"/> object.</returns>
        public Location ToLocation()
        {
            return new Location(
                City?.ToLower() ?? string.Empty, 
                State?.ToLower() ?? string.Empty, 
                Country?.ToLower() ?? string.Empty, 
                PostalCode?.ToLower() ?? string.Empty);
        }
    }

    /// <summary>
    /// Represents a batch operation to retrieve IANA timezones by location.
    /// </summary>
    public class GetIanaTimezoneByLocationBatch
    {
        /// <summary>
        /// The logger instance.
        /// </summary>
        private readonly ILogger<GetIanaTimezoneByLocationBatch> _logger;

        /// <summary>
        /// The timevault service instance.
        /// </summary>
        private readonly ITimevaultService _timevaultService;

        /// <summary>
        /// The JSON serializer options.
        /// </summary>
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetIanaTimezoneByLocationBatch"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="timevaultService">The timevault service instance.</param>
        public GetIanaTimezoneByLocationBatch(ILogger<GetIanaTimezoneByLocationBatch> logger, ITimevaultService timevaultService)
        {
            _logger = logger;
            _timevaultService = timevaultService;
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
        }

        /// <summary>
        /// Executes the Azure Function to retrieve the IANA timezones by location in batch.
        /// </summary>
        /// <param name="req">The HTTP request containing the location data.</param>
        /// <returns>An <see cref="IActionResult"/> representing the HTTP response.</returns>
        [Function("GetIanaTimezoneByLocationBatch")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "timezone/byLocationBatch")] HttpRequest req)
        {
            // Deserialize the request body into a list of location items.
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<List<GetIanaTimezoneByLocationBatchRequestItem>>(requestBody, _jsonSerializerOptions);

            // Safe guard against null or empty data.
            if (data == null || data.Count == 0)
            {
                _logger.LogWarning("The request body must contain at least one location item.");

                var errorResponse = GetIanaTimezoneByLocationResponse.Error(
                    "The request body must contain at least one location item.",
                    null,
                    StatusCodes.Status400BadRequest
                );
                return new BadRequestObjectResult(errorResponse);
            }

            // Filter out invalid locations and convert them to Location objects.
            List<Location> locations = data
                .Where(item =>
                {
                    return !(string.IsNullOrEmpty(item.City)
                        && string.IsNullOrEmpty(item.State)
                        && string.IsNullOrEmpty(item.PostalCode)
                        && string.IsNullOrEmpty(item.Country));
                })
                .Select(item => item.ToLocation())
                .ToList();

            // Ensure that at least one valid location is provided.
            if (!locations.Any())
            {
                _logger.LogWarning("No valid locations provided in the request.");

                return new BadRequestObjectResult(GetIanaTimezoneByLocationResponse.Error(
                    "No valid locations provided in the request.",
                    null,
                    StatusCodes.Status400BadRequest
                ));
            }

            try
            {
                // Retrieve the IANA timezone for each location in the batch.
                // The results are stored in a dictionary where the key is the location and the value is the API response.
                // Then we convert the results into a list of GetIanaTimezoneByLocationResponse objects.
                IDictionary<Location, ApiResponse<string>> results = await _timevaultService.GetIanaCodeByLocationBatchAsync(locations);
                List<GetIanaTimezoneByLocationResponse> responses = results.Select(pair =>
                {
                    Location location = pair.Key;
                    ApiResponse<string> response = pair.Value;
                    if (response.IsSuccess)
                    {
                        return GetIanaTimezoneByLocationResponse.Success(response.Value, location);
                    }
                    else
                    {
                        return GetIanaTimezoneByLocationResponse.Error(response.ErrorMessage, location, response.StatusCode);
                    }
                }).ToList();

                return new OkObjectResult(responses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while processing the batch timezone retrieval: {ex.Message}");

                return new ObjectResult(GetIanaTimezoneByLocationResponse.Error(
                    "An unexpected error occurred while processing the request.", 
                    null, 
                    StatusCodes.Status500InternalServerError))
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}