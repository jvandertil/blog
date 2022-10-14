+++
author = "Jos van der Til"
title = "Fixing 'Could not inject value for GitVersion' in Azure Pipelines"
date  = 2022-09-09T21:00:00+02:00
type = "post"
tags = [ "GitVersion", "Azure Pipelines", "NUKE" ]
+++

If you are using [GitVersion](https://gitversion.net/) with [NUKE](https://nuke.build/) and you are trying to get the pipeline working, but are running into the build failing with `Could not inject value for GitVersion`.

I will assume you are using a YAML pipeline (and in my case I was pulling the sources from GitHub), try this.

First open the pipeline that is experiencing issues and go select 'Edit':
{{< figure src="02-edit-pipeline.png" alt="Screenshot of Azure Pipelines overview.">}}

Select the additional settings that are collapsed and go to 'Triggers':
{{< figure src="03-edit-triggers.png" alt="Expanded additional settings with Triggers highlighted">}}

Once there, select YAML and the repository and make sure that 'Shallow fetch' is disabled.
{{< figure src="04-uncheck-shallow-fetch.png" alt="Screenshot showing Shallow Fetch unchecked">}}

This solved the issue for me and GitVersion started working as expected.
