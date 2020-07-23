+++
author = "Jos van der Til"
title = "SSH cmdlets missing from posh-git"
date  = 2019-03-16T15:00:00+01:00
type = "post"
tags = [ "Git", "Windows", "Powershell" ]
+++

After repaving my machine and installing the latest version of [posh-git](https://github.com/dahlbyk/posh-git) I noticed that my Powershell profile was no longer working properly.
I was using the `Start-SshAgent` cmdlet to load my SSH keys and well, it was no longer recognized.

When I checked the GitHub repository, it was not immediately clear that these have been moved to a separate project: [posh-sshell](https://github.com/dahlbyk/posh-sshell).
Follow the instructions (or clone the repository), and include this new Powershell module as well in your profile!
