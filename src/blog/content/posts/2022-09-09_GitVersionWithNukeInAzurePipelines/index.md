+++
author = "Jos van der Til"
title = "Fixing 'Could not inject value for Gitversion' in Azure Pipelines"
date  = 2022-09-09T21:00:00+02:00
type = "post"
tags = [ "GitVersion", "Azure Pipelines", "NUKE" ]
+++

If you are using [GitVersion](https://gitversion.net/) with [NUKE](https://nuke.build/) and you are trying to get the pipeline working, but are running into the build failing with `Could not inject value for GitVersion`.

I will assume you are using a YAML pipeline (and in my case I was pulling the sources from GitHub), try this.
