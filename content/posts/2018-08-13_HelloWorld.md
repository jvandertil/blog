+++
author = "Jos van der Til"
title = "Hello world!"
date  = 2018-08-13T18:30:00+02:00
type = "post"
+++

This is the first post on my blog! Let's hope I can keep this up.

I'll usually blog about things I've solved as a form of documentation, which I hope will help someone else as well.

## Used technology
Since this is a technology blog, I'll list the technology that powers this blog (this is subject to change, but I'll update any changes I make).

### Hugo
The blog is powered by [Hugo](https://www.gohugo.io), which is a static site generator written in the [Go](https://www.golang.org) programming language.
It is really fast, and building this small blog at the moment takes roughly 100 milliseconds on my laptop.

### GitHub 
The source code for this blog is hosted on [GitHub](https://www.github.com/jvandertil/blog).
Feel free to open an issue if you have found a typo or some other error, or if it is just a small thing which is easily fixed, I'm happy to accept Pull Requests!

### TeamCity
This blog is automatically deployed on each checkin to master using [TeamCity](https://www.jetbrains.com/teamcity).
Automating everything involved should make it easier for me to update things in the blog, as I don't have to worry about uploading or publishing anything.
Each commit will be automatically deployed.

### CloudFlare
I'm using CloudFlare to aggressively cache this site, as it is completely made of static content this is extremely easy to setup.
The automated deploy pipeline will purge the CloudFlare cache so that changes are made visible immediately.

