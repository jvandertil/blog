using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Vandertil.Blog.Pipeline.StronglyTypedBicepGenerator.Parser;

namespace Vandertil.Blog.Pipeline.StronglyTypedBicepGenerator
{
    [Generator]
    public class StronglyTypedBicepGenerator : ISourceGenerator
    {
        internal const string AttributeNamespace = "Vandertil.BicepGenerator";
        internal const string AttributeName = "BicepFileAttribute";

        internal class SyntaxReceiver : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax> DecoratorRequestingClasses { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is ClassDeclarationSyntax cds
                    && cds.AttributeLists.Any()
                    && HasBicepLocationAttribute(cds))
                {
                    DecoratorRequestingClasses.Add(cds);
                }
            }

            private static bool HasBicepLocationAttribute(ClassDeclarationSyntax cds)
            {
                return cds.AttributeLists
                    .SelectMany(x => x.Attributes)
                    .Select(x => x.Name).OfType<NameSyntax>()
                    .Any(x => x.ToString() == "BicepFile" || x.ToString() == "Vandertil.BicepGenerator.BicepFile");
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization(i =>
            {
                var codeWriter = new CodeWriter(LanguageVersion.Default);

                codeWriter
                    .AppendLine("using System;")
                    .AppendLine()
                    .AppendLine($"namespace {AttributeNamespace}")
                    .OpenBlock()
                        .EmitGeneratedCodeAttribute()
                        .EmitExcludeFromCodeCoverageAttribute()
                        .AppendLine("[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]")
                        .AppendLine($"internal sealed class {AttributeName} : Attribute")
                        .OpenBlock()
                            .AppendLine("public string FileName { get; }")
                            .AppendLine()
                            .AppendLine("public BicepFileAttribute(string fileName)")
                            .OpenBlock()
                                .AppendLine("FileName = fileName;")
                            .CloseBlock()
                        .CloseBlock()
                        .AppendLine()
                        .EmitGeneratedCodeAttribute()
                        .EmitExcludeFromCodeCoverageAttribute()
                        .AppendLine("[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]")
                        .AppendLine("internal sealed class SecretAttribute : Attribute")
                        .OpenBlock()
                            .AppendLine("public SecretAttribute() { }")
                        .CloseBlock()
                    .CloseBlock();

                i.AddSource("BicepFileAttribute.Generated", codeWriter.ToString());
            });
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            {
                throw new ArgumentException("Received invalid receiver in Execute step");
            }

            if (context.Compilation is not CSharpCompilation comp)
            {
                return;
            }

            foreach (var classDefinition in receiver.DecoratorRequestingClasses)
            {
                var filePath = classDefinition.SyntaxTree.FilePath;

                var model = context.Compilation.GetSemanticModel(classDefinition.SyntaxTree);

                var bicepFiles = classDefinition.AttributeLists
                   .SelectAttribute($"{AttributeNamespace}.{AttributeName}", model)
                   .Select(x => x.ArgumentList!.Arguments.Single().ToString().Trim('\"'))
                   .Select(file => Path.Combine(Path.GetDirectoryName(filePath), file))
                   .Select(BicepParser.ParseFile)
                   .ToList();

                var sourceBuilder = new CodeWriter(comp.LanguageVersion);
                WriteFileHeaderAndUsings(sourceBuilder);

                OpenNamespace(classDefinition, sourceBuilder);

                OpenContainingClassBlocks(classDefinition, sourceBuilder);

                WriteDeployments(bicepFiles, sourceBuilder);

                WriteParameters(bicepFiles, sourceBuilder);

                CloseContainingClassBlocks(classDefinition, sourceBuilder);

                CloseNamespace(classDefinition, sourceBuilder);

                sourceBuilder.AppendLine("#nullable restore");

                var result = sourceBuilder.ToString();
                context.AddSource($"{Path.GetFileNameWithoutExtension(classDefinition.SyntaxTree.FilePath)}.Generated", SourceText.From(result, Encoding.UTF8));
            }
        }

