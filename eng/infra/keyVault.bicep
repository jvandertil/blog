param name string
param ipRules array

var location = resourceGroup().location

resource keyVault 'Microsoft.KeyVault/vaults@2021-06-01-preview' = {
  name: name
  location: location

  properties: {
    sku: {
      name: 'standard'
      family: 'A'
    }

    networkAcls: {
      bypass: 'None'
      defaultAction: 'Deny'
      ipRules: [for ip in ipRules: {
        value: ip
      }]
    }

    tenantId: tenant().tenantId
    softDeleteRetentionInDays: 7
    accessPolicies: []
  }
}
