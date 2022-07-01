

# ![logo](../../../docs/images/Json-Fliox.svg)     **Fliox Hub Protocol**      ![SPLASH](../../../docs/images/paint-splatter.svg)

[![.NET Tests](https://github.com/friflo/Friflo.Json.Fliox/workflows/.NET/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions)


## Hub Protocol
namespace **`Friflo.Json.Fliox.Hub.Protocol`**

The `Hub Protocol` is a set of model classes used as the communication interface between a `FlioxClient` and a `FlioxHub`.  
The Protocol is designed to be used for **direct** and **remote** calls.

- **direct** - protocol messages are class instances passed between a `FlioxClient` and a `FlioxHub`.  
  This enables performant **in-process** usage of a database adapter.

- **remote** - protocol messages are serialized to JSON so that they can be sent via various
  transport protocols like **HTTP** or **WebSocket**.  
  This enables a **client-server** setup hosting multiple databases at the server and expose them to various clients.

## Usage

In a common **C# .NET** application the protocol is not used directly as the interfaces of `FlioxClient` and `EntitySet<,>` are typically used instead.

As **Fliox Protocol** messages like `SyncRequest` & `SyncResponse` are serialized to **JSON** you can access a server via **HTTP** without an additional SDK.  
You can try out sending `SyncRequest` in the **Playground** tab of the [**Hub Explorer**](../../Fliox.Hub.Explorer/README.md)

*Note:*  
Alternatively access a server via **OpenAPI** or **GraphQL** API.  
These API's are available via the **`OAS`** and **`GQL`** links in the [**Hub Explorer**](../../Fliox.Hub.Explorer/README.md)


## Unit of Work

The fundamental feature of protocol messages is the adaption of the **Unit of Work** pattern.  
Each `SyncRequest` contains a set of tasks like: *Create*, *Read*, *Update*, *Delete*, *SendMessage*, *SendCommand*, ...  
Applying this pattern enables:

- Increase **efficiency** by minimizing the significant remote communication overhead (chattiness)
  by reducing the number of requests or messages send between a client and a server.  
  This is simply achieved by combining a set of tasks into a single request.

- Increase **reliability** in remote communication as a set of tasks in a single request arrives at its target as a whole.  
  In opposite: in a *RESTful* API each database operation is executed in a separate request.  
  So in a scenario where multiple database changes need to be executed - e.g. updates or deletes in multiple tables -
  the remote connection may disconnect.  
  This results in a state where database changes are applied only partly.

- Enables efficient **authentication** as user authentication of a request containing multiple tasks is executed only
  once for all tasks.  
  *Note:* **authorization** of task execution is performed for each task individually.