        private static void WriteParameters(List<BicepFile> bicepFiles, CodeWriter sourceBuilder)
        {
            if (!bicepFiles.Any(x => x.Parameters.Count > 0))
            {
                // No bicep file has parameters, skip this.
                return;
            }

            sourceBuilder
                    .EmitGeneratedCodeAttribute()
                    .EmitExcludeFromCodeCoverageAttribute()
                    .AppendLine("public static class Parameters")
                    .OpenBlock();

            foreach (var file in bicepFiles.OrderBy(x => x.Name))
            {
                var name = file.Name;
                var className = name.Replace('-', '_').Pascalize() + "Parameters";

                sourceBuilder
                    .AppendLine()
                    .EmitGeneratedCodeAttribute()
                    .EmitExcludeFromCodeCoverageAttribute()
                    .Append("public class ").AppendLine(className)
                    .OpenBlock();

                foreach (var param in file.Parameters)
                {
                    if (param.IsSecret)
                    {
                        sourceBuilder.AppendLine($"[{AttributeNamespace}.Secret]");
                    }

                    string required = (param.Required && sourceBuilder.LanguageVersion >= LanguageVersion.CSharp11) ? "required " : "";
                    string setOrInit = (sourceBuilder.LanguageVersion >= LanguageVersion.CSharp9) ? "init" : "set";

                    sourceBuilder
                        .AppendLine($"public {required}{BicepDataTypeToTypeString(param.DataType)} {param.Name} {{ get; {setOrInit}; }}");
                }

                sourceBuilder
                    .CloseBlock(); // nested class
            }

            sourceBuilder
                .CloseBlock();

            static string BicepDataTypeToTypeString(BicepDataType type)
            {
                return type switch
                {
                    BicepDataType.String => "string",
                    BicepDataType.Bool => "bool",
                    BicepDataType.Integer => "int",
                    BicepDataType.Array => "object[]",

                    _ => throw new InvalidOperationException($"Did not know how to convert from {type} to CLR type."),
                };
            }
        }

        private static void WriteFileHeaderAndUsings(CodeWriter sourceBuilder)
        {
            sourceBuilder.AppendLine("#nullable disable");
            sourceBuilder.AppendLine("using System;");
            sourceBuilder.AppendLine("using System.Collections.Concurrent;");
            sourceBuilder.AppendLine("using System.Linq;");
            sourceBuilder.AppendLine("using Nuke.Common.Tooling;");
            sourceBuilder.AppendLine();
        }

        private static void OpenNamespace(ClassDeclarationSyntax cds, CodeWriter sourceBuilder)
        {
            if (!HasNamespace(cds, out var @namespace))
            {
                return;
            }

            sourceBuilder
                .Append("namespace ").AppendLine(@namespace!)
                .OpenBlock();
        }

        private static void CloseNamespace(ClassDeclarationSyntax cds, CodeWriter sourceBuilder)
        {
            if (!HasNamespace(cds, out _))
            {
                return;
            }

            sourceBuilder.CloseBlock();
        }

        private static bool HasNamespace(ClassDeclarationSyntax cds, out string? @namespace)
        {
            @namespace = GetNamespace(cds);

            return @namespace is not null;
        }

        private static void WriteDeployments(IList<BicepFile> bicepFiles, CodeWriter sourceBuilder)
        {
            sourceBuilder
                    .EmitGeneratedCodeAttribute()
                    .EmitExcludeFromCodeCoverageAttribute()
                    .AppendLine("public static class Deployments")
                    .OpenBlock();

            foreach (var file in bicepFiles.OrderBy(x => x.Name))
            {
                foreach (var module in file.Modules.OrderBy(x => x.Name))
                {
                    WriteBicepModule(module, sourceBuilder);
                }

                if (file.Outputs.Count > 0)
                {
                    WriteBicepDeployment(file.Name, file, sourceBuilder);
                }
            }

            sourceBuilder
                .EmitGeneratedCodeAttribute()
                .EmitExcludeFromCodeCoverageAttribute()
                .AppendLine("private static class AzCli")
                .OpenBlock()
                    .AppendLine("private static Nuke.Common.Tooling.Tool Az => ToolResolver.GetPathTool(\"az\");")
                    .AppendLine()
                    .AppendLine("public static string GetDeploymentOutputValue(string resourceGroup, string deploymentName, string outputName)")
                    .OpenBlock()
                        .AppendLine("var output = Az($\"deployment group show --resource-group {resourceGroup} --name {deploymentName} --query properties.outputs.{outputName}.value --output tsv\");")
                        .AppendLine()
                        .AppendLine("return ReadFirstLine(output);")
                    .CloseBlock()
                    .AppendLine()
                    .AppendLine("private static string ReadFirstLine(System.Collections.Generic.IReadOnlyCollection<Nuke.Common.Tooling.Output> output)")
                    .OpenBlock()
                        .AppendLine("return output.EnsureOnlyStd().First().Text;")
                    .CloseBlock()
                .CloseBlock();

            sourceBuilder
                .CloseBlock()
                .AppendLine();
        }

        private static void WriteBicepModule(BicepModule module, CodeWriter sourceBuilder)
        {
            if (module.Outputs.Count > 0)
            {
                WriteBicepDeployment(module.Name, module, sourceBuilder);
            }

            foreach (var nestedModule in module.Modules)
            {
                WriteBicepModule(nestedModule, sourceBuilder);
            }
        }

