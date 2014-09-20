StarMQ
======

StarMQ exposes two primary APIs for messaging via **SimpleBus** and **AdvancedBus**. The Simple API makes assumptions about and exposes methods for common use cases. Despite these assumptions, it maintains a high level of flexibility by offering fluent configuration inputs. For fine-grained control, the Advanced API offers methods that correlate to RabbitMQ’s 0-9-1 implementation.

## Highlights
- The internal messaging architecture supports the addition of pre- and post-processing steps. Example pre-processing steps include message encryption, compression, and authentication. These may be enabled via configuration at startup. At present, the only supported post-processing action is unsubscribing the current consumer.
- Consumers may control the client response to the broker by returning the appropriate Response object from the message handler. The Simple API exposes a basic method that takes care of the response by sending an _ack_ if the message handler successfully completes and a _nack_ if an exception bubbles out of the handler.
- StarMQ supports **dead-lettering** and **alternate exchanges** using default settings and auto-generated exchange names.
- StarMQ comes wired for **logging** with log4net. _Warning_: setting the log level below WARN reduces throughput by over 50%.
- StarMQ uses SimpleInjector for **dependency injection** and is configured for overriding registrations. This allows easy replacement of any component by using OverrideRegistration to register the custom implementation. For example, log4net could be replaced with another logger that implements the generic ILog interface found in log4net.

## Performance
- StarMQ's asynchronous internal architecture allows it to sustain a publishing throughput of ~20,000 messages per second with no pre-processing steps.
- StarMQ automatically attempts to recover lost connections to the broker.
- Publishes during a connection failure are non-blocking and buffered in memory until the connection is restored. _Warning_: high-throughput scenarios may cause memory issues during extended outages.
- Message processing is independent among queues; a fast handler will finish processing all messages even if its messages are interleaved with messages for a slow handler.

## Publisher Confirms
- StarMQ offers guaranteed publishing via RabbitMQ’s publisher confirms.
- RabbitMQ declines a message by sending a basic.nack, which StarMQ passes along by throwing a PublishException. Declines are typically caused by internal broker errors.
- StarMQ waits a configurable timeout interval for a broker response. If the interval elapses, the message is re-published.

## Quick-Start
The Factory class allows fluent configuration and access to the SimpleBus API.
```
var simpleBus = new Factory()
    .OverrideRegistration<ISerializationStrategy, MySerializationStrategy>()
    .EnableCompression()
    .EnableEncryption("AdAstraAndBeyond")
    .AddInterceptor(new MyCustomInterceptor())
    .GetBus(connectionString);
```
The example shows how to register custom implementations, enable built-in pre-processing steps, and insert custom steps (a.k.a. interceptors).

With a SimpleBus, publishing and subscribing is as simple as:
```
simpleBus.PublishAsync("hello world", "my.routing.key").Wait(); // Wait() forces a synchronous call
_simpleBus.SubscribeAsync<string>("id", new List<string> { "my.*" }, x => MyMessageHandler(x));
```
