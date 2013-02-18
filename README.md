Apache Bench Visualizer [ab-viz]
================================

**GUI frontend for [Apache Bench](http://httpd.apache.org/docs/2.2/programs/ab.html). [[Apache Bench Wiki](http://en.wikipedia.org/wiki/ApacheBench)].**

[ab-viz]() lets you quickly benchmark and view detailed graphs.

### Prerequiste
[Microsoft .NET Framework 4.5](http://www.microsoft.com/en-in/download/details.aspx?id=30653).

### Download 
To download executable [click here](https://github.com/gaurangsinha/ab-viz/raw/master/binaries/ab-viz.exe).

### Application Startup
The benchmark options can be seen to the left and the summary is updated to the right. The results can be filtered using the filters provided under the summary tabs.
![alt text](https://github.com/gaurangsinha/ab-viz/raw/master/screenshots/startup.png "Application Startup")

### Benchmark In Progress
Here we can see the benchmark in progress.
* [ab-viz]() lets you execute the benchmark multiple times so that the results aren't skewed.
* [ab-viz]() shows you a progress bar with the number of requests that have been completed.

![alt text](https://github.com/gaurangsinha/ab-viz/raw/master/screenshots/in_progress.png "Benchmark In-Progress")

### Benchmark Summary
The summary tab shows the output of [Apache Bench](http://httpd.apache.org/docs/2.2/programs/ab.html).
![alt text](https://github.com/gaurangsinha/ab-viz/raw/master/screenshots/summary.png "Benchmark Summary")

### Benchmark Response
Here we see the plot of the **'-e'** command of [Apache Bench](http://httpd.apache.org/docs/2.2/programs/ab.html).

_".. for each percentage (from 1% to 100%) the time (in milliseconds) it took to serve that percentage of the requests ..."_
![alt text](https://github.com/gaurangsinha/ab-viz/raw/master/screenshots/percentage.png "Benchmark Percentage")

### Benchmark Request Details
Here we see the plot of the **'-g'** command of [Apache Bench](http://httpd.apache.org/docs/2.2/programs/ab.html).
_".. all measured values out as a .. TSV (Tab separate values) file ..."_
* ctime: Connection Time
* dtime: Processing Time
* ttime: Total Time
* wait: Waiting Time

![alt text](https://github.com/gaurangsinha/ab-viz/raw/master/screenshots/requests.png "Benchmark Request Details")

## License
```
   Copyright 2013 Gaurang Sinha

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
```
