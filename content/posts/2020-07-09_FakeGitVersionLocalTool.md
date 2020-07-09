+++
author = "Jos van der Til"
title = "Running GitVersion as a .NET Core local tool in FAKE"
date  = 2020-07-09T18:00:00+02:00
type = "post"
tags = [ ".NET", "FAKE", "FSharp", "GitVersion" ]
+++

Recently I wanted to use GitVersion to determine the version number for a project. 
To keep the project self-contained I installed GitVersion as a [.NET Local Tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools).
However, when trying to get the generated version numbers through FAKE it didn't work.

Unfortunately, the current version of FAKE does not support running GitVersion as a dotnet tool.
To bridge that gap I wrote the following FAKE script to get it to work.

```fsharp
// Dependencies are listed in build.fsx
// nuget Fake.DotNet.Cli
// nuget Fake.Tools.GitVersion
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Newtonsoft.Json

let run () =
    let proc = Command.RawCommand("gitversion", Arguments.Empty)
                 |> CreateProcess.fromCommand
                 |> CreateProcess.withToolType (ToolType.CreateLocalTool())
                 |> CreateProcess.redirectOutput
                 |> Proc.run

    if proc.ExitCode <> 0 then
        failwithf "GitVersion failed with exit code %i and message %s" proc.ExitCode proc.Result.Output

    proc.Result.Output
    |> fun j -> JsonConvert.DeserializeObject<Fake.Tools.GitVersion.GitVersionProperties>(j)
```

I put this script next to the `build.fsx` script I already had, and named it `gitVersion.fsx` (note the capitalization).
Then I could call it using `GitVersion.run()` like so:

```fsharp
let getVersion() = 
    let gitVersionInfo = GitVersion.run()

    { VersionPrefix = gitVersionInfo.MajorMinorPatch
      VersionSuffix = gitVersionInfo.PreReleaseTag }
```

I hope this helps someone trying to do the same.
