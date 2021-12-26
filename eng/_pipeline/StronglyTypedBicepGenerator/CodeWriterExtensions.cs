using System.Reflection;

namespace Vandertil.Blog.Pipeline.StronglyTypedBicepGenerator
{
    internal static class CodeWriterExtensions
    {
        public static CodeWriter OpenBlock(this CodeWriter writer)
        {
            return writer.AppendLine("{").PushIndent();
        }

        public static CodeWriter CloseBlock(this CodeWriter writer)
        {
            return writer.PopIndent().AppendLine("}");
        }

        public static CodeWriter EmitGeneratedCodeAttribute(this CodeWriter writer)
        {
            return writer
                .AppendLine($"[System.CodeDom.Compiler.GeneratedCode(\"{nameof(StronglyTypedBicepGenerator)}\", \"{Assembly.GetExecutingAssembly().GetName().Version}\")]");
        }

        public static CodeWriter EmitExcludeFromCodeCoverageAttribute(this CodeWriter writer)
        {
            return writer
                .AppendLine("[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
        }
    }
}
