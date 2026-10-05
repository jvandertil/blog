using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities.Collections;

namespace Vandertil.Blog.Pipeline
{
    public interface IClean : IProvideArtifactsDirectory, IProvideSourceDirectory
    {
        Target Clean => _ => _
            .Executes(() =>
            {
                SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(x => x.DeleteDirectory());

                ArtifactsDirectory.CreateOrCleanDirectory();
            });
    }
}
