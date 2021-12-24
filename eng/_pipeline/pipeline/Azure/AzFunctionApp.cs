using System;
using Nuke.Common.IO;

namespace Vandertil.Blog.Pipeline.Azure
{
    public static class AzFunctionApp
    {
        public static IDisposable CreateTemporaryScmFirewallRule(string resourceGroup, string functionAppName, string ipAddress)
        {
            AzCli.Az($"functionapp config access-restriction add --scm-site true --resource-group {resourceGroup} --name {functionAppName} --priority 100 --action Allow --rule-name PipelineDeployment --ip-address {ipAddress} ");

            return new AzCliCleanupDisposable($"functionapp config access-restriction remove --scm-site true --resource-group {resourceGroup} --name {functionAppName} --rule-name PipelineDeployment");
        }

        public static void DeployZipPackage(AbsolutePath zipPackage, string resourceGroup, string functionAppName)
        {
            AzCli.Az($"functionapp deployment source config-zip --resource-group {resourceGroup} --name {functionAppName} --src {zipPackage} --only-show-errors");
        }
    }
}
