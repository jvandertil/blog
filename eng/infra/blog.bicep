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

resource contentStorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
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
      ipRules: [
        for range in cloudFlareIps: {
          value: range
          action: 'Allow'
        }
      ]
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
module keyVault 'keyVault.bicep' = {
  name: 'keyVault'
  params: {
    name: kvName
    location: location
  }
}

resource keyVaultPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = {
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
resource functionAppStorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
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

  resource blobServices 'blobServices' = {
    name: 'default'

    resource deployments 'containers' = {
      name: 'deployments'
    }
  }
}

resource flexHostingPlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: 'asp-fa-flex-${appName}-${env}'
  location: location

  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }

  kind: 'functionapp'
  properties: {
    reserved: true
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: 'asp-fa-${appName}-${env}'
  location: location

  kind: 'linux'

  sku: {
    tier: 'Dynamic'
    name: 'Y1'
  }

  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2025-03-01' = {
  name: 'fa-flex-${appName}-${env}'
  location: location
  kind: 'functionapp,linux'
  properties: {
    serverFarmId: flexHostingPlan.id
    clientAffinityEnabled: false

    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${functionAppStorageAccount.properties.primaryEndpoints.blob}${functionAppStorageAccount::blobServices::deployments.name}'
          authentication: {
            type: 'SystemAssignedIdentity'
          }
        }
      }

      scaleAndConcurrency: {
        instanceMemoryMB: 512
        maximumInstanceCount: 1
      }

      runtime: {
        name: 'dotnet-isolated'
        version: '10.0'
      }
    }

    siteConfig: {
      use32BitWorkerProcess: false

      appSettings: [
        {
          name: 'AzureWebJobsStorage__accountName'
          value: functionAppStorageAccount.name
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: monitoring.outputs.appInsightsConnectionString
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
          value: string(env == 'prd')
        }
        {
          name: 'KeyVault__Url'
          value: 'https://${kvName}${environment().suffixes.keyvaultDns}'
        }
        {
          name: 'KeyVault__KeyName'
          value: 'jvandertil-blog-bot'
        }
      ]

      minTlsVersion: '1.2'
      minTlsCipherSuite: 'TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256'
      scmMinTlsVersion: '1.2'
      ftpsState: 'Disabled'
      http20Enabled: true

      scmIpSecurityRestrictionsUseMain: false
      scmIpSecurityRestrictionsDefaultAction: 'Deny'
      scmIpSecurityRestrictions: []
    }

    httpsOnly: true
    clientCertEnabled: false
  }

  identity: {
    type: 'SystemAssigned'
  }
}

module functionAppRole 'modules/storage-account-role.bicep' = {
  name: 'functionAppRole'
  params: {
    storageAccountName: functionAppStorageAccount.name
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinition: 'Storage Blob Data Contributor'
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
