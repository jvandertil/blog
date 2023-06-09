param env string
param location string = resourceGroup().location

var appName = 'jvandertilblog'
var cloudFlareIps = json(loadTextContent('cloudflare-ips.txt')).result.ipv4_cidrs

module monitoring 'monitoring.bicep' = {
  name: 'monitoring'
  params: {
    env: env
    appName: appName
    location: location
  }
}

resource contentStorageAccount 'Microsoft.Storage/storageAccounts@2021-06-01' = {
  name: 'sa${appName}${env}'
  location: location
  kind: 'StorageV2'

  sku: {
    name: 'Standard_GRS'
  }

  properties: {
    accessTier: 'Hot'

    allowSharedKeyAccess: true
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: true

    allowCrossTenantReplication: false

    networkAcls: {
      defaultAction: 'Allow' // This will be set to Deny after deployment
      bypass: 'None'
      ipRules: [for range in cloudFlareIps: {
        value: range
        action: 'Allow'
      }] 
    }

    // customDomain is configured later in the pipeline
  }

  resource blob 'blobServices' = {
    name: 'default'

    resource web 'containers' = {
      name: '$web'
    }
  }
}

var kvName = 'kv-${appName}-${env}'
module keyVault 'keyVault.bicep' ={
  name: 'keyVault'
  params: {
    name: kvName
    ipRules: split(functionApp.properties.possibleOutboundIpAddresses, ',')
    location: location
  }
}

resource keyVaultPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2021-06-01-preview' = {
  name: '${kvName}/replace'

  properties: {
    accessPolicies: [
      {
        objectId: functionApp.identity.principalId
        permissions: {
          keys: [
            'get'
            'sign'
          ]
        }
        tenantId: functionApp.identity.tenantId
      }
    ]
  }

  dependsOn: [
    keyVault
  ]
}

// This storage account should be used ONLY for the function app.
resource functionAppStorageAccount 'Microsoft.Storage/storageAccounts@2021-04-01' = {
  name: 'safa${appName}${env}'
  location: location

  kind: 'StorageV2'

  sku: {
    name: 'Standard_LRS'
  }

  properties: {
    networkAcls: {
      defaultAction: 'Allow'
    }

    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowCrossTenantReplication: false
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2021-01-15' = {
  name: 'asp-fa-${appName}-${env}'
  location: location
  sku: {
    tier: 'Dynamic'
    name: 'Y1'
  }
}

resource functionApp 'Microsoft.Web/sites@2021-01-15' = {
  name: 'fa-${appName}-${env}'
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: hostingPlan.id
    clientAffinityEnabled: true
    siteConfig: {
      use32BitWorkerProcess: false

      appSettings: concat([
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${functionAppStorageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${functionAppStorageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${functionAppStorageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${functionAppStorageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: monitoring.outputs.applicationInsightsInstrumentationKey
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~2'
        }
        {
          name: 'GitHub__ApplicationId'
          value: '76324'
        }
        {
          name: 'GitHub__Username'
          value: 'jvandertil'
        }
        {
          name: 'GitHub__Repository'
          value: 'blog'
        }
        {
          name: 'GitHub__EnablePullRequestCreation'
          value: env == 'prd'
        }
        {
          name: 'KeyVault__Url'
          value: 'https://${kvName}${environment().suffixes.keyvaultDns}'
        }
        {
          name: 'KeyVault__KeyName'
          value: 'jvandertil-blog-bot'
        }
      ])

      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      ftpsState: 'Disabled'
      http20Enabled: true

      scmIpSecurityRestrictionsUseMain: false
      scmIpSecurityRestrictions: [
      ]
    }

    httpsOnly: true
    clientCertEnabled: false
  }

  identity: {
    type: 'SystemAssigned'
  }

}

resource functionAppLogs 'microsoft.insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'service'
  scope: functionApp
  properties: {
    logs: [
      {
        category: 'FunctionAppLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
    workspaceId: monitoring.outputs.logAnalyticsId
  }
}

output storageAccountName string = contentStorageAccount.name
output storageAccountWebEndpoint string = contentStorageAccount.properties.primaryEndpoints.web
output functionAppName string = functionApp.name
