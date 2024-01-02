using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nuke.Common.IO;
using Nuke.Common.Tooling;

namespace Vandertil.Blog.Pipeline.Azure
{
    public static class AzCli
    {
        public static Tool Az => ToolResolver.GetPathTool("az");

        public static string ReadFirstLine(IReadOnlyCollection<Output> output)
        {
            return output.EnsureOnlyStd().First().Text.Trim();
        }

        public static void DeployTemplate(AbsolutePath templateFile, string resourceGroup, object? parametersObj = null)
        {
            ArgumentStringHandler command = $"deployment group create --mode Complete --template-file {templateFile} --resource-group {resourceGroup}";

            if (parametersObj is not null)
            {
                var parameterObjType = parametersObj.GetType();

                var parameters = parameterObjType
                    .GetProperties()
                    .ToDictionary(x => x.Name, x => x.GetValue(parametersObj, null));

                command.AppendLiteral(" --parameters ");

                var nonEmptyParameters = parameters.Where(x => !(x.Value is null or ""));
                foreach (var entry in nonEmptyParameters)
                {
                    bool isSecret = parameterObjType.GetProperty(entry.Key)?.GetCustomAttribute<BicepGenerator.SecretAttribute>() is not null;

                    command.AppendLiteral(entry.Key);
                    command.AppendLiteral("=");
                    AppendFormatted(entry.Value, ref command, isSecret ? "r" : null);
                    command.AppendLiteral(" ");
                }
            }

            try
            {
                Az(command);
            }
            catch
            {
                Az($"deployment group list --resource-group {resourceGroup}");

                throw;
            }
        }

        public static TDeployment DeployTemplate<TDeployment>(AbsolutePath templateFile, string resourceGroup, object? parametersObj = null)
        {
            DeployTemplate(templateFile, resourceGroup, parametersObj);

            return (TDeployment)Activator.CreateInstance(typeof(TDeployment), new object[] { resourceGroup })!;
        }

        private static void AppendFormatted(object? value, ref ArgumentStringHandler handler, string? format)
        {
            if (value is string)
            {
                // Special case string as it is an IEnumerable<char> as well.
                handler.AppendFormatted(value, format: format);
            }
            else if (value is IEnumerable enumerable)
            {
                var arrayArgument = FormatArrayValue(enumerable);
                handler.AppendFormatted(arrayArgument, format: format);
            }
            else
            {
                handler.AppendFormatted(value, format: format);
            }
        }

        private static string FormatArrayValue(IEnumerable enumerable)
        {
            var values = enumerable
                .Cast<object>()
                .Select(item => FormattableString.Invariant($"'{item}'"));

            var formattedValues = string.Join(",", values);
            return $"[{formattedValues}]";
        }
    }
}
