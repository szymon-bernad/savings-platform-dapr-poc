param appname string = 'savingsplatform'
param location string = resourceGroup().location

param spApiName string = 'savingsplatform-poc-api'
param spApiImage string = '${containerRegistry}/${spApiName}:0.1'
param backendApiPort int = 80

param spEventStoreName string = 'savingsplatform-poc-eventstore'
param spEventStoreImage string = '${containerRegistry}/${spEventStoreName}:0.2'
param eventStoreApiPort int = 80

param spPaymentProxyName string = 'sp-poc-paymentproxy'
param spPaymentProxyImage string = '${containerRegistry}/savings-platform-poc-paymentproxy:0.2'

param containerRegistry string = ''
param containerRegistryUsername string = ''

param serviceBusResName string = ''
param serviceBusResGroup string = ''
@secure()
param containerRegistryPassword string = ''
param registryPassName string = 'registry-password'

var environmentName = '${appname}-env'



//Reference to ServiceBus resource
resource serviceBusResource 'Microsoft.ServiceBus/namespaces@2021-11-01' existing = {
  name: serviceBusResName
  scope: resourceGroup(serviceBusResGroup)
}

//Build Svc Bus Connection String
var listKeysEndpoint = '${serviceBusResource.id}/AuthorizationRules/RootManageSharedAccessKey'
var sharedAccessKey = '${listKeys(listKeysEndpoint, serviceBusResource.apiVersion).primaryKey}'
var serviceBusConStringValue = 'Endpoint=sb://${serviceBusResName}.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=${sharedAccessKey}'


// Container Apps Environment 
module environment 'aca-env.bicep' = {
  dependsOn: [ ]
  name: '${deployment().name}--acaenvironment'
  params: {
    acaEnvironmentName: environmentName
    location: location
  }
}

// SavingPlatform API App
module spApiApp 'container-app.bicep' = {
  name: '${deployment().name}--${spApiName}'
  dependsOn: [
    environment
  ]
  params: {
    enableIngress: true
    isExternalIngress: true
    location: location
    environmentId: environment.outputs.acaEnvironmentId
    containerAppName: spApiName
    containerImage: spApiImage
    targetPort: backendApiPort
    isPrivateRegistry: true
    minReplicas: 1
    maxReplicas: 1
    containerRegistry: containerRegistry
    containerRegistryUsername: containerRegistryUsername
    registryPassName: registryPassName
    revisionMode: 'Single'
    secListObj: {
      secArray: [
        {
          name: registryPassName
          value: containerRegistryPassword
        } ]
    }
    envList: [ 
      {
        name: 'DAPR_HTTP_PORT'
        value: '3500'
      }
      {
         name: 'SimulationConfig__SpeedMultiplier'
         value: '1'
      }
      {
        name: 'NAMESPACE'
        value: 'savingsplatform'
      }
      {
        name: 'StateStore__StateStoreName'
        value: 'statestore-postgres'
      } 
    ]
  }
}

// SavingPlatform EventStore App
module spEventStoreApp 'container-app.bicep' = {
  name: '${deployment().name}--${spEventStoreName}'
  dependsOn: [
    environment
  ]
  params: {
    enableIngress: true
    isExternalIngress: true
    location: location
    environmentId: environment.outputs.acaEnvironmentId
    containerAppName: spEventStoreName
    containerImage: spEventStoreImage
    targetPort: eventStoreApiPort
    isPrivateRegistry: true
    minReplicas: 1
    maxReplicas: 1
    containerRegistry: containerRegistry
    containerRegistryUsername: containerRegistryUsername
    registryPassName: registryPassName
    revisionMode: 'Single'
    secListObj: {
      secArray: [
        {
          name: registryPassName
          value: containerRegistryPassword
        } ]
    }
    envList: [ 
      {
        name: 'DAPR_HTTP_PORT'
        value: '3500'
      }
      {
         name: 'ConnectionStrings__DocumentStore'
         value: 'host=dapr-tst-pgdb.postgres.database.azure.com;username=postgresadm;password=example123!;port=5432;database=event-store'
      }
      {
         name: 'DocumentStoreConfig__PlatformId'
         value: '7a50b4e8-df1a-4253-b17d-0955e06fbe83'
      }
      {
        name: 'NAMESPACE'
        value: 'savingsplatform'
      } ]
  }
}

