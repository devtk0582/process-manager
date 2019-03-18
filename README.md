# process-manager

Process Manager is light-weight system built to manage processes using .NET Core and achieve self-healing cluster. It includes two parts: program manager and process manager. Process manager will have one master process and multiple child processes. Master process is responsible to collect health check info from child processes and restart them if necessary. Child processes will report health check info to master process regularly. Program manager will check master process regularly to make sure that it is running properly.


## Features

* Kestrel web server as web host with configurable urls and ports for health check
* Dynamically start / stop processes based on health check status
* Can be easily expanded to be distributed system with customized health check api endpoints
* Health check dashboard with all thread-safe logs
