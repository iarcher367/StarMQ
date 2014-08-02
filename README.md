StarMQ
======

Features:

StarMQ uses SimpleInjector for **dependency injection** and is configured for overriding registrations. This allows you to easily replace any component by registering a custom implementation after calling Registration.RegisterServices(). For example, log4net could be replaced with another logger that implements the generic ILog interface found in log4net.

StarMQ comes wired for **logging** with log4net and will play nice with applications configured for log4net.

For HA clusters, set the connection string to connect to the load balancer.

<<<< will dispatcher attempt to execute commands while disconnected?

Todo:
- supports Publisher Confirms
- able to control response back to server: nack, reject, 
- able to trigger unsubscribe from queue
- built-in DLX handling
- supports HA, clusters
- pipeline design for consumers; insertion points to register strategy for cross-cutting concerns: de-dup, auth
- ability to send derived types over base-type-bound queue
- ability to set TTL or custom headers (e.g. auth) per message


- redelivered: failover?
