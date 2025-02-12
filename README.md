# Aspire4RabbitMQDelayedMessageExchange    
An easy way to leverage RabbitMQ's Delayed Message Exchange Plugin in Aspire hosted applications using just one line of code. This is great for any event/message-driven application architectures.
It allows you to delay delivery of messages interacting directly with RabbitMQ or indirectly via NServiceBus, MassTransit or the .NET messaging library of your choice.

Don't need the source code? Just get the Nuget package: https://www.nuget.org/packages/Aspire4RabbitMQDelayedMessageExchange/
Read more about delayed delivery in RabbitMQ here: https://www.rabbitmq.com/blog/2015/04/16/scheduling-messages-with-rabbitmq

## Problem statement
.NET Aspire supports the official Management Plugin for RabbitMQ, adding a web GUI for inspecting queues and exchanges of the popular message broker.
However, it currently doesn't currently (as of early 2025) support adding the Delayed Message Exchange Plugin.

My little library Aspire4RabbitMQDelayedMessageExchange solves the problem by writing a docker file to a temporary location inside your AppHost for you, and running it in Docker as part of your distributed application.

## Quickstart
1. Install Aspire4RabbitMQDelayedMessageExchange in your AppHost project via the Nuget package. Usually there's no need to import any namespaces as you'll already be referencing `Aspire.Hosting`.
2. In your AppHost project's Program.cs file, after your call to `AddRabbitMQ` chain a call to `WithDelayedDeliveryPlugin`.
3. There is no 3. You are done! You can now use delayed message exchanges as directed by your messaging library (e.g. MassTransit, NServiceBus etc.)

### Example Program.cs in AppHost
```
var builder = DistributedApplication.CreateBuilder(args);

var rabbitMQ = builder.AddRabbitMQ(AspireResourceNames.MessageBroker.ConnectionName)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin()
    .WithDelayedMessageExchangePlugin();
```

## Custom behaviours (optional)
You can pass a `tag` argument to the `WithDelayedMessageExchangePlugin` method if you like. This will force a particular version of the plugin to be installed (e.g. "3.0" or "4.0") rather than the default, which is "latest".
## Troubleshooting
If I find and overcome any hurdles I'll mention them here, but it's early days. So far so good.
## Contributing
I'm a hobbyist. I know there are loads of people out there who be able to improve this in ways I can't, or see opportunities for improvement that I can't even imagine. If you want to contribute, bring it on! Send me a pull request.
