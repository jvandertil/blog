+++
author = "Jos van der Til"
title = "Using built-in SSH with Git on Windows 10"
date  = 2019-01-18T18:00:00+01:00
type = "post"
tags = [ "Git", "Windows", "Powershell" ]
+++

Starting with Windows 10 version 1803 a native version of OpenSSH is bundled and installed by default.
If you are using [posh-git](https://github.com/dahlbyk/posh-git) you'll notice that the `Start-SshAgent` command fails with an error:
```none
unable to start ssh-agent service, error :1058
Error connecting to agent: No such file or directory
```

This is because by default the OpenSSH agent (ssh-agent) service is disabled.
To enable it, open an elevated PowerShell window and run:
```powershell
Set-Service -StartupType Manual ssh-agent
```

Now `Start-SshAgent` will work as it always has, however by default Git (for Windows) will continue to use the bundled OpenSSH package.
This means that you are now running an ssh-agent that Git will not use.

To fix this add the following to the Git config:
```properties
[core]
    sshCommand = C:/WINDOWS/System32/OpenSSH/ssh.exe
```

I generally use this command to edit the global Git config: `git config --global -e`.

{{< notice >}}
If you end up in vim and are unfamilar how to exit, use ESC to go to the command bar, and type ':wq' to save and exit, or ':q!' to exit without saving.
{{< /notice >}}

Now Git will switch to the Windows supplied OpenSSH implementation, and everything works together nicely.

*Update 29-01-2020: Removed quotes from Git config as these are not needed; added git config instructions.*
