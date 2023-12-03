module someAppService 'moduleTemplate.bicep' = {
  params: {
    appServiceName: '${appServicePrefix}-test-${env}'

    logAnalyticsWorkspaceId: logAnalyticsWorkspaceId
  }
  name: 'appServiceFromTemplate'
}

output thisProperty string = 'test'
output thatProperty string = 'test 2'"
