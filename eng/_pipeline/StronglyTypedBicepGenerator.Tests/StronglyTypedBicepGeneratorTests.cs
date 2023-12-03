using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyMSTest;
using VerifyTests;

namespace Vandertil.Blog.Pipeline.StronglyTypedBicepGenerator.Tests;

[TestClass]
public class StronglyTypedBicepGeneratorTests : VerifyBase
{
    [TestMethod]
    public Task EmitsAttributeClass()
    {
        return Verify($$"""
            using BicepGenerator;
            namespace BicepTests
            {
                public static partial class Bicep
                {
                }
            }
            """);
    }

    [TestMethod]
    public Task EmitsNamedDeploymentWithOutputs()
    {
        return Verify($$"""
            using BicepGenerator;
            namespace BicepTests
            {
                [BicepFile("{{TestFile("outputOnly.bicep")}}")]
                public static partial class Bicep
                {
                }
            }
            """);
    }

    [TestMethod]
    public Task EmitsNamedDeploymentWithOutputsWhenUsingFullyQualifiedAttribute()
    {
        return Verify($$"""
            namespace BicepTests
            {
                [BicepGenerator.BicepFile("{{TestFile("outputOnly.bicep")}}")]
                public static partial class Bicep
                {
                }
            }
            """);
    }

    [TestMethod]
    public Task EmitsParametersWithInputs()
    {
        return Verify($$"""
            using BicepGenerator;
            namespace BicepTests
            {
                [BicepFile("{{TestFile("inputOnly.bicep")}}")]
                public static partial class Bicep
                {
                }
            }
            """);
    }

    [TestMethod]
    public Task EmitsModulesAsSeparateDeployments()
    {
        return Verify($$"""
            using BicepGenerator;
            namespace BicepTests
            {
                [BicepFile("{{TestFile("templateWithModule.bicep")}}")]
                public static partial class Bicep
                {
                }
            }
            """);
    }

    private static string TestFile(string fileName)
    {
        return $"TestFiles{Path.DirectorySeparatorChar}{fileName}";
    }

    private Task Verify(string source, [CallerFilePath] string path = null)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, path: path);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree },
            references: references);

        var generator = new StronglyTypedBicepGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation);

        var verifierSettings = new VerifySettings();

        return Verify(driver, verifierSettings);
    }
}
