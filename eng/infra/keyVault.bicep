param name string

param location string = resourceGroup().location

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
      defaultAction: 'Allow' // Function app can use more ip addresses than we can configure. Alternative is to whitelist the entire Azure region.
    }

    tenantId: tenant().tenantId
    softDeleteRetentionInDays: 7
    accessPolicies: []
  }
}
