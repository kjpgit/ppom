## Overview

Source code for https://www.patternedpom.com (my wife's ecommerce site)


## Tech Stack

* C# / .NET Core 2.2 (On Linux)

* RazorLight for [page templates](source/SiteBuilder/templates/)

* HTML Prettiness: CSS (Less), JQuery, Bootstrap, Photoswipe, xZoom

* Shopping Cart: Vanilla.js, LocalStorage 

* Payment Processing: Paypal

* Content Management: Google Spreadsheet, local directories of images and
  CommonMark markdown files.  Ghostwriter for CommonMark WYSIWYG editing.

* Hosting: AWS CloudFront, S3, Lambda.   It's mostly a static site, for super cheap hosting.

* AWS .NET SDK for [S3 syncing](source/SyncS3/Program.cs)


## History

I rewrote this in 2019 to use C# instead of Python.  I'm very frustrated by
Python these days - it's great for small scripts but terrible for large or
mission critical applications.  And I'm very excited about using C# on Linux,
now that Microsoft is officially supporting it.  It is very pleasant to use.
