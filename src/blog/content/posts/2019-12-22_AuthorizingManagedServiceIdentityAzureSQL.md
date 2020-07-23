+++
author = "Jos van der Til"
title = "Authorizing Managed Service Identity in Azure SQL Database"
date  = 2019-12-22T14:00:00+01:00
type = "post"
tags = [ ".NET", "Azure", "SqlServer" ]
+++

When trying to deploy a simple web application and Azure SQL database through Azure DevOps pipelines,
I wanted to use a system managed application identity to authorize the web application to access the database.
This requires running something like the following SQL script on the Azure SQL database.
```sql
CREATE USER [<identity-name>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [<identity-name>];
ALTER ROLE db_datawriter ADD MEMBER [<identity-name>];
ALTER ROLE db_ddladmin ADD MEMBER [<identity-name>];
```

I was having a lot of trouble getting the Azure SqlCmd task to work, while the error(s) it was showing was not helpful at all.
For example:
```powershell
Failed to reach SQL server <server-address>,1433. One or more errors occurred.
Error Message : System.Management.Automation.ActionPreferenceStopException: The running command stopped because the preference variable "ErrorActionPreference" or common parameter is set to Stop: One or more errors occurred.
   at System.Management.Automation.ExceptionHandlingOps.CheckActionPreference(FunctionContext funcContext, Exception exception)
   at System.Management.Automation.Interpreter.ActionCallInstruction`2.Run(InterpretedFrame frame)
   at System.Management.Automation.Interpreter.EnterTryCatchFinallyInstruction.Run(InterpretedFrame frame)
   at System.Management.Automation.Interpreter.EnterTryCatchFinallyInstruction.Run(InterpretedFrame frame)
Message To Parse: System.Management.Automation.ActionPreferenceStopException: The running command stopped because the preference variable "ErrorActionPreference" or common parameter is set to Stop: One or more errors occurred.
   at System.Management.Automation.ExceptionHandlingOps.CheckActionPreference(FunctionContext funcContext, Exception exception)
   at System.Management.Automation.Interpreter.ActionCallInstruction`2.Run(InterpretedFrame frame)
   at System.Management.Automation.Interpreter.EnterTryCatchFinallyInstruction.Run(InterpretedFrame frame)
   at System.Management.Automation.Interpreter.EnterTryCatchFinallyInstruction.Run(InterpretedFrame frame)
```
Apparently 'One or more errors occurred' is the best it could show me most of the time.
So, I created a small tool (available on [GitHub](https://github.com/jvandertil/AzureSqlAppIdentityAuthTool)) that would authorize the identity for me.

## Running from Azure DevOps
Since the database is protected by a firewall, I have to add the Azure DevOps agent IP address to the firewall rules temporarily.
The easiest way I could think of was running a PowerShell script to set a variable.
```powershell
$clientIp = (curl https://icanhazip.com).Content

Write-Host "##vso[task.setvariable variable=Agent.IpAddress;]$clientIp"
Write-Host Agent IP: $clientIp
```
Followed by an Azure CLI command step that will create the firewall rule.
```powershell
az sql server firewall-rule create --start-ip-address $(Agent.IpAddress) `
                                   --end-ip-address $(Agent.IpAddress) `
                                   --server $(SqlServerName) `
                                   --resource-group $(ResourceGroupName) `
                                   --name $(SqlServerFirewallRuleName)
```

Then I can run my authorization tool to get the database set up.
```powershell
.\AzureSqlAppIdentityAuthTool.exe --connection-string "$(SqlServerInstallConnectionString)" `
                                  --identity $(AppServerAppName) `
                                  --no-ddladmin
```

I do not want to allow the agent IP address forever, so I added an Azure CLI step to clean it up after I am done.
```powershell
az sql server firewall-rule delete --name $(SqlServerFirewallRuleName) `
                                   --resource-group $(ResourceGroupName) `
                                   --server $(SqlServerName)
```
This is set to run 'Even if a previous task has failed, even if the deployment was canceled' so that it should always run.

To check the permissions for the identity you can use the following query:
```sql
SELECT [member].[name] AS username, 
       [role].[name] AS rolename
  FROM [sys].[database_role_members]
       JOIN [sys].[database_principals] [role] 
       ON [role_principal_id] = [role].principal_id

       JOIN [sys].[database_principals] [member] 
       ON [member_principal_id] = [member].principal_id

 WHERE [member].[name] = '<identity name>'
```

I hope this saves someone from going through the SqlCmd pain I went through before giving up and writing something myself.
