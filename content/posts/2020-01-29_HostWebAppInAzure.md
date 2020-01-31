+++
author = "Jos van der Til"
title = "Hosting an ASP.NET Core web application in Azure"
date  = 2020-01-29T11:00:00+01:00
type = "post"
tags = [ ".NET", "ASP.NET", "Azure" ]
+++

As a side project, I am working on a web application that I want to host in Azure eventually.
There is a ton of documentation available around Azure but instructions vary by product.
I have documented the steps I needed to run a web application in Azure.

To make it easier to automate the deployment steps I am avoiding the Azure portal.
I want to script these steps later so that I can automate my deployments.
Everything I want to do can be done using the Azure CLI so, for now, I will be using that.

## Creating the Azure infrastructure
If you are following along, do not forget to authenticate the Azure CLI.
```shell
az login
```

First I need a resource group that will hold all the Azure resources. 
A resource group is essentially a namespace in which different Azure resources can be placed.
All the resources in a resource group should share the same lifecycle.
```shell
az group create --name $ResourceGroupName --location westeurope 
```

To host a WebApp an AppService plan is required, think of it as the web server or web farm that will host the website.
```powershell
az appservice plan create --name $AppServicePlanName `
                          --resource-group $ResourceGroupName `
                          --location westeurope `
                          --sku FREE
```

And the Azure WebApp itself, assigned to the AppService plan.
```shell
az webapp create --name $WebAppName --plan $AppServicePlanName --resource-group $ResourceGroupName
```

Now that the infrastructure is set up in Azure, we can package and deploy the application.

## Packaging and deploying the application
First I need to publish the application in a runnable form. 
The easiest way for a .NET Core application is the `publish` subcommand of the `dotnet` CLI.
```shell
dotnet publish $ProjectPath --output $OutputDir
```

The Azure CLI has support for several different deployment methods.
However eventually I want to deploy from Azure DevOps, and for now, I think the easiest way to facilitate this is the ZIP file method.

I will use some PowerShell to create a zip file containing the published application.
```powershell
Compress-Archive -Path $OutputDir/* -DestinationPath $ApplicationZip
```

And then the following Azure CLI command to deploy the application to the WebApp.
```powershell
az webapp deployment source config-zip --name $WebAppName `
                                       --resource-group $ResourceGroupName `
                                       --src $ApplicationZip
```

And with that you have your ASP.NET Core application running in Azure.

By default, the application will detect the environment is running in as 'Production'.
You can change this, and other application settings using the `config appsettings` command.
For example, to switch the ASP.NET environment to 'Development'.
```powershell
az webapp config appsettings set --name $WebAppName `
                                 --resource-group $ResourceGroupName `
                                 --settings ASPNETCORE_ENVIRONMENT=Development
```
