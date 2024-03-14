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
    /// Represents a class that retrieves the IANA timezone by coordinate.
    /// </summary>
    public class GetIanaTimezoneByCoordinate
    {
        /// <summary>
        /// The logger instance.
        /// </summary>
        private readonly ILogger<GetIanaTimezoneByCoordinate> _logger;

        /// <summary>
        /// The timevault service instance.
        /// </summary>
        private readonly ITimevaultService _timevaultService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetIanaTimezoneByCoordinate"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="timevaultService">The timevault service instance.</param>
        public GetIanaTimezoneByCoordinate(ILogger<GetIanaTimezoneByCoordinate> logger, ITimevaultService timevaultService)
        {
            _logger = logger;
            _timevaultService = timevaultService;
        }

        /// <summary>
        /// Executes the Azure Function to get the IANA timezone by coordinate.
        /// </summary>
        /// <param name="req">The HTTP request containing the latitude and longitude query parameters.</param>
        /// <returns>An <see cref="IActionResult"/> representing the result of the operation.</returns>
        [Function("getIanaTimezoneByCoordinate")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            string latitudeString = req.Query["latitude"].ToString();
            string longitudeString = req.Query["longitude"].ToString();

            if (string.IsNullOrEmpty(latitudeString))
            {
                return new BadRequestObjectResult("The 'latitude' query parameter is required.");
            }

            if (string.IsNullOrEmpty(longitudeString))
            {
                return new BadRequestObjectResult("The 'longitude' query parameter is required.");
            }

            // Parse the latitude
            double latitude;
            if (!double.TryParse(latitudeString, NumberStyles.Any, CultureInfo.InvariantCulture, out latitude))
            {
                return new BadRequestObjectResult("The 'latitude' query parameter is not a valid number.");
            }

            // Parse the longitude
            double longitude;
            if (!double.TryParse(longitudeString, NumberStyles.Any, CultureInfo.InvariantCulture, out longitude))
            {
                return new BadRequestObjectResult("The 'longitude' query parameter is not a valid number.");
            }

            // Construct the coordinate
            var coordinate = new GeoCoordinate(latitude, longitude);

            try
            {
                var timezone = await _timevaultService.GetIanaTimezoneAsync(coordinate);
                return new OkObjectResult(timezone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting the IANA timezone by coordinate.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}