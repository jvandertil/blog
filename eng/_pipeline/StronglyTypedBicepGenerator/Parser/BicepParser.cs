using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Vandertil.Blog.Pipeline.StronglyTypedBicepGenerator.Parser
{
    internal static class BicepParser
    {
        public static BicepFile ParseFile(string filePath)
        {
            using var reader = File.OpenText(filePath);
            return ParseFile(filePath, reader);
        }

        public static BicepFile ParseFile(string filePath, TextReader reader)
        {
            var file = new BicepFile(Path.GetFileNameWithoutExtension(filePath));

            string contents = reader.ReadToEnd();

            file.Parameters.AddRange(ParseParameters(contents));
            file.Outputs.AddRange(ParseOutputs(contents));
            file.Modules.AddRange(GetModulesRecursive(contents, filePath));

            return file;
        }

        private static IEnumerable<BicepModule> GetModulesRecursive(string contents, string parentPath)
        {
            var matches = Regex.Matches(contents, "module \\w+ '(.+)' ?= ?{\\s+[\\S\\s]*?name: ?'(.+)'");

            if (matches.Count > 0)
            {
                for (int i = 0; i < matches.Count; ++i)
                {
                    var match = matches[i];
                    var relativePath = match.Groups[1].Value;
                    var name = match.Groups[2].Value;

                    var module = new BicepModule(name, relativePath);

                    var modulePath = Path.Combine(Path.GetDirectoryName(parentPath), module.RelativePath);
                    using var reader = File.OpenText(modulePath);
                    var moduleContents = reader.ReadToEnd();

                    module.Outputs.AddRange(ParseOutputs(moduleContents));
                    module.Modules.AddRange(GetModulesRecursive(moduleContents, modulePath));

                    yield return module;
                }
            }
        }

        private static IEnumerable<BicepOutput> ParseOutputs(string contents)
        {
            var matches = Regex.Matches(contents, "^output (\\w+) (\\w+).*$", RegexOptions.Multiline);

            if (matches.Count > 0)
            {
                for (int i = 0; i < matches.Count; ++i)
                {
                    var match = matches[i];
                    var name = match.Groups[1].Value;
                    var type = match.Groups[2].Value;

                    yield return new BicepOutput(name, ParseDataType(type));
                }
            }
        }

        private static IEnumerable<BicepParameter> ParseParameters(string contents)
        {
            var matches = Regex.Matches(contents, "^param (\\w+) (\\w+).*$", RegexOptions.Multiline);

            if (matches.Count > 0)
            {
                for (int i = 0; i < matches.Count; ++i)
                {
                    var match = matches[i];
                    var name = match.Groups[1].Value;
                    var type = match.Groups[2].Value;

                    yield return new BicepParameter(name, ParseDataType(type));
                }
            }
        }

        private static BicepDataType ParseDataType(string type)
        {
            switch (type.ToUpperInvariant())
            {
                case "BOOL":
                    return BicepDataType.Bool;
                case "STRING":
                    return BicepDataType.String;
                case "INT":
                    return BicepDataType.Integer;
                default:
                    throw new InvalidOperationException($"Unsupported Bicep type: {type}.");
            }
        }
    }
}
