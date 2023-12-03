using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.ValueInjection;

namespace Vandertil.Blog.Pipeline.Azure
{
    public static class AzCli
    {
        public static Tool Az => ToolResolver.GetPathTool("az");

        public static string ReadFirstLine(IReadOnlyCollection<Output> output)
        {
            return output.EnsureOnlyStd().First().Text.Trim();
        }

        public static void DeployTemplate(AbsolutePath templateFile, string resourceGroup, object parametersObj = null)
        {
            var commandBuilder = new StringBuilder($"deployment group create --mode Complete --template-file \"{templateFile}\" --resource-group {resourceGroup}");

            if (parametersObj is not null)
            {
                var parameters = parametersObj
                    .GetType()
                    .GetProperties()
                    .ToDictionary(x => x.Name, x => x.GetValue(parametersObj, null));

                commandBuilder.Append(" --parameters ");

                foreach (var entry in parameters)
                {
                    commandBuilder.Append(entry.Key).Append('=').Append(FormattableString.Invariant($"\"{entry.Value}\" "));
                }
            }

            try
            {
                Az(commandBuilder.ToString());
            }
            catch
            {
                Az($"deployment group list --resource-group {resourceGroup}");

                throw;
            }
        }

        public static TDeployment DeployTemplate<TDeployment>(AbsolutePath templateFile, string resourceGroup, object parametersObj = null)
        {
            DeployTemplate(templateFile, resourceGroup, parametersObj);

            return (TDeployment)Activator.CreateInstance(typeof(TDeployment), new object[] { resourceGroup });
        }
    }
}
