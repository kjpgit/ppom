## Overview

Source code for https://www.patternedpom.com (my wife's ecommerce site)


## Tech Stack

* C# / .NET Core 2.2 (On Linux)

* RazorLight for [page templates](source/SiteBuilder/templates/)

* HTML Prettiness: CSS (Less), JQuery, Bootstrap, Photoswipe, xZoom

* Shopping Cart: Vanilla.js, LocalStorage 

* Payment Processing: Paypal

* Google Spreadsheets .NET SDK for [fetching product listing data](source/GoogleSpreadsheetData/GoogleSheets.cs)

* Markdig for CommonMark Markdown processing.  Ghostwriter for CommonMark WYSIWYG editing.

* Hosting: AWS CloudFront, S3, Lambda.   It's mostly a static site, for super cheap hosting.

* AWS .NET SDK for [S3 syncing](source/SyncS3/Program.cs)


## History

I rewrote this in 2019 to use C# instead of Python.  I'm very frustrated by
Python these days - it's great for small scripts but terrible when refactoring
large or mission critical applications.  

I'm very excited about using C# on Linux, now that Microsoft is officially
supporting it.  It was very pleasant to use on this project!
