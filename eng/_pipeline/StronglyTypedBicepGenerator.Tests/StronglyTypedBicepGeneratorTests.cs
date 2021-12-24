using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Vandertil.Blog.Pipeline.StronglyTypedBicepGenerator.Tests
{
    [TestClass]
    public class StronglyTypedBicepGeneratorTests
    {
        private const string Namespace = "MyCode";
        private const string ClassName = "Bicep";

        [TestMethod]
        public void EmitsAttributeClass()
        {
            Compilation inputCompilation = CreateCompilation("");

            RunGenerator(inputCompilation, out var outputCompilation);

            outputCompilation.Assembly.GetTypeByMetadataName("BicepGenerator.BicepFileAttribute").Should().NotBeNull();
        }

        [TestMethod]
        public void EmitsNamedDeploymentWithOutputs()
        {
            File.WriteAllText("outputOnly.bicep",
                @"output thisProperty string = 'test'
output thatProperty string = 'test 2'");

            Compilation inputCompilation = CreateCompilation($@"
using BicepGenerator;
namespace {Namespace}
{{
    [BicepFile(""outputOnly.bicep"")]
    public static partial class {ClassName}
    {{
    }}
}}
");

            RunGenerator(inputCompilation, out var outputCompilation);
            var assembly = outputCompilation.Assembly;

            AssertDeploymentProperty(assembly, "OutputOnly", "ThisProperty", typeof(string));
            AssertDeploymentProperty(assembly, "OutputOnly", "ThatProperty", typeof(string));
        }

        [TestMethod]
        public void EmitsNamedDeploymentWithOutputsWhenUsingFullyQualifiedAttribute()
        {
            File.WriteAllText("outputOnly.bicep",
                @"output thisProperty string = 'test'
output thatProperty string = 'test 2'");
            Compilation inputCompilation = CreateCompilation($@"
namespace {Namespace}
{{
    [BicepGenerator.BicepFile(""outputOnly.bicep"")]
    public static partial class {ClassName}
    {{
    }}
}}
");

            RunGenerator(inputCompilation, out var outputCompilation);
            var assembly = outputCompilation.Assembly;

            AssertDeploymentProperty(assembly, "OutputOnly", "ThisProperty", typeof(string));
            AssertDeploymentProperty(assembly, "OutputOnly", "ThatProperty", typeof(string));
        }

        [TestMethod]
        public void EmitsParametersWithInputs()
        {
            File.WriteAllText("inputOnly.bicep",
                @"param environment string
param tag string = 'test'
param someNumber int
param someBool bool");

            Compilation inputCompilation = CreateCompilation($@"
using BicepGenerator;
namespace {Namespace}
{{
    [BicepFile(""inputOnly.bicep"")]
    public static partial class {ClassName}
    {{
    }}
}}
");

            RunGenerator(inputCompilation, out var outputCompilation);
            var assembly = outputCompilation.Assembly;

            AssertParametersProperty(assembly, "InputOnlyParameters", "environment", typeof(string));
            AssertParametersProperty(assembly, "InputOnlyParameters", "tag", typeof(string));
            AssertParametersProperty(assembly, "InputOnlyParameters", "someNumber", typeof(int));
            AssertParametersProperty(assembly, "InputOnlyParameters", "someBool", typeof(bool));
        }

        [TestMethod]
        public void EmitsModulesAsSeparateDeployments()
        {
            File.WriteAllText("outputOnly.bicep",
                @"module someAppService 'someTemplate.bicep' = {
  params: {
    appServiceName: '${appServicePrefix}-test-${env}'

    logAnalyticsWorkspaceId: logAnalyticsWorkspaceId
  }
  name: 'appServiceFromTemplate'
}

output thisProperty string = 'test'
output thatProperty string = 'test 2'");

            File.WriteAllText("someTemplate.bicep", @"output appServiceName string = appService.name");

            Compilation inputCompilation = CreateCompilation($@"
using BicepGenerator;
namespace {Namespace}
{{
    [BicepFile(""outputOnly.bicep"")]
    public static partial class {ClassName}
    {{
    }}
}}
");

            RunGenerator(inputCompilation, out var outputCompilation);
            var assembly = outputCompilation.Assembly;

            AssertDeploymentProperty(assembly, "AppServiceFromTemplate", "AppServiceName", typeof(string));
        }

        private static void AssertDeploymentProperty(IAssemblySymbol assembly, string className, string propertyName, Type propertyType)
        {
            var type = assembly.GetTypeByMetadataName($"{Namespace}.{ClassName}+Deployments+{className}");

            var property = type.GetMembers(propertyName)[0] as IPropertySymbol;
            property.Type.Name.Should().Be(propertyType.Name);
        }

        private static void AssertParametersProperty(IAssemblySymbol assembly, string className, string propertyName, Type propertyType)
        {
            var type = assembly.GetTypeByMetadataName($"{Namespace}.{ClassName}+Parameters+{className}");

            var property = type.GetMembers(propertyName)[0] as IPropertySymbol;
            property.Type.Name.Should().Be(propertyType.Name);
        }

        private static GeneratorDriverRunResult RunGenerator(Compilation inputCompilation, out Compilation outputCompilation)
        {
            // directly create an instance of the generator
            // (Note: in the compiler this is loaded from an assembly, and created via reflection at runtime)
            StronglyTypedBicepGenerator generator = new StronglyTypedBicepGenerator();

            // Create the driver that will control the generation, passing in our generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Run the generation pass
            // (Note: the generator driver itself is immutable, and all calls return an updated version of the driver that you should use for subsequent calls)
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out outputCompilation, out var diagnostics);

            Assert.IsTrue(diagnostics.IsEmpty, diagnostics.Select(x => x.ToString()).Aggregate(new StringBuilder(), (sb, input) => sb.AppendLine(input), x => x.ToString()));
            Assert.IsTrue(outputCompilation.GetDiagnostics().IsEmpty, outputCompilation.GetDiagnostics().Select(x => x.ToString()).Aggregate(new StringBuilder(), (sb, input) => sb.AppendLine(input), x => x.ToString()));

            return driver.GetRunResult();
        }

        private static Compilation CreateCompilation(string source)
        {
            var references = new List<MetadataReference>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                if (!assembly.IsDynamic)
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
            }
            references.Add(MetadataReference.CreateFromFile(typeof(Nuke.Common.NukeBuild).GetTypeInfo().Assembly.Location));

            return CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest), $"{ClassName}.cs") },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithSpecificDiagnosticOptions(new[]
                {
                    new KeyValuePair<string, ReportDiagnostic>("CS1701", ReportDiagnostic.Suppress),
                    new KeyValuePair<string, ReportDiagnostic>("CS8019", ReportDiagnostic.Suppress),
                }));
        }
    }
}
