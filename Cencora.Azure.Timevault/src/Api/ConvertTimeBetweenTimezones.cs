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
    /// Represents the response object for converting time between timezones.
    /// </summary>
    internal struct ConvertTimeBetweenTimezonesResponse : IEquatable<ConvertTimeBetweenTimezonesResponse>
    {
        /// <summary>
        /// Gets or sets the location from which the time is being converted.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Location? FromLocation { get; set; }

        /// <summary>
        /// Gets or sets the location to convert the time to.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Location? ToLocation { get; set; }

        /// <summary>
        /// Gets or sets the starting time for the conversion.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? FromTime { get; set; }

        /// <summary>
        /// Gets or sets the target time in the destination timezone.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? ToTime { get; set; }

        /// <summary>
        /// Gets or sets the error message associated with the conversion between timezones.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets a value indicating whether the operation was successful.
        /// </summary>
        [JsonIgnore]
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;

        /// <summary>
        /// Determines whether the current <see cref="ConvertTimeBetweenTimezonesResponse"/> object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.</returns>
        public bool Equals(ConvertTimeBetweenTimezonesResponse other)
        {
            return FromLocation == other.FromLocation &&
                   ToLocation == other.ToLocation &&
                   FromTime == other.FromTime &&
                   ToTime == other.ToTime &&
                   string.Equals(ErrorMessage, other.ErrorMessage) &&
                   StatusCode == other.StatusCode;
        }

        /// <summary>
        /// Determines whether the current object is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>
        ///   <c>true</c> if the current object is equal to the <paramref name="obj"/> parameter; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object? obj)
        {
            return obj is ConvertTimeBetweenTimezonesResponse other && Equals(other);
        }

        /// <summary>
        /// Computes a hash code for the current instance.
        /// </summary>
        /// <returns>A hash code for the current instance.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(FromLocation, ToLocation, FromTime, ToTime, ErrorMessage, StatusCode);
        }

        /// <summary>
        /// Determines whether two <see cref="ConvertTimeBetweenTimezonesResponse"/> objects are equal.
        /// </summary>
        /// <param name="left">The first <see cref="ConvertTimeBetweenTimezonesResponse"/> object to compare.</param>
        /// <param name="right">The second <see cref="ConvertTimeBetweenTimezonesResponse"/> object to compare.</param>
        /// <returns><c>true</c> if the two objects are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(ConvertTimeBetweenTimezonesResponse? left, ConvertTimeBetweenTimezonesResponse? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two instances of the <see cref="ConvertTimeBetweenTimezonesResponse"/> class are not equal.
        /// </summary>
        /// <param name="left">The left instance to compare.</param>
        /// <param name="right">The right instance to compare.</param>
        /// <returns><c>true</c> if the instances are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(ConvertTimeBetweenTimezonesResponse? left, ConvertTimeBetweenTimezonesResponse? right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Creates a successful response object for converting time between timezones.
        /// </summary>
        /// <param name="fromLocation">The location from which the time is being converted.</param>
        /// <param name="toLocation">The location to convert the time to.</param>
        /// <param name="fromTime">The starting time for the conversion.</param>
        /// <param name="toTime">The target time in the destination timezone.</param>
        /// <param name="statusCode">The status code.</param>
        /// <returns>A successful response object for converting time between timezones.</returns>
        public static ConvertTimeBetweenTimezonesResponse Success(Location? fromLocation, Location? toLocation, DateTime? fromTime, DateTime? toTime, int statusCode = StatusCodes.Status200OK)
        {
            return new ConvertTimeBetweenTimezonesResponse
            {
                FromLocation = fromLocation,
                ToLocation = toLocation,
                FromTime = fromTime,
                ToTime = toTime,
                StatusCode = statusCode
            };
        }

        /// <summary>   
        /// Creates an error response object for converting time between timezones.
        /// </summary>
        /// <param name="errorMessage">The error message associated with the conversion between timezones.</param>
        /// <param name="statusCode">The status code.</param>
        /// <returns>An error response object for converting time between timezones.</returns>
        public static ConvertTimeBetweenTimezonesResponse Error(string? errorMessage, Location? fromLocation, Location? toLocation, int statusCode = StatusCodes.Status500InternalServerError)
        {
            return new ConvertTimeBetweenTimezonesResponse
            {
                ErrorMessage = errorMessage,
                FromLocation = fromLocation,
                ToLocation = toLocation,
                StatusCode = statusCode
            };
        }
    }

    /// <summary>
    /// Represents a class that converts the time between two timezones based on the provided location and time parameters.
    /// </summary>
    public class ConvertTimeBetweenTimezones
    {
        /// <summary>
        /// The logger instance.
        /// </summary>
        private readonly ILogger<ConvertTimeBetweenTimezones> _logger;

        /// <summary>
        /// The timevault service instance.
        /// </summary>
        private readonly ITimevaultService _timevaultService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertTimeBetweenTimezones"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="timevaultService">The timevault service instance.</param>
        public ConvertTimeBetweenTimezones(ILogger<ConvertTimeBetweenTimezones> logger, ITimevaultService timevaultService)
        {
            _logger = logger;
            _timevaultService = timevaultService;
        }

        /// <summary>
        /// Converts the time between two timezones based on the provided location and time parameters.
        /// </summary>
        /// <param name="req">The HTTP request containing the location and time parameters.</param>
        /// <returns>An <see cref="IActionResult"/> representing the result of the conversion.</returns>
        [Function("ConvertTimeBetweenTimezones")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "time/convert")] HttpRequest req)
        {
            string fromCityString = req.Query["fromCity"].ToString().ToLower();
            string fromStateString = req.Query["fromState"].ToString().ToLower();
            string fromPostalCodeString = req.Query["fromPostalCode"].ToString().ToLower();
            string fromCountryString = req.Query["fromCountry"].ToString().ToLower();
            string fromTimeString = req.Query["fromTime"].ToString();

            string toCityString = req.Query["toCity"].ToString().ToLower();
            string toStateString = req.Query["toState"].ToString().ToLower();
            string toPostalCodeString = req.Query["toPostalCode"].ToString().ToLower();
            string toCountryString = req.Query["toCountry"].ToString().ToLower();

            if (string.IsNullOrEmpty(fromCityString) && string.IsNullOrEmpty(fromStateString) && string.IsNullOrEmpty(fromPostalCodeString) && string.IsNullOrEmpty(fromCountryString))
            {
                _logger.LogWarning("At least one of the 'from' location parameters must be provided.");

                var errorResponse = ConvertTimeBetweenTimezonesResponse.Error(
                    "At least one of the 'from' location parameters must be provided.",
                    null,
                    null,
                    StatusCodes.Status400BadRequest
                );
                return new BadRequestObjectResult(errorResponse);
            }

            if (string.IsNullOrEmpty(toCityString) && string.IsNullOrEmpty(toStateString) && string.IsNullOrEmpty(toPostalCodeString) && string.IsNullOrEmpty(toCountryString))
            {
                _logger.LogWarning("At least one of the 'to' location parameters must be provided.");

                var errorResponse = ConvertTimeBetweenTimezonesResponse.Error(
                    "At least one of the 'to' location parameters must be provided.",
                    null,
                    null,
                    StatusCodes.Status400BadRequest
                );
                return new BadRequestObjectResult(errorResponse);
            }

            Location fromLocation = new Location(fromCityString, fromStateString, fromPostalCodeString, fromCountryString);
            Location toLocation = new Location(toCityString, toStateString, toPostalCodeString, toCountryString);

            if (string.IsNullOrEmpty(fromTimeString))
            {
                _logger.LogWarning("The 'fromTime' parameter must be provided.");

                var errorResponse = ConvertTimeBetweenTimezonesResponse.Error(
                    "The 'fromTime' parameter must be provided.",
                    fromLocation,
                    toLocation,
                    StatusCodes.Status400BadRequest
                );
                return new BadRequestObjectResult(errorResponse);
            }

            DateTime dateTime;
            if (!DateTime.TryParse(fromTimeString, out dateTime))
            {
                _logger.LogWarning($"The 'fromTime' parameter is not a valid date and time: {fromTimeString}. Please provide a valid date in ISO 8601 format.");

                var errorResponse = ConvertTimeBetweenTimezonesResponse.Error(
                    "The 'fromTime' parameter is not a valid date and time. Please provide a valid date in ISO 8601 format.",
                    fromLocation,
                    toLocation,
                    StatusCodes.Status400BadRequest
                );
                return new BadRequestObjectResult(errorResponse);
            }

            try
            {
                ApiResponse<string> fromTimezoneResponse = await _timevaultService.GetIanaCodeByLocationAsync(fromLocation);
                if (!fromTimezoneResponse.IsSuccess)
                {
                    var errorResponse = ConvertTimeBetweenTimezonesResponse.Error(
                        fromTimezoneResponse.ErrorMessage ?? $"No timezone found for the provided location: {fromLocation}",
                        fromLocation,
                        toLocation,
                        fromTimezoneResponse.StatusCode
                    );
                    return new ObjectResult(errorResponse)
                    {
                        StatusCode = errorResponse.StatusCode
                    };
                }

                ApiResponse<string> toTimezoneResponse = await _timevaultService.GetIanaCodeByLocationAsync(toLocation);
                if (!toTimezoneResponse.IsSuccess)
                {
                    var errorResponse = ConvertTimeBetweenTimezonesResponse.Error(
                        toTimezoneResponse.ErrorMessage ?? $"No timezone found for the provided location: {toLocation}",
                        fromLocation,
                        toLocation,
                        toTimezoneResponse.StatusCode
                    );
                    return new ObjectResult(errorResponse)
                    {
                        StatusCode = errorResponse.StatusCode
                    };
                }

                string fromTimezone = fromTimezoneResponse.Value;
                string toTimezone = toTimezoneResponse.Value;

                // Parse the provided time string
                TimeZoneInfo sourceTimeZone = TimeZoneInfo.FindSystemTimeZoneById(fromTimezone);
                TimeZoneInfo targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById(toTimezone);

                // Assuming dateTime represents a local time in the source timezone
                // First, ensure dateTime is associated with the source timezone
                DateTime sourceDateTime;
                if (dateTime.Kind == DateTimeKind.Unspecified)
                {
                    // Convert 'unspecified' DateTime to 'local' DateTime in source timezone
                    sourceDateTime = TimeZoneInfo.ConvertTime(dateTime, sourceTimeZone, sourceTimeZone);
                }
                else
                {
                    // For 'Utc' or 'Local' kinds, directly convert to source timezone
                    sourceDateTime = TimeZoneInfo.ConvertTime(dateTime, sourceTimeZone);
                }

                // Now, convert the DateTime from the source timezone to the target timezone
                DateTime targetDateTime = TimeZoneInfo.ConvertTime(sourceDateTime, sourceTimeZone, targetTimeZone);

                return new OkObjectResult(ConvertTimeBetweenTimezonesResponse.Success(fromLocation, toLocation, sourceDateTime, targetDateTime));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while converting time between timezones: {ex.Message}");

                return new ObjectResult(ConvertTimeBetweenTimezonesResponse.Error(
                    "An error occurred while converting time between timezones.",
                    fromLocation,
                    toLocation
                ))
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }

    /// <summary>
    /// Represents a batch request for converting time between timezones.
    /// </summary>
    internal struct ConvertTimeBetweenTimezonesBatchRequest
    {
        /// <summary>
        /// Gets or sets the city of the location from which the time is being converted.
        /// </summary>
        public string? FromCity { get; set; }

        /// <summary>
        /// Gets or sets the state of the location from which the time is being converted.
        /// </summary>
        public string? FromState { get; set; }

        /// <summary>
        /// Gets or sets the postal code of the location from which the time is being converted.
        /// </summary>
        public string? FromPostalCode { get; set; }

        /// <summary>
        /// Gets or sets the country of the location from which the time is being converted.
        /// </summary>
        public string? FromCountry { get; set; }

        /// <summary>
        /// Gets or sets the city of the location to convert the time to.
        /// </summary>
        public string? ToCity { get; set; }

        /// <summary>
        /// Gets or sets the state of the location to convert the time to.
        /// </summary>
        public string? ToState { get; set; }

        /// <summary>
        /// Gets or sets the postal code of the location to convert the time to.
        /// </summary>
        public string? ToPostalCode { get; set; }

        /// <summary>
        /// Gets or sets the country of the location to convert the time to.
        /// </summary>
        public string? ToCountry { get; set; }

        /// <summary>
        /// Gets or sets the starting time for the conversion.
        /// </summary>
        public string? FromTime { get; set; }

        /// <summary>
        /// Checks if the from location is valid.
        /// </summary>
        /// <returns><c>true</c> if the from location is valid; otherwise, <c>false</c>.</returns>
        public bool IsFromLocationValid()
        {
            return !(string.IsNullOrEmpty(FromCity)
                && string.IsNullOrEmpty(FromState)
                && string.IsNullOrEmpty(FromPostalCode)
                && string.IsNullOrEmpty(FromCountry));
        }

        /// <summary>
        /// Checks if the destination location is valid.
        /// </summary>
        /// <returns><c>true</c> if the destination location is valid; otherwise, <c>false</c>.</returns>
        public bool IsToLocationValid()
        {
            return !(string.IsNullOrEmpty(ToCity)
                && string.IsNullOrEmpty(ToState)
                && string.IsNullOrEmpty(ToPostalCode)
                && string.IsNullOrEmpty(ToCountry));
        }

        /// <summary>
        /// Represents a location with city, state, country, and postal code information.
        /// </summary>
        /// <returns>A location object.</returns>
        public Location GetFromLocation()
        {
            return new Location(
                FromCity?.ToLower() ?? string.Empty,
                FromState?.ToLower() ?? string.Empty,
                FromCountry?.ToLower() ?? string.Empty,
                FromPostalCode?.ToLower() ?? string.Empty
            );
        }

        /// <summary>
        /// Represents a location with city, state, country, and postal code information.
        /// </summary>
        /// <returns>A location object.</returns>
        public Location GetToLocation()
        {
            return new Location(
                ToCity?.ToLower() ?? string.Empty,
                ToState?.ToLower() ?? string.Empty,
                ToCountry?.ToLower() ?? string.Empty,
                ToPostalCode?.ToLower() ?? string.Empty
            );
        }
    }

    public class ConvertTimeBetweenTimezonesBatch
    {
        /// <summary>
        /// The logger instance.
        /// </summary>
        private readonly ILogger<ConvertTimeBetweenTimezonesBatch> _logger;

        /// <summary>
        /// The timevault service instance.
        /// </summary>
        private readonly ITimevaultService _timevaultService;

        /// <summary>
        /// The JSON serializer options.
        /// </summary>
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertTimeBetweenTimezonesBatch"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="timevaultService">The timevault service instance.</param>
        public ConvertTimeBetweenTimezonesBatch(ILogger<ConvertTimeBetweenTimezonesBatch> logger, ITimevaultService timevaultService)
        {
            _logger = logger;
            _timevaultService = timevaultService;
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }


        /// <summary>
        /// Converts time between timezones in batch.
        /// </summary>
        /// <param name="req">The HTTP request.</param>
        /// <returns>An <see cref="IActionResult"/> representing the result of the operation.</returns>
        [Function("ConvertTimeBetweenTimezonesBatch")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "time/convertBatch")] HttpRequest req)
        {
            // Deserialize the request body into a list of location items.
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<List<ConvertTimeBetweenTimezonesBatchRequest>>(requestBody, _jsonSerializerOptions);

            // Safe guard against null or empty data.
            if (data == null || data.Count == 0)
            {
                _logger.LogWarning("The request body must contain at least one request item.");

                var errorResponse = GetIanaTimezoneByLocationResponse.Error(
                    "The request body must contain at least one request item.",
                    null,
                    StatusCodes.Status400BadRequest
                );
                return new BadRequestObjectResult(errorResponse);
            }

            var fromLocations = data.Where(x => x.IsFromLocationValid()).Select(x => x.GetFromLocation()).Distinct().ToList();
            var toLocations = data.Where(x => x.IsToLocationValid()).Select(x => x.GetToLocation()).Distinct().ToList();

            // We concatenate the from and to locations and remove duplicates,
            // as this will reduce the number of requests to the Timevault service.
            // This will significantly reduce the operation time for large batches as well reduce costs.
            var allLocations = fromLocations.Concat(toLocations).Distinct().ToList();
            
            IDictionary<Location, ApiResponse<string>> ianaCodes;
            try
            {
                ianaCodes = await _timevaultService.GetIanaCodeByLocationBatchAsync(allLocations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving the IANA timezone codes for the locations: {ex.Message}");

                return new ObjectResult(ConvertTimeBetweenTimezonesResponse.Error(
                    ex.Message,
                    null,
                    null
                ))
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }

            var response = new List<ConvertTimeBetweenTimezonesResponse>();
            foreach (var item in data)
            {
                Location fromLocation = item.GetFromLocation();
                Location toLocation = item.GetToLocation();

                ApiResponse<string>? fromLocationIanaCode;
                ApiResponse<string>? toLocationIanaCode;

                if (!ianaCodes.TryGetValue(fromLocation, out fromLocationIanaCode))
                {
                    _logger.LogWarning($"No timezone found for the provided location: {fromLocation}");

                    response.Add(ConvertTimeBetweenTimezonesResponse.Error(
                        fromLocationIanaCode?.ErrorMessage ?? $"No timezone found for the provided location: {fromLocation}",
                        fromLocation,
                        toLocation,
                        fromLocationIanaCode?.StatusCode ?? StatusCodes.Status500InternalServerError
                    ));
                    continue;
                }

                if (!ianaCodes.TryGetValue(toLocation, out toLocationIanaCode))
                {
                    _logger.LogWarning($"No timezone found for the provided location: {toLocation}");

                    response.Add(ConvertTimeBetweenTimezonesResponse.Error(
                        toLocationIanaCode?.ErrorMessage ?? $"No timezone found for the provided location: {toLocation}",
                        fromLocation,
                        toLocation,
                        toLocationIanaCode?.StatusCode ?? StatusCodes.Status500InternalServerError
                    ));
                    continue;
                }

                if (!fromLocationIanaCode.IsSuccess)
                {
                    response.Add(ConvertTimeBetweenTimezonesResponse.Error(
                        fromLocationIanaCode.ErrorMessage ?? $"No timezone found for the provided location: {fromLocation}",
                        fromLocation,
                        toLocation,
                        fromLocationIanaCode.StatusCode
                    ));
                    continue;
                }

                if (!toLocationIanaCode.IsSuccess)
                {
                    response.Add(ConvertTimeBetweenTimezonesResponse.Error(
                        toLocationIanaCode.ErrorMessage ?? $"No timezone found for the provided location: {toLocation}",
                        fromLocation,
                        toLocation,
                        toLocationIanaCode.StatusCode
                    ));
                    continue;
                }

                string fromTimezone = fromLocationIanaCode.Value;
                string toTimezone = toLocationIanaCode.Value;
                string fromTimeString = item.FromTime ?? string.Empty;

                DateTime dateTime;
                if (!DateTime.TryParse(fromTimeString, out dateTime))
                {
                    _logger.LogWarning($"The 'fromTime' parameter is not a valid date and time: {fromTimeString}. Please provide a valid date in ISO 8601 format.");

                    var errorResponse = ConvertTimeBetweenTimezonesResponse.Error(
                        "The 'fromTime' parameter is not a valid date and time. Please provide a valid date in ISO 8601 format.",
                        fromLocation,
                        toLocation,
                        StatusCodes.Status400BadRequest
                    );
                    return new BadRequestObjectResult(errorResponse);
                }

                try
                {

                    // Parse the provided time string
                    TimeZoneInfo sourceTimeZone = TimeZoneInfo.FindSystemTimeZoneById(fromTimezone);
                    TimeZoneInfo targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById(toTimezone);

                    // Assuming dateTime represents a local time in the source timezone
                    // First, ensure dateTime is associated with the source timezone
                    DateTime sourceDateTime;
                    if (dateTime.Kind == DateTimeKind.Unspecified)
                    {
                        // Convert 'unspecified' DateTime to 'local' DateTime in source timezone
                        sourceDateTime = TimeZoneInfo.ConvertTime(dateTime, sourceTimeZone, sourceTimeZone);
                    }
                    else
                    {
                        // For 'Utc' or 'Local' kinds, directly convert to source timezone
                        sourceDateTime = TimeZoneInfo.ConvertTime(dateTime, sourceTimeZone);
                    }

                    // Now, convert the DateTime from the source timezone to the target timezone
                    DateTime targetDateTime = TimeZoneInfo.ConvertTime(sourceDateTime, sourceTimeZone, targetTimeZone);

                    response.Add(ConvertTimeBetweenTimezonesResponse.Success(fromLocation, toLocation, sourceDateTime, targetDateTime));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error occurred while converting time between timezones: {ex.Message}");

                    response.Add(ConvertTimeBetweenTimezonesResponse.Error(
                        "An error occurred while converting time between timezones.",
                        fromLocation,
                        toLocation
                    ));
                }
            }
            return new OkObjectResult(response);
        }
    }
}