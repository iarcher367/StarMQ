StarMQ
======

StarMQ exposes two primary APIs for messaging via **SimpleBus** and **AdvancedBus**. The Simple API provides few configuration options as it assumes default behaviour for common use-cases. For fine-grained control, the Advanced API offers methods that correlate to RabbitMQ’s 0-9-1 implementation.

## Highlights
- The internal messaging architecture supports the addition of pre- and post-processing steps. Example pre-processing steps include message encryption, compression, and authentication. These may be toggled via configuration and registered at startup. At present, the only supported post-processing action is unsubscribing the current consumer.
- Consumers may control the client response to the broker by returning the appropriate Response object from the message handler. The Simple API exposes a basic method that takes care of the response by sending an _ack_ if the message handler successfully completes and a _nack_ if an exception bubbles out of the handler.
- StarMQ supports **dead-lettering** and **alternate exchanges** using default settings and auto-generated exchange names.
- StarMQ comes wired for **logging** with log4net. _Warning_: setting the log level below WARN reduces throughput by over 50%.
- StarMQ uses SimpleInjector for **dependency injection** and is configured for overriding registrations. This allows easy replacement of any component by registering a custom implementation after calling Registration.RegisterServices(). For example, log4net could be replaced with another logger that implements the generic ILog interface found in log4net.

## Performance
- StarMQ can publish an average of just over 20,000 messages per second with no pre-processing steps.
- StarMQ automatically attempts to recover lost connections to the broker.
- Publishes during a connection failure are non-blocking and buffered in memory until the connection is restored. _Warning_: high-throughput scenarios may cause memory issues during extended outages.

## Publisher Confirms
- StarMQ offers guaranteed publishing via RabbitMQ’s publisher confirms.
- RabbitMQ declines a message by sending a basic.nack, which StarMQ passes along by throwing a PublishException. Declines are typically due to internal broker errors.
- StarMQ waits a configurable timeout interval for a broker response. If the interval elapses, the message is re-published.

Todo:
- able to trigger unsubscribe from queue
- pipeline design for consumers; insertion points to register strategy for cross-cutting concerns: de-dup, auth
- supports HA, clusters
- ability to send derived types over base-type-bound queue
- ability to set TTL or custom headers (e.g. auth) per message
- implement pre-processing steps
- support TPL inbound dispatcher strategy

For HA clusters, set the connection string to connect to the load balancer. StarMQ will detect the connection loss on failover and automatically recover.

- redelivered: failover?
