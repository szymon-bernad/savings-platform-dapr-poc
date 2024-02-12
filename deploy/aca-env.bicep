param acaEnvironmentName string
param location string = resourceGroup().location

resource environment 'Microsoft.App/managedEnvironments@2022-03-01' = {
  name: acaEnvironmentName
  location: location
  properties: {
  }
}

output acaEnvironmentId string = environment.id