+++
author = "Jos van der Til"
title = "Easy way to set Azure RBAC roles in Bicep"
date  = 2022-06-21T21:00:00+02:00
type = "post"
tags = [ "Bicep", "Azure" ]
+++

When deploying resources in Azure using Bicep, occasionally you will have to assign rights to a user or principal to perform certain actions.
For example, authorizing an app service to access a storage account.

Initially you would create something like this:
```bicep
// Assume we have an app service with a System Assigned managed service identity
var principalId = appService.identity.principalId;

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-06-01' existing = {
    name: 'some-existing-storage-account'
}

resource roleAuthorization 'Microsoft.Authorization/roleAssignments@2015-07-01' = {
    name: guid(storageAccount.id, resourceGroup().id, principalId)
    scope: storageAccount
    properties: {
        principalId: principalId
        roleDefinitionId: '/providers/Microsoft.Authorization/roleDefinitions/ba92f5b4-2d11-453d-a403-e96b0029c9fe' // Storage Blob Data Contributor
    }
}
```

I came up with the following Bicep module which shows a nice way to hide the nasty details such as the role guids in a module.
```bicep
param storageAccountName string
param principalId string

@allowed([
    'Storage Blob Data Contributor'
    'Storage Blob Data Reader'
])
param roleDefinition string

var roles = {
    // See https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles for these mappings
    'Storage Blob Data Contributor' = '/providers/Microsoft.Authorization/roleDefinitions/ba92f5b4-2d11-453d-a403-e96b0029c9fe'
    'Storage Blob Data Reader' = '/providers/Microsoft.Authorization/roleDefinitions/2a2b9908-6ea1-4ae2-8e65-a410df84e7d1'
}

var roleDefinitionId = roles[roleDefinition]

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-06-01' existing = {
    name: storageAccountName
}

resource roleAuthorization 'Microsoft.Authorization/roleAssignments@2015-07-01' = {
    // Generate a unique but deterministic resource name
    name: guid('storage-rbac', storageAccount.id, resourceGroup().id, principalId, roleDefinitionId)
    scope: storageAccount
    properties: {
        principalId: principalId
        roleDefinitionId: roleDefinitionId
    }
}
```

This makes the Bicep files a lot more readable, especially when you have to assign roles more often.

Creating a module to do this also has the advantage that you can change the scope, for example when the storage account is part of a different resource group.

Cleaned up initial example:
```bicep
// Assume we have an app service with a System Assigned managed service identity
var principalId = appService.identity.principalId;

// I used 'storageAuth.bicep' as the file name, but doesn't matter.
module roleAuthorization 'storageAuth.bicep' = {
    name: 'roleAuthorization'
    properties: {
        principalId: principalId
        storageAccountName: 'some-existing-storage-account'
        roleDefinition: 'Storage Blob Data Contributor'
    }
}
```

I hope someone has some use for this as well.
