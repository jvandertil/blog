using System;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;

namespace Vandertil.Blog.Pipeline
{
    public interface IBlogContentPipeline : IProvideArtifactsDirectory, IProvideSourceDirectory
    {
        private const string HugoVersion = "0.134.2";

        private AbsolutePath ContentSourceDirectory => SourceDirectory / "blog";

        private AbsolutePath HugoToolFolder => RootDirectory / ".bin" / "hugo";

        private Tool Hugo => ToolResolver.GetTool(HugoToolFolder / (EnvironmentInfo.IsWin ? "hugo.exe" : "hugo"));

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

                artifactPath.CompressTo(ArtifactsDirectory / "blog.zip");
                artifactPath.DeleteDirectory();
            });

        private async Task RestoreHugoBinary()
        {
            string HugoFileName = EnvironmentInfo.IsWin ? $"hugo_extended_{HugoVersion}_windows-amd64.zip" : $"hugo_extended_{HugoVersion}_linux-amd64.tar.gz";
            string HugoReleaseUrl = $"https://github.com/gohugoio/hugo/releases/download/v{HugoVersion}/{HugoFileName}";
            AbsolutePath destinationFile = HugoToolFolder / HugoFileName;

            if (!destinationFile.Exists())
            {
                Serilog.Log.Information("Restoring Hugo binary.");

                await HttpTasks.HttpDownloadFileAsync(HugoReleaseUrl, destinationFile, clientConfigurator: client => { client.Timeout = TimeSpan.FromSeconds(30); return client; }, headerConfigurator: headers => headers.Add("User-Agent", "jvandertil/blog build script"));
                destinationFile.UncompressTo(destinationFile.Parent);

                if (!EnvironmentInfo.IsWin)
                {
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
