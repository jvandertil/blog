param env string
param appName string
param location string = resourceGroup().location

var workspaceName = 'la-${appName}-${env}'
var appInsightsName = 'ai-${appName}-${env}'

resource workspace 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: workspaceName
  location: location
  properties: {
    retentionInDays: 90
    workspaceCapping: {
       dailyQuotaGb: 10
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties:{
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
  }
}

output logAnalyticsId string = workspace.id
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
output appInsightsConnectionString string = appInsights.properties.ConnectionString
