//HintName: NewStronglyTypedBicepGeneratorTests.Generated.cs
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
            public class OutputOnly
            {
                private static readonly ConcurrentDictionary<(string resourceGroup, string outputName), string> _outputsCache = new ConcurrentDictionary<(string resourceGroup, string outputName), string>();

                private static bool TryGetFromCache(string resourceGroup, string outputName, out string outputValue)
                {
                    return _outputsCache.TryGetValue((resourceGroup, outputName), out outputValue);
                }

                private static void AddToCache(string resourceGroup, string outputName, string outputValue)
                {
                    _outputsCache.TryAdd((resourceGroup, outputName), outputValue);
                }

                private readonly string _resourceGroup;
                private readonly bool _useCache;

                public OutputOnly(string resourceGroup) : this(resourceGroup, false) { }
                public OutputOnly(string resourceGroup, bool noCache)
                {
                    _resourceGroup = resourceGroup ?? throw new System.ArgumentNullException(nameof(resourceGroup));
                    _useCache = !noCache;
                }

                public string ThatProperty
                {
                    get
                    {
                        string value;
                        if (!_useCache || !TryGetFromCache(_resourceGroup, "thatProperty", out value))
                        {
                            value = AzCli.GetDeploymentOutputValue(_resourceGroup, "outputOnly", "thatProperty");

                            if (_useCache)
                            {
                                AddToCache(_resourceGroup, "thatProperty", value);
                            }
                        }

                        return value;
                    }
                }

                public string ThisProperty
                {
                    get
                    {
                        string value;
                        if (!_useCache || !TryGetFromCache(_resourceGroup, "thisProperty", out value))
                        {
                            value = AzCli.GetDeploymentOutputValue(_resourceGroup, "outputOnly", "thisProperty");

                            if (_useCache)
                            {
                                AddToCache(_resourceGroup, "thisProperty", value);
                            }
                        }

                        return value;
                    }
                }
            }

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
            public class OutputOnlyParameters
            {
            }
        }
    }
}
#nullable restore