// SavingPlatform API App
module redisApp 'container-app.bicep' = {
  name: '${deployment().name}--${appname}-redis'
  dependsOn: [
    environment
  ]
  params: {
    enableIngress: true
    isExternalIngress: false
    location: location
    environmentId: environment.outputs.acaEnvironmentId
    containerAppName: '${appname}-redis'
    containerImage: 'redis:7.2.4'
    targetPort: 6379
    isPrivateRegistry: false
    minReplicas: 1
    maxReplicas: 1
    revisionMode: 'Single'
    containerRegistry: ''
    containerRegistryUsername: ''
    registryPassName: ''
    secListObj: {
      secArray: [ ]
    }
    envList: [ ]
	transport: 'http' // NOTE: needs to be set to 'tcp' but it's not supported in bicep
  }
}


// SavingPlatform EventStore App
module spPaymentProxyApp 'container-app.bicep' = {
  name: '${deployment().name}--${spPaymentProxyName}'
  dependsOn: [
    environment
  ]
  params: {
    enableIngress: true
    isExternalIngress: true
    location: location
    environmentId: environment.outputs.acaEnvironmentId
    containerAppName: spPaymentProxyName
    containerImage: spPaymentProxyImage
    targetPort: 80
    isPrivateRegistry: true
    minReplicas: 1
    maxReplicas: 1
    containerRegistry: containerRegistry
    containerRegistryUsername: containerRegistryUsername
    registryPassName: registryPassName
    revisionMode: 'Single'
    secListObj: {
      secArray: [
        {
          name: registryPassName
          value: containerRegistryPassword
        } ]
    }
    envList: [ 
      {
        name: 'DAPR_HTTP_PORT'
        value: '3500'
      }
      {
         name: 'StateStore__StateStoreName'
         value: 'statestore-payments'
      }
      {
         name: 'NAMESPACE'
         value: 'payments'
      }
      {
         name: 'ProxyCfg__SavingsPlatformAppName'
         value: spApiName
      }      ]
  }
}

////Statestore Component
resource statestoreDaprComponent 'Microsoft.App/managedEnvironments/daprComponents@2022-03-01' = {
  name: '${environmentName}/statestore-postgres'
  dependsOn: [
    environment
  ]
  properties: {
    componentType: 'state.postgresql'
    version: 'v1'
    initTimeout: '60s'
    secrets: [
      {
        name: 'pg-conn'
        value: 'host=dapr-tst-pgdb.postgres.database.azure.com user=postgresadm password=example123! port=5432 connect_timeout=65 database=dapr-store'
      }
    ]
    metadata: [
      {
        name: 'connectionString'
        secretRef: 'pg-conn'
      }
      {
        name: 'actorStateStore'
        value: 'true'
      } ]
    scopes: [
      spApiName
      spEventStoreName
    ]
  }
}

//pubsub Service Bus Component
resource pubsubServicebusDaprComponent 'Microsoft.App/managedEnvironments/daprComponents@2022-03-01' = {
  name: '${environmentName}/savingspubsub'
  dependsOn: [
    environment
  ]
  properties: {
    componentType: 'pubsub.azure.servicebus'
    version: 'v1'
    secrets: [
      {
        name: 'sb-root-connectionstring'
        value: serviceBusConStringValue
      }
    ]
    metadata: [
      {
        name: 'connectionString'
        secretRef: 'sb-root-connectionstring'
      }
    ]
    scopes: [
      spApiName
      spEventStoreName
      spPaymentProxyName
    ]
  }
}

////Statestore Component
resource statestoreRedisDaprComponent 'Microsoft.App/managedEnvironments/daprComponents@2022-03-01' = {
  name: '${environmentName}/statestore-payments'
  dependsOn: [
    environment
  ]
  properties: {
    componentType: 'state.redis'
    version: 'v1'
    initTimeout: '60s'
    secrets: [ ]
    metadata: [
      {
        name: 'redisHost'
        value: '${appname}-redis:6379'
      } ]
    scopes: [
      spPaymentProxyName
    ]
  }
}