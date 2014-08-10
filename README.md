StarMQ
======

StarMQ exposes two primary APIs for messaging via **SimpleBus** and **AdvancedBus**. The Simple API provides few configuration options as it assumes default behavior for common use-cases. For fine-grained control, the Advanced API offers methods that correlate to RabbitMQ’s 0-9-1 implementation.

The internal messaging architecture supports the addition of pre- and post-processing steps and provides the message handler the option to control the response to the broker. Example pre-processing steps include message encryption, compression, and authentication. These may be toggled via configuration and registered at startup. At present, the only supported post-processing action is unsubscribing the current consumer.

The post-processing action and the response to the broker are set within the Response object. This object is returned by the message handler when using the Advanced API. The Simple API takes care of the response by sending an **ack** if the message handler successfully completes and a **nack** if an exception bubbles out of the handler.

In terms of **performance**, StarMQ is averages 20k messages per second when publishing.

Highlights:

StarMQ uses SimpleInjector for **dependency injection** and is configured for overriding registrations. This allows you to easily replace any component by registering a custom implementation after calling Registration.RegisterServices(). For example, log4net could be replaced with another logger that implements the generic ILog interface found in log4net.

StarMQ comes wired for **logging** with log4net and will play nice with applications configured for log4net. **Warning**: setting the log level below WARN reduces throughput by over 50%.

StarMQ supports **dead-lettering** using default settings and auto-generated exchange names.

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
- implement pre-processing steps


- redelivered: failover?


2014-08-16 13:23:16,192 [AMQP Connection amqp-0-9://Lx0711:5672] DEBUG StarMQ.Core.PersistentConnection [(null)] - OnDisconnected event fired.
2014-08-16 13:23:16,193 [AMQP Connection amqp-0-9://Lx0711:5672] INFO  StarMQ.Core.PersistentConnection [(null)] - Server terminated connection. Reconnecting...
2014-08-16 13:23:16,193 [AMQP Connection amqp-0-9://Lx0711:5672] INFO  StarMQ.Core.PersistentConnection [(null)] - Attempting to connect to server.
2014-08-16 13:23:16,193 [AMQP Connection amqp-0-9://Lx0711:5672] INFO  StarMQ.Core.PersistentConnection [(null)] - Server connection created to Lx0711:5672:/
