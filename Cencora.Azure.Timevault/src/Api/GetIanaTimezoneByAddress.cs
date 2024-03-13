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
    /// Represents a class that retrieves the timezone based on the provided address information.
    /// </summary>
    public class GetIanaTimezoneByAddress
    {
        /// <summary>
        /// The logger instance.
        /// </summary>
        private readonly ILogger<GetIanaTimezoneByAddress> _logger;

        /// <summary>
        /// The timevault service instance.
        /// </summary>
        private readonly ITimevaultService _timevaultService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetIanaTimezoneByAddress"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="timevaultService">The timevault service instance.</param>
        public GetIanaTimezoneByAddress(ILogger<GetIanaTimezoneByAddress> logger, ITimevaultService timevaultService)
        {
            _logger = logger;
            _timevaultService = timevaultService;
        }

        /// <summary>
        /// Retrieves the IANA timezone information based on the provided address parameters.
        /// </summary>
        /// <param name="req">The HTTP request object.</param>
        /// <returns>An <see cref="IActionResult"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// This function is an Azure Function triggered by HTTP requests. It expects the following query parameters:
        /// - address: The street address.
        /// - city: The city name.
        /// - state: The state name.
        /// - postalCode: The postal code.
        /// - country: The country name.
        /// 
        /// At least one of the above parameters must be provided. If none of the parameters are provided, a bad request response is returned.
        /// 
        /// The function uses the provided address parameters to retrieve the corresponding IANA timezone using the <see cref="_timevaultService"/> service.
        /// If successful, the function returns the timezone information as an OkObjectResult. If an error occurs, a 500 Internal Server Error response is returned.
        /// </remarks>
        [Function("getIanaTimezoneByAddress")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            // Retrieve the address parameters from the query string.
            string addressString = req.Query["address"].ToString();
            string cityString = req.Query["city"].ToString();
            string stateString = req.Query["state"].ToString();
            string postalCodeString = req.Query["postalCode"].ToString();
            string countryString = req.Query["country"].ToString();

            // Ensure that at least one of the address parameters is provided.
            if (string.IsNullOrEmpty(addressString)
                    && string.IsNullOrEmpty(cityString)
                    && string.IsNullOrEmpty(stateString)
                    && string.IsNullOrEmpty(postalCodeString)
                    && string.IsNullOrEmpty(countryString))
            {
                return new BadRequestObjectResult("Please provide at least one of the following parameters: address, city, state, postalCode, country");
            }

            // Create an address object from the provided parameters.
            Address address = new Address
            {
                Street = addressString,
                City = cityString,
                State = stateString,
                PostalCode = postalCodeString,
                Country = countryString
            };

            // Try to retrieve the timezone for the provided address.
            try
            {
                var timezone = await _timevaultService.GetIanaTimezoneAsync(address);
                if (string.IsNullOrEmpty(timezone))
                {
                    return new NotFoundObjectResult($"No timezone found for the provided address: {address}");
                }
                return new OkObjectResult(timezone);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while trying to get the timezone for the address {address}: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}