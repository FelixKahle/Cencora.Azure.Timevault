// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using Cencora.Azure.Timevault;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

string cosmosDBEndpoint = Environment.GetEnvironmentVariable("COSMOS_DB_ENDPOINT") ?? throw new ArgumentNullException("COSMOS_DB_ENDPOINT");
string timevaultDatabaseName = Environment.GetEnvironmentVariable("TIMEVAULT_DATABASE_NAME") ?? throw new ArgumentNullException("TIMEVAULT_DATABASE_NAME");
string timevaultContainerName = Environment.GetEnvironmentVariable("TIMEVAULT_CONTAINER_NAME") ?? throw new ArgumentNullException("TIMEVAULT_CONTAINER_NAME");
string? mapsClientId = Environment.GetEnvironmentVariable("MAPS_CLIENT_ID");

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddSingleton<TimevaultFunctionSettings>(new TimevaultFunctionSettings
        {
            TimevaultCosmosDBDatabaseName = timevaultDatabaseName,
            TimevaultCosmosDBContainerName = timevaultContainerName,
        });

        // Add the Timevault service
        services.AddSingleton<ITimevaultService, TimevaultService>();
    })
    .Build();

host.Run();
