// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using System.Text.Json;
using Azure.Identity;
using Azure.Maps.Search;
using Azure.Maps.Timezone;
using Cencora.Azure.Timevault;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Obtain the configuration settings from environment variables
// Its crucial to have these settings in place, otherwise the application will not work
// Therefore, we throw an ArgumentNullException if the environment variable is not set
string cosmosDBEndpoint = Environment.GetEnvironmentVariable("COSMOS_DB_ENDPOINT") ?? throw new ArgumentNullException("COSMOS_DB_ENDPOINT");
string timevaultDatabaseName = Environment.GetEnvironmentVariable("TIMEVAULT_DATABASE_NAME") ?? throw new ArgumentNullException("TIMEVAULT_DATABASE_NAME");
string timevaultContainerName = Environment.GetEnvironmentVariable("TIMEVAULT_CONTAINER_NAME") ?? throw new ArgumentNullException("TIMEVAULT_CONTAINER_NAME");
string mapsClientId = Environment.GetEnvironmentVariable("MAPS_CLIENT_ID") ?? throw new ArgumentNullException("MAPS_CLIENT_ID");

int ianaCodeUpdateIntervalInMinutes = EnvironmentVariableHelper.GetInt32("IANA_CODE_UPDATE_INTERVAL_IN_MINUTES", 43200); // 30 days
int maxConcurrentCosmosDBRequests = EnvironmentVariableHelper.GetInt32("MAX_CONCURRENT_TASK_COSMOS_DB_REQUESTS", 20);
int maxConcurrentTimezoneRequests = EnvironmentVariableHelper.GetInt32("MAX_CONCURRENT_TIMEZONE_REQUESTS", 10);

// Create a new Managed Identity Credential
// This will be used to authenticate with Azure services.
// The Managed Identity is automatically assigned to the Azure Function
// and maintained by the Azure platform.
var credential = new ManagedIdentityCredential(); 

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Configure the JSON serializer to use camelCase
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        // Add the settings for the Timevault api
        services.AddSingleton<TimevaultFunctionSettings>(new TimevaultFunctionSettings
        {
            TimevaultCosmosDBDatabaseName = timevaultDatabaseName,
            TimevaultCosmosDBContainerName = timevaultContainerName,
            IanaCodeUpdateIntervalInMinutes = ianaCodeUpdateIntervalInMinutes,
            MaxConcurrentTaskCosmosDBRequests = maxConcurrentCosmosDBRequests,
            MaxConcurrentTimezoneRequests = maxConcurrentTimezoneRequests
        });

        // Configure the CosmosClient and add it to the services
        CosmosClientOptions cosmosClientOptions = new CosmosClientOptions
        {
            // Allow bulk execution of operations
            AllowBulkExecution = true,
            // We want to use camelCase for our JSON properties
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            },
        };
        services.AddSingleton<CosmosClient>(new CosmosClient(accountEndpoint: cosmosDBEndpoint, tokenCredential: credential, cosmosClientOptions));

        // Maps services
        services.AddSingleton<MapsSearchClient>(new MapsSearchClient(credential, mapsClientId));
        services.AddSingleton<MapsTimezoneClient>(new MapsTimezoneClient(credential, mapsClientId));

        // Add the Timevault service
        services.AddSingleton<ITimevaultService, TimevaultService>();
    })
    .Build();

host.Run();
