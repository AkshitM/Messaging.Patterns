Messaging.Patterns
==================
[![Build status](https://ci.appveyor.com/api/projects/status/gdkvga7qylhs8jue?svg=true)](https://ci.appveyor.com/project/peteraritchie/messaging-patterns)

Messaging.Patterns is a library that contains patterns for implementing message-oriented systems.  The patterns are implemetations from [Messaging.Primitives](https://github.com/peteraritchie/Messaging.Primitives)

##Bus
A Bus is an simple implementation of [IBus](https://github.com/peteraritchie/Messaging.Primitives/blob/master/PRI.Messaging.Primitives/IBus.cs).  This class currently facilitates chaining message handlers or or consumers (implementations of [IConsumer](https://github.com/peteraritchie/Messaging.Primitives/blob/master/PRI.Messaging.Primitives/IConsumer.cs).
This bus provides the ability to automatically find and chain together handlers by providing a directory, wildcard and namespace specifier with the [AddHandlersAndTranslators](https://github.com/peteraritchie/Messaging.Patterns/blob/master/PRI.Messaging.Patterns/Extensions/Bus/BusExtensions.cs#L28) extension method.
A handler is an IConsumer implementation and a translator is an IPipe implementation and IPipes are also consumers.  As pipes are encountered they are connected to consumers of the pipes outgoing type.  So, when the bus is given a message to handle, the message is broadcast to all consumers; much like a [publish-subscribe channel](http://www.enterpriseintegrationpatterns.com/patterns/messaging/PublishSubscribeChannel.html).  If a consumer is a pipe, the pipe processes the message then sends it to another consumer.  If there is only one consumer of the message type to be handled by the bus, it will not broadcast but send to the one and only handler; like a [point-to-point channel](http://www.enterpriseintegrationpatterns.com/patterns/messaging/PointToPointChannel.html). 
