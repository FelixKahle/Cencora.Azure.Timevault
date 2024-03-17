// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Cencora.Azure.Timevault
{
    /// <summary>
    /// Represents a class that retrieves the timezone based on the provided location information.
    /// </summary>
    public class GetIanaTimezoneBylocation
    {
        /// <summary>
        /// The logger instance.
        /// </summary>
        private readonly ILogger<GetIanaTimezoneBylocation> _logger;

        /// <summary>
        /// The timevault service instance.
        /// </summary>
        private readonly ITimevaultService _timevaultService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetIanaTimezoneBylocation"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="timevaultService">The timevault service instance.</param>
        public GetIanaTimezoneBylocation(ILogger<GetIanaTimezoneBylocation> logger, ITimevaultService timevaultService)
        {
            _logger = logger;
            _timevaultService = timevaultService;
        }

        /// <summary>
        /// Retrieves the IANA timezone information based on the provided location parameters.
        /// </summary>
        /// <param name="req">The HTTP request object.</param>
        /// <returns>An <see cref="IActionResult"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// This function is an Azure Function triggered by HTTP requests. It expects the following query parameters:
        /// - city: The city name.
        /// - state: The state name.
        /// - postalCode: The postal code.
        /// - country: The country name.
        /// 
        /// At least one of the above parameters must be provided. If none of the parameters are provided, a bad request response is returned.
        /// 
        /// The function uses the provided location parameters to retrieve the corresponding IANA timezone using the <see cref="_timevaultService"/> service.
        /// If successful, the function returns the timezone information as an OkObjectResult. If an error occurs, a 500 Internal Server Error response is returned.
        /// </remarks>
        [Function("getIanaTimezoneByLocation")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
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
                return new BadRequestObjectResult("Please provide at least one of the following parameters: location, city, state, postalCode, country");
            }

            // The strings can not be null, so we can create a new location object.
            // <see query.ToString() returns an empty string if the query parameter is not provided.
            Location location = new Location(cityString, stateString, countryString, postalCodeString);

            // Try to retrieve the timezone for the provided location.
            try
            {
                var timezone = await _timevaultService.GetIanaTimezoneAsync(location);
                if (string.IsNullOrEmpty(timezone))
                {
                    return new NotFoundObjectResult($"No timezone found for the provided location: {location}");
                }

                GetIanaTimezoneByLocationResult result = new GetIanaTimezoneByLocationResult
                {
                    Location = location,
                    IanaTimezone = timezone
                };
                
                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while trying to get the timezone for the location {location}: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}