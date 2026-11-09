using System;
using System.IO;
using System.IO.Compression;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tools.DotNet;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

namespace Vandertil.Blog.Pipeline
{
    public interface IBlogCommentFunctionPipeline : IProvideSourceDirectory, IProvideArtifactsDirectory, IProvideConfiguration
    {
        private AbsolutePath SolutionRoot => SourceDirectory / "blog-comment-function";

        private AbsolutePath FunctionSourceDirectory => SolutionRoot / "src";
        private AbsolutePath FunctionTestsDirectory => SolutionRoot / "tests";
        private AbsolutePath Solution => SolutionRoot / "BlogComments.sln";

        Target Build => _ => _
            .Executes(() =>
            {
                DotNetRestore(s => s.SetProjectFile(Solution));

                DotNetBuild(s => s
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .EnableNoRestore());

                // NUKE build does not support MTP v2 yet.
                //var projectFiles = FunctionTestsDirectory.GlobFiles("**/*.csproj");
                //foreach (var project in projectFiles)
                //{
                //    DotNetTest(s => s
                //        .EnableNoBuild()
                //        .SetConfiguration(Configuration)
                //        .SetProjectFile(project)
                //        .AddLoggers("trx")
                //        .SetDataCollector("XPlat Code Coverage")
                //        .SetResultsDirectory(ArtifactsDirectory / "TestResults")
                //    );
                //}

                var artifactsPath = ArtifactsDirectory / "work";

                DotNetPublish(s => s
                    .SetConfiguration(Configuration)
                    .SetProject(FunctionSourceDirectory / "BlogComments" / "BlogComments.csproj")
                    .SetOutput(artifactsPath)
                    .EnableSelfContained()
                    .SetRuntime("linux-x64"));

                var hostJsonPath = artifactsPath / "host.json";
                if (OperatingSystem.IsLinux())
                {
                    // Flex Consumption reads files directly from the deployed package. Keep
                    // host.json readable by the Functions host when ZIP metadata is applied.
                    File.SetUnixFileMode(
                        hostJsonPath,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
                }

                ZipFile.CreateFromDirectory(
                    artifactsPath,
                    ArtifactsDirectory / "blog-comments-function.zip",
                    CompressionLevel.Optimal,
                    includeBaseDirectory: false);
                artifactsPath.DeleteDirectory();
            });
    }
}
