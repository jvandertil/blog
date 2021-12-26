using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using static Nuke.Common.IO.FileSystemTasks;

namespace Vandertil.Blog.Pipeline
{
    public interface IBlogContentPipeline : IProvideArtifactsDirectory, IProvideSourceDirectory
    {
        private AbsolutePath ContentSourceDirectory => SourceDirectory / "blog";

        private AbsolutePath HugoToolFolder => RootDirectory / ".bin" / "hugo";
        private Tool Hugo => ToolResolver.GetLocalTool(HugoToolFolder / (EnvironmentInfo.IsWin ? "hugo.exe" : "hugo"));

        Target Build => _ => _
            .Executes(async () =>
            {
                await RestoreHugoBinary();

                AbsolutePath artifactPath = ArtifactsDirectory / "blog";

                Hugo($"--source {ContentSourceDirectory} --destination {artifactPath} --minify");

                CompressionTasks.CompressZip(artifactPath, ArtifactsDirectory / "blog.zip");
                DeleteDirectory(artifactPath);
            });

        private async Task RestoreHugoBinary()
        {
            const string HugoVersion = "0.83.1";
            string HugoFileName = EnvironmentInfo.IsWin ? $"hugo_extended_{HugoVersion}_Windows-64bit.zip" : $"hugo_extended_{HugoVersion}_Linux-64bit.tar.gz";
            string HugoReleaseUrl = $"https://github.com/gohugoio/hugo/releases/download/v{HugoVersion}/{HugoFileName}";
            AbsolutePath destinationFile = HugoToolFolder / HugoFileName;

            if (!FileExists(destinationFile))
            {
                await HttpTasks.HttpDownloadFileAsync(HugoReleaseUrl, destinationFile);
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
                Logger.Info("Skipping Hugo restore, already restored.");
            }
        }
    }
}
