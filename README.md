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
- `MAX_CONCURRENT_TASK_COSMOS_DB_REQUESTS`: This setting limits the number of concurrent requests made to the Cosmos DB service to prevent overloading and potential throttling by the service. It's applied at the task level, meaning each task (e.g., a query execution or document upsert) can make up to this number of concurrent requests, but the limit isn't shared across multiple tasks. Example: If the limit is set to 20, two running tasks can each make up to 20 concurrent requests to Cosmos DB, potentially resulting in a total of 40 concurrent requests from the application. The default value is 20.
- `MAX_CONCURRENT_TIMEZONE_REQUESTS`: Max amount of concurrent requests to the Timezone service, with a default value of 10 if not set.

**Note:** If you are using the main.bicep file to deploy the Azure infrastructure, all these variables will be correctly set up for you.

### `timezone/byLocation` API Endpoint

#### Overview
The `timezone/byLocation` endpoint is designed to retrieve the IANA timezone based on geographical information. It accepts GET requests with the following parameters, aimed to specify the location for which the timezone information is requested. The precision of the timezone returned improves with the number of parameters provided.

#### Request Parameters
- `city` (optional): The name of the city.
- `country` (optional): The name of the country.
- `state` (optional): The name of the state or region.
- `postalCode` (optional): The postal or ZIP code.

**Note:** Providing more detailed location information results in a more accurate timezone determination.

#### Success Response Format
The response is a JSON object containing the queried location details and its corresponding IANA timezone identifier.

```json
{
  "location": {
    "city": "<queried city>",
    "state": "<queried state>",
    "country": "<queried country>",
    "postalCode": "<queried postalCode>"
  },
  "ianaTimezone": "<IANA timezone identifier>",
  "statusCode": 200
}
```
#### Error Response Format
If the request fails the following JSON will returned.
Note that the `location` part of the JSON is only present if the caller passed the request parameter correctly.

```json
{
  "location": {
    "city": "<queried city>",
    "state": "<queried state>",
    "country": "<queried country>",
    "postalCode": "<queried postalCode>"
  },
  "statusCode": "<Error Code>",
  "errorMessage": "<Error Message"
}
```

### `timezone/byLocationBatch` API Endpoint

#### Overview
The `timezone/byLocationBatch` endpoint is designed to retrieve the IANA timezone based on geographical information in a batch operation. It accepts POST requests with the following parameters, aimed to specify the location for which the timezone information is requested. The precision of the timezone returned improves with the number of parameters provided.

#### Request Parameters
The `timezone/byLocationBatch` endpoint accepts a json as parameter in the following format:

```json
[
  {
    "city": "<City>",
    "state": "<State>",
    "postalCode": "<Postal Code>",
    "country": "<Country>"
  },
  {
    "city": "<City>",
    "state": "<State>",
    "postalCode": "<Postal Code>",
    "country": "<Country>"
  },
]
```

#### Success Response Format
```json
[
  {
    "location": {
      "city": "<City>",
      "state": "<State>",
      "country": "<Country>",
      "postalCode": "<Postal Code>"
    },
    "ianaTimezone": "<Iana Timezone>",
    "statusCode": 200
  },
  {
    "location": {
      "city": "<City>",
      "state": "<State>",
      "country": "<Country>",
      "postalCode": "<Postal Code>"
    },
    "errorMessage": "<Error Message>",
    "statusCode": "<Error Status Code>"
  },
]
```

### `time/convert` API Endpoint

#### Overview
The `time/convert` endpoint is designed for converting time between different timezones based on specified locations and a given time. It supports GET requests with parameters detailing the origin and destination locations, as well as the initial time.

#### timeConversion
- `fromCity` (optional): The name of the origin city.
- `fromState` (optional): The name of the origin state or region.
- `fromCountry` (optional): The name of the origin country.
- `fromPostalCode` (optional): The postal or ZIP code of the origin location.
- `fromTime`: The original time in ISO 8601 format (mandatory).
- `toCity` (optional): The name of the destination city.
- `toState` (optional): The name of the destination state or region.
- `toCountry` (optional): The name of the destination country.
- `toPostalCode` (optional): The postal or ZIP code of the destination location.

**Note**: It is crucial to provide the fromTime parameter in a valid ISO 8601 format to ensure accurate conversion. At least one location parameter (city, state, country, postalCode) must be provided for both origin and destination. Providing more detailed location information results in a more accurate timezone determination.

