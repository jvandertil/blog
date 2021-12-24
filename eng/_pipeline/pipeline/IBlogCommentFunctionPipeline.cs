using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

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

                var projectFiles = FunctionTestsDirectory.GlobFiles("**/*.csproj");
                foreach (var project in projectFiles)
                {
                    DotNetTest(s => s
                        .EnableNoBuild()
                        .SetConfiguration(Configuration)
                        .SetProjectFile(project)
                        .AddLoggers("trx")
                        .SetDataCollector("XPlat Code Coverage")
                        .SetResultsDirectory(ArtifactsDirectory / "TestResults")
                    );
                }

                var artifactsPath = ArtifactsDirectory / "work";

                DotNetPublish(s => s
                    .EnableNoBuild()
                    .SetConfiguration(Configuration)
                    .SetProject(FunctionSourceDirectory / "BlogComments" / "BlogComments.csproj")
                    .SetOutput(artifactsPath));

                CompressionTasks.CompressZip(artifactsPath, ArtifactsDirectory / "blog-comments-function.zip");
                DeleteDirectory(artifactsPath);
            });
    }

    public interface IBlogUploaderPipeline : IProvideSourceDirectory, IProvideArtifactsDirectory, IProvideConfiguration
    {
        private AbsolutePath SolutionRoot => SourceDirectory / "Uploader";

        private AbsolutePath UploaderSourceDirectory => SolutionRoot / "src";
        private AbsolutePath UploaderTestsDirectory => SolutionRoot / "tests";
        private AbsolutePath Solution => SolutionRoot / "Uploader.sln";

        Target Build => _ => _
            .Executes(() =>
            {
                DotNetRestore(s => s.SetProjectFile(Solution));

                DotNetBuild(s => s
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .EnableNoRestore());

                var projectFiles = UploaderTestsDirectory.GlobFiles("**/*.csproj");
                foreach (var project in projectFiles)
                {
                    DotNetTest(s => s
                        .EnableNoBuild()
                        .SetConfiguration(Configuration)
                        .SetProjectFile(project)
                        .AddLoggers("trx")
                        .SetDataCollector("XPlat Code Coverage")
                        .SetResultsDirectory(ArtifactsDirectory / "TestResults")
                    );
                }

                var artifactsPath = ArtifactsDirectory / "work";

                DotNetPublish(s => s
                    .EnableNoBuild()
                    .SetConfiguration(Configuration)
                    .SetProject(UploaderSourceDirectory / "Uploader" / "Uploader.csproj")
                    .SetOutput(artifactsPath));

                CompressionTasks.CompressZip(artifactsPath, ArtifactsDirectory / "uploader.zip");
                DeleteDirectory(artifactsPath);
            });
    }
}
