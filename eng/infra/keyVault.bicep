param env string

var appName = 'jvandertilblog'
var location = resourceGroup().location

resource keyVault 'Microsoft.KeyVault/vaults@2021-06-01-preview' = {
  name: 'kv-${appName}-${env}'
  location: location

  properties: {
    enableRbacAuthorization: true
    enabledForTemplateDeployment: true

    sku: {
      name: 'standard'
      family: 'A'
    }

    networkAcls: {
      bypass: 'None'
      defaultAction: 'Deny'
    }

    tenantId: tenant().tenantId
    softDeleteRetentionInDays: 7
    accessPolicies: []
  }
}

output keyVaultName string = keyVault.name
