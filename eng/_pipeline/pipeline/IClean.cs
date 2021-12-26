using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;

namespace Vandertil.Blog.Pipeline
{
    public interface IClean : IProvideArtifactsDirectory, IProvideSourceDirectory
    {
        Target Clean => _ => _
            .Executes(() =>
            {
                SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);

                EnsureCleanDirectory(ArtifactsDirectory);
            });
    }
}