#### Success Response Format
The response is a JSON object detailing the original and converted times along with the respective locations:

```json
{
  "fromLocation": {
    "city": "<origin city>",
    "state": "<origin state>",
    "country": "<origin country>",
    "postalCode": "<origin postalCode>"
  },
  "toLocation": {
    "city": "<destination city>",
    "state": "<destination state>",
    "country": "<destination country>",
    "postalCode": "<destination postalCode>"
  },
  "fromTime": "<original time in ISO 8601 format>",
  "toTime": "<converted time in ISO 8601 format>",
  "statusCode": "200"
}
```

#### Error Response Format
If the request fails the following JSON will returned.
Note that the `fromLocation` and `toLocation` parts of the JSON are only present if the caller passed the request parameter correctly.

```json
{
  "fromLocation": {
    "city": "<origin city>",
    "state": "<origin state>",
    "country": "<origin country>",
    "postalCode": "<origin postalCode>"
  },
  "toLocation": {
    "city": "<destination city>",
    "state": "<destination state>",
    "country": "<destination country>",
    "postalCode": "<destination postalCode>"
  },
  "fromTime": "<original time in ISO 8601 format>",
  "statusCode": "<Error Code>",
  "errorMessage": "<Error Message>"
}
```

### `time/convertBatch` API Endpoint

#### Overview
The `time/convert` endpoint is designed for converting time between different timezones based on specified locations and a given time in a batch operation. It supports POST requests with parameters detailing the origin and destination locations, as well as the initial time.

```json
[
  {
    "fromCity": "<From City>",
    "fromState": "<From State>",
    "fromPostalCode": "<From Posta Code>",
    "fromCountry": "<From Country>",
    "toCity": "<To City>",
    "toState": "<To State>",
    "toPostalCode": "<To Postal Code>",
    "toCountry": "<To Country>",
    "fromTime": "<From Time (ISO 1806)>"
  },
  {
    "fromCity": "<From City>",
    "fromState": "<From State>",
    "fromPostalCode": "<From Posta Code>",
    "fromCountry": "<From Country>",
    "toCity": "<To City>",
    "toState": "<To State>",
    "toPostalCode": "<To Postal Code>",
    "toCountry": "<To Country>",
    "fromTime": "<From Time (ISO 1806)>"
  }
]
```

#### Success Response Format

```json
[
  {
    "fromLocation": {
      "city": "<From City>",
      "state": "<From State>",
      "country": "<From Country>",
      "postalCode": "<From Postal Code>"
    },
    "toLocation": {
      "city": "<To Location>",
      "state": "<To State>",
      "country": "<To Country>",
      "postalCode": "<To Postal Code>"
    },
    "fromTime": "<From Time (ISO 1806)>",
    "toTime": "<To Time (ISO 1806)>",
    "statusCode": 200
  },
  {
    "fromLocation": {
      "city": "<From City>",
      "state": "<From State>",
      "country": "<From Country>",
      "postalCode": "<From Postal Code>"
    },
    "toLocation": {
      "city": "<To Location>",
      "state": "<To State>",
      "country": "<To Country>",
      "postalCode": "<To Postal Code>"
    },
    "errorMessage": "<Error Message>",
    "statusCode": "<Error Status Code>"
  },
]
```

**Note**: It is crucial to provide the fromTime parameter in a valid ISO 8601 format to ensure accurate conversion. At least one location parameter (city, state, country, postalCode) must be provided for both origin and destination. Providing more detailed location information results in a more accurate timezone determination.

## Implementation Note

The `azure-sdk-for-net` currently does not offer a client implementation for the Azure Maps Timezone service. To bridge this gap, we've developed our custom implementation, which is included in this repository and distributed as a NuGet package alongside the Timevault service code. This solution serves as an interim workaround, enabling us to utilize Azure Maps for timezone information retrieval effectively.

We anticipate that the Microsoft team responsible for `azure-sdk-for-net` will eventually release a robust implementation for the Azure Maps Timezone client. Once available, we plan to adopt this official client, ensuring our Timevault service leverages the most stable, efficient, and supported means of interaction with Azure Maps services.

## License

This project is licensed under the MIT License - see the LICENSE file for details.