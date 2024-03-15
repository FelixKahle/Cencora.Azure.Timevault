// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

// This file will make creating the necessary resources for the Timevault project easier. It will create the following resources:
// - Azure CosmosDB account
// - Azure Function App
// - Azure Storage Account
// - Azure Application Insights
// - Azure Maps Account
// - Role Assignments
// All these services can be set up using the Azure Portal, but this file will make it easier to set up the services in a consistent way.
// It also reduces the risk of human error.

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// General
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

@description('The base name for the project.')
param projectBaseName string = 'timevault'

@description('The geographical location where the function app and its associated resources are deployed. This location should match the Azure region of the resource group to minimize latency and costs.')
param location string = resourceGroup().location

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// CosmosDB
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

@description('Specifies the unique name of the Azure Cosmos DB account to be created or managed. This name is used as an identifier across Azure to reference the Cosmos DB account and must be unique across Azure. The name will be part of the Cosmos DB account URL.')
param cosmosDBAccountName string = '${projectBaseName}cosmosdbaccount'
@description('The name of the database inside the CosmosDB account.')
param databaseName string = '${projectBaseName}database'
@description('The name of the container that holds all jobs from the VIB.')
param containerName string = 'timevaultcontainer'

resource cosmosDBAccount 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' = {
  kind: 'GlobalDocumentDB'
  name: cosmosDBAccountName
  location: location
  properties: {
    disableLocalAuth: false
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        // This makes no sense in my opinion, even the documentation suggests to name the id <accountName>-<locationName>.
        // However, in the moment of writing, this line creates a warning, so I have to disable it
        #disable-next-line BCP073
        id: '${cosmosDBAccountName}-${location}'
        failoverPriority: 0
        locationName: location
      }
    ]
    backupPolicy: {
      type: 'Continuous'
      continuousModeProperties: {
        tier: 'Continuous7Days'
    }
    }
    isVirtualNetworkFilterEnabled: false
    virtualNetworkRules: []
    ipRules: []
    minimalTlsVersion: 'Tls12'
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
    enableFreeTier: false
    capacity: {
      totalThroughputLimit: 4000
    }
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-11-15' = {
  parent: cosmosDBAccount
  name: databaseName
  location: location
  properties: {
    resource: {
      id: databaseName
    }
  }
}

resource container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-11-15' = {
  parent: database
  name: containerName
  location: location
  properties: {
    resource: {
      id: containerName
      partitionKey: {
        paths: [
          '/ianaCode'
        ]
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
    }
  }
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Function App
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

@description('The performance and replication tier (SKU) of the Azure Storage account used by the function app. Standard_LRS indicates a standard-tier, locally redundant storage.')
param functionStorageSKU string = 'Standard_LRS'
@description('The pricing tier (SKU) of the function app, which determines the cost and capabilities of the function app. For example, Y1 is a dynamic consumption plan that scales based on demand.')
param functionAppSKU string = 'Y1'
@description('The user-defined name of the function app. This name is used as the hostname for the function app and must be globally unique across Azure.')
param functionAppName string = '${projectBaseName}functionapp'
@description('The name of the Azure App Service Plan that hosts the function app. This plan defines the physical resources allocated to the function app, including pricing tier and compute resources.')
param functionHostingPlanName string = '${functionAppName}hostingplan'
@description('The name of the Azure Storage account used by the function app for storing function code, logs, and state. Storage account names must be unique across Azure.')
@minLength(3)
@maxLength(24)
param functionStorageName string = '${projectBaseName}fnstrg'
@description('The name of the Application Insights instance used to monitor the function app')
param functionApplicationInsightsName string = '${functionAppName}insights'

resource functionHostingPlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: functionHostingPlanName
  kind: 'functionapp'
  location: location
  sku: {
    name: functionAppSKU
    tier: 'Dynamic'
  }
}

resource functionStorage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: functionStorageName
  kind: 'StorageV2'
  location: location
  sku: {
    name: functionStorageSKU
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: functionApplicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}

resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: functionAppName
  kind: 'functionapp'
  location: location
  properties: {
    serverFarmId: functionHostingPlan.id
    httpsOnly: true
    siteConfig: {
      use32BitWorkerProcess: false
      netFrameworkVersion: 'v8.0'
      cors: {
        allowedOrigins: [
          'https://portal.azure.com'
        ]
      }
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${functionStorage.name};AccountKey=${functionStorage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}' // Azure Functions internal setting
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED'
          value: '1'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${functionStorage.name};AccountKey=${functionStorage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}' // Azure Functions internal setting
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'COSMOS_DB_ENDPOINT'
          value: cosmosDBAccount.properties.documentEndpoint
        }
        {
          name: 'TIMEVAULT_DATABASE_NAME'
          value: databaseName
        }
        {
          name: 'TIMEVAULT_CONTAINER_NAME'
          value: containerName
        }
        {
          name: 'MAPS_CLIENT_ID'
          value: mapsAccount.properties.uniqueId
        }
      ]
    }
    clientAffinityEnabled: false
    virtualNetworkSubnetId: null
    publicNetworkAccess: 'Enabled'
  }
  identity: {
    type: 'SystemAssigned'
  }
}

var sqlRoleDefinitionId = '00000000-0000-0000-0000-000000000002'
resource sqlRoleAssignmentFunctionApp 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2021-04-15' = {
  parent: cosmosDBAccount
  name: guid(sqlRoleDefinitionId, functionApp.id, cosmosDBAccount.id)
  properties: {
    roleDefinitionId: '${resourceGroup().id}/providers/Microsoft.DocumentDB/databaseAccounts/${cosmosDBAccount.name}/sqlRoleDefinitions/${sqlRoleDefinitionId}'
    principalId: functionApp.identity.principalId
    scope: cosmosDBAccount.id
  }
}

var mapsRoleDefintionId = '423170ca-a8f6-4b0f-8487-9e4eb8f49bfa'
resource mapsRoleAssignementFunctionApp 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, mapsRoleDefintionId, mapsAccount.id, functionApp.id)
  scope: mapsAccount
  properties: {
    description: 'Role assignement for the function app to access the Maps Service'
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', mapsRoleDefintionId)
    principalId: functionApp.identity.principalId
  }
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Azure Maps
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

@description('The name of the Azure Maps account.')
param mapsAccountName string = '${projectBaseName}mapsaccount'

resource mapsAccount 'Microsoft.Maps/accounts@2023-06-01' = {
  name: mapsAccountName
  location: location
  sku: {
    name: 'G2'
  }
  kind: 'Gen2'
  properties: {
    disableLocalAuth: true
  }
}
