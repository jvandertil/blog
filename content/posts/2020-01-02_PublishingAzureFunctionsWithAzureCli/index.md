+++
author = "Jos van der Til"
title = "Publishing Azure Functions project to Azure using the CLI"
date  = 2020-01-02T14:00:00+01:00
type = "post"
tags = [ "CSharp", ".NET", "Azure", "Serverless" ]
draft = true
+++

Checkout [sample project](AzureFunctions101.zip)

```shell
az group create --name $ResourceGroup --location westeurope
```

```powershell
az storage account create --name $StorageAccount `
                          --resource-group $ResourceGroup `
                          --location westeurope `
                          --kind storagev2

az functionapp create --name quintorfuncdemo `
                      --resource-group $ResourceGroup `
                      --storage $StorageAccount `
                      --consumption-plan-location westeurope
```

```powershell
dotnet publish .\AzureFunctions101\AzureFunctions101.csproj -c Release -o .\artifacts\

Compress-Archive .\artifacts\* -DestinationPath .\functionapp.zip -Force
```

Pushing the application to Azure.
```powershell
az functionapp deployment source config-zip --src .\functionapp.zip `
                                            --resource-group $ResourceGroup `
                                            --name quintorfuncdemo
```