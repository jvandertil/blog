using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using static Nuke.Common.IO.FileSystemTasks;

namespace Vandertil.Blog.Pipeline
{
    public interface IBlogContentPipeline : IProvideArtifactsDirectory, IProvideSourceDirectory
    {
        private const string HugoVersion = "0.100.1";

        private AbsolutePath ContentSourceDirectory => SourceDirectory / "blog";

        private AbsolutePath HugoToolFolder => RootDirectory / ".bin" / "hugo";
        private Tool Hugo => ToolResolver.GetLocalTool(HugoToolFolder / (EnvironmentInfo.IsWin ? "hugo.exe" : "hugo"));

        Target Serve => _ => _
            .Executes(async () =>
            {
                await RestoreHugoBinary();

                Hugo($"serve --environment dev --source {ContentSourceDirectory} --minify -DEF");
            });

        Target Build => _ => _
            .Executes(async () =>
            {
                await RestoreHugoBinary();

                AbsolutePath artifactPath = ArtifactsDirectory / "blog";

                foreach (var environment in Environment.All())
                {
                    Hugo($"--environment {environment} --source {ContentSourceDirectory} --destination {artifactPath / environment} --minify");
                }

                CompressionTasks.CompressZip(artifactPath, ArtifactsDirectory / "blog.zip");
                DeleteDirectory(artifactPath);
            });

        private async Task RestoreHugoBinary()
        {
            string HugoFileName = EnvironmentInfo.IsWin ? $"hugo_extended_{HugoVersion}_Windows-64bit.zip" : $"hugo_extended_{HugoVersion}_Linux-64bit.tar.gz";
            string HugoReleaseUrl = $"https://github.com/gohugoio/hugo/releases/download/v{HugoVersion}/{HugoFileName}";
            AbsolutePath destinationFile = HugoToolFolder / HugoFileName;

            if (!destinationFile.Exists())
            {
                await HttpTasks.HttpDownloadFileAsync(HugoReleaseUrl, destinationFile, headerConfigurator: headers => headers.Add("User-Agent", "jvandertil/blog build script"));

                if (EnvironmentInfo.IsWin)
                {
                    CompressionTasks.UncompressZip(destinationFile, destinationFile.Parent);
                }
                else
                {
                    CompressionTasks.UncompressTarGZip(destinationFile, destinationFile.Parent);
                    var chmod = ToolResolver.GetPathTool("chmod");
                    chmod($"+x {destinationFile.Parent / "hugo"}");
                }
            }
            else
            {
                Serilog.Log.Information("Skipping Hugo restore, already restored.");
            }
        }
    }
}
