﻿//HintName: NewStronglyTypedBicepGeneratorTests.Generated.cs
#nullable disable
using System.Collections.Concurrent;
using System.Linq;
using Nuke.Common.Tooling;

namespace BicepTests
{
    [System.CodeDom.Compiler.GeneratedCode("StronglyTypedBicepGenerator", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static partial class Bicep
    {
        [System.CodeDom.Compiler.GeneratedCode("StronglyTypedBicepGenerator", "1.0.0.0")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public static class Deployments
        {
            [System.CodeDom.Compiler.GeneratedCode("StronglyTypedBicepGenerator", "1.0.0.0")]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            private static class AzCli
            {
                private static Nuke.Common.Tooling.Tool Az => ToolResolver.GetPathTool("az");

                public static string GetDeploymentOutputValue(string resourceGroup, string deploymentName, string outputName)
                {
                    var output = Az($"deployment group show --resource-group {resourceGroup} --name {deploymentName} --query properties.outputs.{outputName}.value --output tsv");

                    return ReadFirstLine(output);
                }

                private static string ReadFirstLine(System.Collections.Generic.IReadOnlyCollection<Nuke.Common.Tooling.Output> output)
                {
                    return output.EnsureOnlyStd().First().Text;
                }
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("StronglyTypedBicepGenerator", "1.0.0.0")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public static class Parameters
        {

            [System.CodeDom.Compiler.GeneratedCode("StronglyTypedBicepGenerator", "1.0.0.0")]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            public class InputOnlyParameters
            {
                public string environment { get; set; }
                public string tag { get; set; }
                public int someNumber { get; set; }
                public bool someBool { get; set; }
            }
        }
    }
}
#nullable restore
