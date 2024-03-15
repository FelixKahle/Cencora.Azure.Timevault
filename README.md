# Timevault Service for Cencora

This repository contains the source code for the Timevault service, developed for Cencora. Timevault is designed to manage and store timezone information related to specific locations or geographical coordinates. This enables efficient time translation between timezones for applications requiring accurate scheduling and time management across global locations.

## Overview

The Timevault service leverages Azure Maps services to retrieve geographical information based on given locations. It then uses this information to determine the appropriate IANA timezone. The resolved timezone data is stored in a Cosmos DB database for future reference. This approach significantly reduces the number of calls made to Azure Maps services, optimizing operational costs by relying more on the cost-effective Cosmos DB operations for recurring timezone queries.

## Services

### Azure Maps Services

- **Geocoding**: Converts locations into geographic coordinates (latitude and longitude).
- **Timezone Lookup**: Determines the timezone information based on geographic coordinates.

### Cosmos DB

Stores and indexes timezone information linked to locations and geographic coordinates, enabling quick retrieval of timezone data for previously queried locations.

## Usage

### Ensure Proper Configuration

Before making requests, verify that the following environment variables are correctly set in your service configuration:

- `COSMOS_DB_ENDPOINT`: Your Azure Cosmos DB endpoint.
- `TIMEVAULT_DATABASE_NAME`: The name of your database within Azure Cosmos DB.
- `TIMEVAULT_CONTAINER_NAME`: The name of your container within the Cosmos DB database.
- `MAPS_CLIENT_ID`: Your Azure Maps client ID.
- `IANA_CODE_UPDATE_INTERVAL_IN_MINUTES`: Interval for updating IANA codes, with a default value of 43200 minutes (30 days) if not set.

**Note:** If you are using the main.bicep file to deploy the Azure infrastructure, all these variables will be correctly set up for you.

### `getIanaTimezoneByLocation` Function

The `getIanaTimezoneByLocation` function is for retrieving the IANA timezone based on location details. It is designed to increase the accuracy of the timezone returned with the specificity of the information provided.

#### Parameters

This function requires at least one of the following parameters to be specified. The more details you provide, the higher the likelihood of accurately determining the correct timezone:

- `city` (optional): Name of the city.
- `country` (optional): Name of the country.
- `state` (optional): Name of the state or region.
- `postalCode` (optional): Postal or ZIP code.

## Implementation Note

The `azure-sdk-for-net` currently does not offer a client implementation for the Azure Maps Timezone service. To bridge this gap, we've developed our custom implementation, which is included in this repository and distributed as a NuGet package alongside the Timevault service code. This solution serves as an interim workaround, enabling us to utilize Azure Maps for timezone information retrieval effectively.

We anticipate that the Microsoft team responsible for `azure-sdk-for-net` will eventually release a robust implementation for the Azure Maps Timezone client. Once available, we plan to adopt this official client, ensuring our Timevault service leverages the most stable, efficient, and supported means of interaction with Azure Maps services.

## License

This project is licensed under the MIT License - see the LICENSE file for details.