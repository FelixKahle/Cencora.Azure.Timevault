# Timevault Service for Cencora

This repository contains the source code for the Timevault service, developed for Cencora. Timevault is designed to manage and store timezone information related to specific addresses or geographical coordinates. This enables efficient time translation between timezones for applications requiring accurate scheduling and time management across global locations.

## Overview

The Timevault service leverages Azure Maps services to retrieve geographical information based on given addresses. It then uses this information to determine the appropriate IANA timezone. The resolved timezone data is stored in a Cosmos DB database for future reference. This approach significantly reduces the number of calls made to Azure Maps services, optimizing operational costs by relying more on the cost-effective Cosmos DB operations for recurring timezone queries.

## Services

### Azure Maps Services

- **Geocoding**: Converts addresses into geographic coordinates (latitude and longitude).
- **Timezone Lookup**: Determines the timezone information based on geographic coordinates.

### Cosmos DB

Stores and indexes timezone information linked to addresses and geographic coordinates, enabling quick retrieval of timezone data for previously queried locations.

## Usage

To fetch timezone information for a given address or set of coordinates, make a request to the Timevault service's relevant endpoint. The service will attempt to return timezone data from Cosmos DB if available; otherwise, it will query Azure Maps for the required information and store it in Cosmos DB for future use.

## License

This project is licensed under the MIT License - see the LICENSE file for details.