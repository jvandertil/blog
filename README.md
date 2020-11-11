[![Build Status](https://dev.azure.com/jvandertil/OpenSource/_apis/build/status/Deploy/Blog.Content.Deploy?branchName=master)](https://dev.azure.com/jvandertil/OpenSource/_build/latest?definitionId=21&branchName=master)

# jvandertil.nl

This repository contains the source code and content for my blog located at [www.jvandertil.nl](https://www.jvandertil.nl).

Feel free to open an issue or a pull request if you notice something missing, wrong, misspelled, or any other reason.

## Contents
This repository contains the following parts:

* eng/
  * Contains scripts for deploying and building the blog components
* infra/
  * Contains the Pulumi stack that can provision the cloud infrastructure needed to run the blog in Azure.
* src/blog
  * Contains the hugo source and content of my blog posts.
* src/blog-comment-function
  * Contains the Azure Functions that power the comments on my blog posts.
* src/Uploader
  * Contains the program that uploads the blog content to Azure and purges the CloudFlare cache to immediately make it visible across the globe.
