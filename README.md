StarMQ
======

StarMQ exposes two primary APIs for messaging via **SimpleBus** and **AdvancedBus**. The Simple API makes assumptions about and exposes methods for common use cases. Despite these assumptions, it maintains a high level of flexibility by offering fluent configuration inputs. For fine-grained control, the Advanced API offers methods that correlate to RabbitMQ’s 0-9-1 implementation.

## Highlights
- The internal messaging architecture supports the addition of pre- and post-processing steps. Example pre-processing steps include message encryption, compression, and authentication. These may be enabled via configuration at startup. At present, the only supported post-processing is unsubscribing the current consumer.
- StarMQ supports **dead-lettering** and **alternate exchanges** using default settings and auto-generated exchange names.
- StarMQ comes wired for logging via the ILog interface. See ILog.cs for a sample log4net logger. _Warning_: setting the log level below WARN may drastically reduce throughput.
- StarMQ uses an internal IoC container that supports overriding registrations. This allows easy replacement of any component by using OverrideRegistration to register the custom implementation.

## Performance
- StarMQ's asynchronous internal architecture allows it to sustain a publishing throughput of ~20,000 messages per second with no pre-processing steps.
- StarMQ automatically attempts to recover lost connections to the broker. It will also recover non-exclusive consumers.
- Publishes during a connection failure are non-blocking and buffered in memory until the connection is restored. _Warning_: high-throughput scenarios may cause memory issues during extended outages.
- A call to dispose the bus instance will not complete until all buffered messages are published. _Note_: this is not yet compatible with publisher confirms.
- Message processing is independent among queues; a consumer with a fast handler will finish processing all messages even if its messages are interleaved with messages for a consumer with a slow handler.

## Publisher Confirms
- StarMQ supports guaranteed publishing via RabbitMQ’s publisher confirms.
- RabbitMQ declines a message by sending a basic.nack, which are logged as errors. No further action is taken as declines are typically caused by internal broker errors.
- StarMQ waits a configurable timeout interval for a broker response. If the interval elapses, the message is re-published.

## Consumers
- Consumers may control the client response to the broker by returning the appropriate Response object from the message handler. The Simple API exposes a basic method that takes care of the response by sending an _ack_ if the message handler successfully completes and a _nack_ if an exception bubbles out of the handler.
- A consumer may also be fluently configured with multiple handlers. Only one handler may be registered per .Net type. Upon receiving a message, StarMQ examines it to determine the .Net type and then uses the type to select the appropriate handler. If no type is specified or no matching handler is found, the consumer defaults to the first handler registered.

## High-Availability
For HA clusters, set the connection string to point at the load balancer. StarMQ will detect the connection loss on failover and automatically recover the connection along with any non-exclusive consumers.

## Quick-Start
The Factory class allows fluent configuration and access to the SimpleBus API.
```
var simpleBus = new Factory()
    .OverrideRegistration<ILog>(x => new Log4NetLogger(x.ConcreteType))
    .OverrideRegistration<ISerializationStrategy, MySerializationStrategy>()
    .EnableCompression()
    .EnableEncryption("MySuperSecretKey")
    .AddInterceptor(new MyCustomInterceptor())
    .GetBus(connectionString);
```
The example shows the _optional_ intermediate steps to register custom implementations, enable built-in pre-processing steps, and insert custom steps (a.k.a. interceptors). The line below shows the connection string format and the default values used if the parameter is not supplied.
```
cancelonhafailover=false;heartbeat=10;host=localhost;password=guest;port=5672;publisherconfirms=false;reconnect=5000;timeout=10000;username=guest;virtualhost=/
```
With a SimpleBus, publishing and subscribing may be minimally accomplished using:
```
simpleBus.PublishAsync("hello world",
    context => context.WithRoutingKey("my.routing.key")).Wait(); // Wait() forces a synchronous call, if desired
simpleBus.SubscribeAsync(
    handlers => handlers.Add<string>((data, context) => MyMessageHandler(data)));
```
The above makes use of all the defaults. Alternatively, to customize:
```
simpleBus.PublishAsync("hello world",
    context => context.WithRoutingKey("my.routing.key")
        .WithHeader("myHeader","myValue"),
        true, false,
    exchange => exchange.WithAlternateExchangeName("my alternate exchange")
        .WithAutoDelete(true)
        .WithDurable(false)
        .WithName("primary exchange"));

simpleBus.SubscribeAsync(
    handlers => handlers.Add<string>((data, context) => MyMessageHandler(data, context))
        .Add<MyClass>((data, context) => MySecondHandler(data, context)),
    queue => queue.WithAutoDelete(true)
        .WithBindingKey("*.b")
        .WithBindingKey("a.*")
        .WithCancelOnHaFailover(true)
        .WithDeadLetterExchangeName("DLX")
        .WithDeadLetterRoutingKey("#")
        .WithDurable(false)
        .WithExclusive(true)
        .WithExpires(10)
        .WithMessageTimeToLive(10)
        .WithName("my queue")
        .WithPriority(-3),
    exchange => exchange.WithAlternateExchangeName("my alternate exchange")
        .WithAutoDelete(true)
        .WithDurable(false)
        .WithName("my exchange")
        .WithType(ExchangeType.Fanout));
```
Both publishers and consumers have access to a DeliveryContext object. This allows publishers to set the required routing key and other desired message properties, like a custom header. The delivery context allows consumers to access the message's redelivery status, routing key, and properties within a handler.