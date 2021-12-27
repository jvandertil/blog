using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;

namespace Vandertil.Blog.Pipeline.Azure
{
    public static class AzStorage
    {
        public static IDisposable AllowIpAddressTemporary(string resourceGroup, string storageAccountName, string ipAddress)
        {
            AzCli.Az($"storage account network-rule add --resource-group {resourceGroup} --account-name {storageAccountName} --ip-address {ipAddress}");

            return new AzCliCleanupDisposable($"storage account network-rule remove --resource-group {resourceGroup} --account-name {storageAccountName} --ip-address {ipAddress}");
        }

        public static IDisposable GetPrimaryStorageKeyWithAutoCycle(string resourceGroup, string storageAccountName, out string primaryStorageKey)
        {
            var output = AzCli.Az($"storage account keys list --resource-group {resourceGroup} --account-name {storageAccountName} --query \"[?keyName == 'key1'].value | [0]\" --output tsv", logOutput: false);
            primaryStorageKey = AzCli.ReadFirstLine(output);

            return new AzCliCleanupDisposable($"storage account keys renew --resource-group {resourceGroup} --account-name {storageAccountName} --key primary --output none");
        }

        public static async Task SyncFolderToContainerAsync(AbsolutePath path, string resourceGroup, string storageAccountName, string containerName)
        {
            using (GetPrimaryStorageKeyWithAutoCycle(resourceGroup, storageAccountName, out string storageKey))
            {
                await CheckForPermissions(storageAccountName, storageKey, containerName);
                AzCli.Az($"storage blob sync --source {path} --container {containerName} --account-name {storageAccountName} --account-key {storageKey} --only-show-errors", outputFilter: x => x.Replace(storageKey, "***"));
            }
        }

        private static async Task CheckForPermissions(string storageAccount, string accessKey, string containerName)
        {
            Logger.Info("Checking if network rule has propagated...");

            var WaitTime = TimeSpan.FromSeconds(5);
            var TimeOut = TimeSpan.FromMinutes(1);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            bool permissionsActivated = false;
            while (!permissionsActivated && stopwatch.Elapsed < TimeOut)
            {
                try
                {
                    AzCli.Az($"storage blob list --account-name {storageAccount} --account-key {accessKey} --container-name {containerName} --num-results 1", logOutput: false, logInvocation: false);
                    permissionsActivated = true;
                }
                catch
                {
                    Logger.Info("Storage request failed, sleeping and retrying...");
                }

                await Task.Delay(WaitTime).ConfigureAwait(false);
            }

            if (!permissionsActivated)
            {
                throw new TimeoutException($"Network rule was not propagated within the specified time limit: {TimeOut}");
            }
        }
    }
}
