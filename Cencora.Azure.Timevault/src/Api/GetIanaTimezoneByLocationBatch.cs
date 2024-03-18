// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Cencora.Azure.Timevault
{
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
        /// Initializes a new instance of the <see cref="GetIanaTimezoneByLocationBatch"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="timevaultService">The timevault service instance.</param>
        public GetIanaTimezoneByLocationBatch(ILogger<GetIanaTimezoneByLocationBatch> logger, ITimevaultService timevaultService)
        {
            _logger = logger;
            _timevaultService = timevaultService;
        }

        [Function("getIanaTimezoneByLocationBatch")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                if (string.IsNullOrEmpty(requestBody))
                {
                    return new BadRequestObjectResult("The request body cannot be empty.");
                }

                var locationBatch = JsonSerializer.Deserialize<List<Location>>(requestBody);
                if (locationBatch == null || !locationBatch.Any())
                {
                    return new BadRequestObjectResult("The request body must contain a non-empty array of location objects.");
                }

                Dictionary<Location, string?> result = await _timevaultService.GetIanaTimezoneBatchAsync(locationBatch);

                result.Select(x => new GetIanaTimezoneByLocationResult
                {
                    Location = x.Key,
                    IanaTimezone = x.Value
                }).ToList();

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while processing the request: {ex}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}