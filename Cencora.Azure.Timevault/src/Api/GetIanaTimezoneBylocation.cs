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
        /// Executes the Azure Function to retrieve the IANA timezone by location.
        /// </summary>
        /// <param name="req">The HTTP request object.</param>
        /// <returns>An <see cref="IActionResult"/> representing the result of the operation.</returns>
        /// <remarks>
        /// This function expects the following query parameters:
        /// - city: The name of the city.
        /// - state: The name of the state.
        /// - postalCode: The postal code.
        /// - country: The name of the country.
        /// 
        /// At least one of the location parameters (city, state, postalCode, country) must be provided.
        /// 
        /// If the operation is successful, it returns the IANA timezone for the specified location.
        /// If an error occurs, it returns a 500 Internal Server Error.
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

            try
            {
                string ianaTimezone = await _timevaultService.GetIanaCodeByLocationAsync(location);
                
                GetIanaTimezoneByLocationResult result = new GetIanaTimezoneByLocationResult
                {
                    Location = location,
                    IanaTimezone = ianaTimezone
                };

                return new OkObjectResult(result);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the IANA timezone by location.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}