        private static void WriteBicepDeployment(string name, IHasBicepOutputs item, CodeWriter sourceBuilder)
        {
            var className = name.Replace('-', '_').Pascalize();

            sourceBuilder
                .EmitGeneratedCodeAttribute()
                .EmitExcludeFromCodeCoverageAttribute()
                .Append("public class ").AppendLine(className)
                .OpenBlock(); // nested class

            OutputClassCacheHelper(sourceBuilder);

            sourceBuilder
                .AppendLine("private readonly string _resourceGroup;")
                .AppendLine("private readonly bool _useCache;")
                .AppendLine()
                .AppendLine($"public {className}(string resourceGroup) : this(resourceGroup, false) {{ }}")
                .AppendLine($"public {className}(string resourceGroup, bool noCache)")
                .OpenBlock()
                    .AppendLine("_resourceGroup = resourceGroup ?? throw new System.ArgumentNullException(nameof(resourceGroup));")
                    .AppendLine("_useCache = !noCache;")
                .CloseBlock();

            foreach (var outputName in item.Outputs.OrderBy(x => x.Name))
            {
                sourceBuilder.AppendLine();
                ConvertOutputToProperty(outputName.Name, name, sourceBuilder);
            }

            sourceBuilder.CloseBlock(); // end nested class
            sourceBuilder.AppendLine();
        }

        private static void OpenContainingClassBlocks(ClassDeclarationSyntax classDeclaration, CodeWriter sourceBuilder)
        {
            // Walk up any nested class definitions and output classes in correct order.
            var classes = new Stack<ClassDeclarationSyntax>();

            SyntaxNode? node = classDeclaration;
            while (node is ClassDeclarationSyntax c)
            {
                classes.Push(c);

                node = node.Parent;
            }

            while (classes.Count > 0)
            {
                var @class = classes.Pop();

                sourceBuilder
                    .EmitGeneratedCodeAttribute()
                    .EmitExcludeFromCodeCoverageAttribute();

                foreach (var modifier in @class.Modifiers)
                {
                    sourceBuilder
                        .Append(modifier.ToString())
                        .Append(' ');
                }

                sourceBuilder
                    .Append("class").Append(' ').AppendLine(@class.Identifier.ToString())
                    .OpenBlock();
            }
        }

        private static void CloseContainingClassBlocks(ClassDeclarationSyntax classDeclaration, CodeWriter sourceBuilder)
        {
            SyntaxNode? node = classDeclaration;
            while (node is ClassDeclarationSyntax)
            {
                sourceBuilder.CloseBlock();

                node = node.Parent;
            }
        }

        private static string? GetNamespace(SyntaxNode node)
        {
            SyntaxNode? namespaceNode = node;
            while (!namespaceNode.IsKind(SyntaxKind.NamespaceDeclaration)
                   && !namespaceNode.IsKind(SyntaxKind.FileScopedNamespaceDeclaration))
            {
                namespaceNode = namespaceNode?.Parent;
            }

            return (namespaceNode as BaseNamespaceDeclarationSyntax)?.Name.ToString();
        }

        private static void ConvertOutputToProperty(string outputName, string deploymentName, CodeWriter writer)
        {
            writer
                .AppendLine($"public string {outputName.Pascalize()}")
                .OpenBlock()
                    .AppendLine("get")
                    .OpenBlock()
                        .AppendLine("string value;")
                        .AppendLine($"if (!_useCache || !TryGetFromCache(_resourceGroup, \"{outputName}\", out value))")
                        .OpenBlock()
                            .AppendLine($"value = AzCli.GetDeploymentOutputValue(_resourceGroup, \"{deploymentName}\", \"{outputName}\");")
                            .AppendLine()
                            .AppendLine("if (_useCache)")
                            .OpenBlock()
                                .AppendLine($"AddToCache(_resourceGroup, \"{outputName}\", value);")
                            .CloseBlock()
                        .CloseBlock()
                    .AppendLine()
                    .AppendLine("return value;")
                    .CloseBlock()
                .CloseBlock();
        }

        private static void OutputClassCacheHelper(CodeWriter writer)
        {
            writer
                .AppendLine("private static readonly ConcurrentDictionary<(string resourceGroup, string outputName), string> _outputsCache = new ConcurrentDictionary<(string resourceGroup, string outputName), string>();")
                .AppendLine()
                .AppendLine("private static bool TryGetFromCache(string resourceGroup, string outputName, out string outputValue)")
                .OpenBlock()
                    .AppendLine("return _outputsCache.TryGetValue((resourceGroup, outputName), out outputValue);")
                .CloseBlock()
                .AppendLine()
                .AppendLine("private static void AddToCache(string resourceGroup, string outputName, string outputValue)")
                .OpenBlock()
                    .AppendLine("_outputsCache.TryAdd((resourceGroup, outputName), outputValue);")
                .CloseBlock()
                .AppendLine();
        }
    }
}

internal static class EnumerableExtensions
{
    public static IEnumerable<AttributeSyntax> SelectAttribute(this IEnumerable<AttributeListSyntax> attributeLists, string attributeName, SemanticModel model)
    {
        foreach (var list in attributeLists)
        {
            foreach (var attribute in list.Attributes)
            {
                var type = model.GetSymbolInfo(attribute).Symbol?.ContainingType;
                if (type is null)
                {
                    continue;
                }

                var attributeFullName = type.ToDisplayString();

                if (attributeFullName == attributeName)
                {
                    yield return attribute;
                }
            }
        }
    }
}
