+++
author = "Jos van der Til"
title = "Connecting to your Azure SQL database over a Private Endpoint"
date  = 2024-05-03T15:00:00+02:00
type = "post"
tags = [ "Bicep", "Azure", "SQL Server" ]
+++

In a corporate network, internal database servers are usually heavily firewalled in separate network segments.
However, when we deploy our database in Azure, we can connect to it directly over the internet.

To improve the security of your database, you should use a private link (also known as Private Endpoint) to connect to your database from your application.
This will route the traffic over internal Azure connections, and you can disallow any public access to the database server.

A private endpoint will expose the resource it is connected to as a private IP address in a VNET. Using a Private DNS Zone we will overwrite the database server hostname to resolve to the private IP address.

In this post I will provide the Bicep template I use to deploy a private link to a Azure SQL database that is used by an Azure App Service.

First lets set up some basic naming and parameters that we need.
```bicep
param sqlServerId string
param location string = 'westeurope'

var applicationName = 'example-app'
var environment = 'dev'

var networkSecurityGroupName = 'nsg-${applicationName}-${environment}'
var vnetName = 'vnet-${applicationName}-${environment}'
var sqlPrivateEndpointName = 'pl-sql-${applicationName}-${environment}'
```

Set up a Virtual Network (with a (unconfigured) Network Security Group) if not already present.
```bicep
resource nsg 'Microsoft.Network/networkSecurityGroups@2022-11-01' = {
    name: networkSecurityGroupName
    location: location
}

resource vnet 'Microsoft.Network/virtualNetworks@2022-11-01' = {
    name: vnetName
    location: location
    properties: {
        addressSpace: {
            addressPrefixes: [
                '192.168.0.0/16' // Just use the entire range for this example.
            ]
        }
        subnets: [
            // Separate subnet for app services
            // We do not need a service endpoint as we will use a private link
            {
                name: 'web'
                properties: {
                    networkSecurityGroup: {
                        id: nsg.id
                    }
                }

                addressPrefix: '192.168.0.0/24'

                delegations: [
                    {
                        name: 'appservice'
                        properties: {
                            serviceName: 'Microsoft.Web/serverFarms'
                        }
                    }
                ]
            }
            // Separate subnet for SQL Server private links
            {
                name: 'sql'
                properties: {
                    networkSecurityGroup: {
                        id: nsg.id
                    }
                }

                addressPrefix: '192.168.1.0/24'
            }
        ]
    }

    // Expose subnets as subresources here for easy access.
    // Subnets should be defined in the VNET properties, otherwise ARM will try to delete the subnet.
    // That will only work on a clean run, and fail once you redeploy the template on an existing environment.
    // See https://github.com/Azure/bicep/issues/4653 for details.
    resource webSubnet 'subnets' existing = {
        name: 'web'
    }

    resource sqlSubnet 'subnets' existing = {
        name: 'sql'
    }
}
```

Now for the work to get the private endpoint working.
First we will set up a Private DNS zone that will hold the entries for the private linked SQL servers.
```bicep
resource sqlPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
    name: 'privatelink${environment().suffixes.sqlServerHostname}'
    location: 'global'

    resource dnsVnetLink 'virtualNetworkLinks' = {
        name: 'example-app-vnet'
        location: 'global'

        properties: {
            registrationEnabled: false // We do not need auto registration of virtual machines in this DNS zone.
            virtualNetwork: {
                id: vnet.id
            }
        }
    }
}
```

Next we create the private link to the SQL server, which is identified by the `sqlServerId` variable.
We also register this private link in the Private DNS Zone we just created.

```bicep
resource sqlPrivateLink 'Microsoft.Network/privateEndpoints@2022-11-01' = {
    name: sqlPrivateEndpointName
    location: location

    properties: {
        subnet: {
            id: vnet::sqlSubnet.id
        }

        // This is totally optional, but standardizes the name between redeployments.
        customNetworkInterfaceName: '${sqlPrivateEndpointName}.nic.${guid(resourceGroup().id)}'

        privateLinkServiceConnections: [
            {
                name: sqlPrivateEndpointName
                properties: {
                    // Plug your SQL Server id here
                    privateLinkServiceId: sqlServerId

                    groupIds: [
                        'sqlServer'
                    ]
                }
            }
        ]
    }

    resource dns 'privateDnsZoneGroups' = {
        name: 'default'
        properties: {
            privateDnsZoneConfigs: [
                {
                    name: 'privatelink-database-windows-net'
                    properties: {
                        privateDnsZoneId: sqlPrivateDnsZone.id
                    }
                }
            ]
        }
    }
}
```

That should be enough to get the private link working!

Note that you do not need to update the SQL server hostname in the connection string, just using `sql-server.database.windows.net` will CNAME to `sql-server.privatelink.database.windows.net` and then resolve to the private IP address that has been assigned to the private link.
