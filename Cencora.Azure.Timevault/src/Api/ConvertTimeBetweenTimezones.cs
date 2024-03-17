// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Cencora.Azure.Timevault
{
    /// <summary>
    /// Represents a class that converts time between different timezones.
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
        /// Converts the time between timezones based on the provided location and time.
        /// </summary>
        /// <param name="req">The HTTP request containing the location and time parameters.</param>
        /// <returns>An <see cref="IActionResult"/> representing the result of the conversion.</returns>
        [Function("convertTimeBetweenTimezones")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
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
                return new BadRequestObjectResult("At least one of the 'from' location parameters must be provided.");
            }

            if (string.IsNullOrEmpty(fromTimeString))
            {
                return new BadRequestObjectResult("The 'fromTime' parameter must be provided.");
            }

            if (string.IsNullOrEmpty(toCityString) && string.IsNullOrEmpty(toStateString) && string.IsNullOrEmpty(toPostalCodeString) && string.IsNullOrEmpty(toCountryString))
            {
                return new BadRequestObjectResult("At least one of the 'to' location parameters must be provided.");
            }

            Location fromLocation = new Location(fromCityString, fromStateString, fromPostalCodeString, fromCountryString);
            Location toLocation = new Location(toCityString, toStateString, toPostalCodeString, toCountryString);
            
            DateTime dateTime;
            if (!DateTime.TryParse(fromTimeString, out dateTime))
            {
                return new BadRequestObjectResult("The 'fromTime' parameter is not a valid date and time. Please provide a valid date in ISO 8601 format.");
            }

            try
            {
                var fromTimezone = await _timevaultService.GetIanaTimezoneAsync(fromLocation);
                var toTimezone = await _timevaultService.GetIanaTimezoneAsync(toLocation);

                if (string.IsNullOrEmpty(fromTimezone))
                {
                    return new NotFoundObjectResult($"No timezone found for the provided location: {fromLocation}");
                }

                if (string.IsNullOrEmpty(toTimezone))
                {
                    return new NotFoundObjectResult($"No timezone found for the provided location: {toLocation}");
                }

                // Parse the provided time string
                TimeZoneInfo sourceTimeZone = TimeZoneInfo.FindSystemTimeZoneById(fromTimezone);
                TimeZoneInfo targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById(toTimezone);

                // Correctly create a DateTimeOffset with the source timezone's offset
                DateTimeOffset sourceDateTimeOffset = new DateTimeOffset(dateTime, sourceTimeZone.GetUtcOffset(dateTime));

                // Convert the DateTimeOffset to the target timezone
                DateTimeOffset targetDateTimeOffset = TimeZoneInfo.ConvertTime(sourceDateTimeOffset, targetTimeZone);

                // Extract the DateTime in the target timezone
                DateTime targetDateTime = targetDateTimeOffset.DateTime;

                ConvertTimeBetweenTimezonesResult result = new ConvertTimeBetweenTimezonesResult
                {
                    FromLocation = fromLocation,
                    ToLocation = toLocation,
                    FromTime = dateTime,
                    ToTime = targetDateTime
                };

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while converting the time between timezones: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
