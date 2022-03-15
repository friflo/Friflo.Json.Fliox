

# ![logo](../../../docs/images/Json-Fliox.svg)     **Fliox Hub Client**      ![SPLASH](../../../docs/images/paint-splatter.svg)

[![.NET Tests](https://github.com/friflo/Friflo.Json.Fliox/workflows/.NET/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions)


## `FlioxClient`
namespace **`Friflo.Json.Fliox.Hub.Client`**

The intention of `FlioxClient` is extending it by a domain specific class.

``` csharp
    public class AppClient : FlioxClient {
        // --- containers
        public readonly EntitySet <long, Article>     articles;

        public FlioxClient(FlioxHub hub) : base (hub) { }
    }
```

By doing this the `AppClient` offer two main functionalities:
-   Define a **database schema** by declaring its containers, commands and messages
-   Instances of `AppClient` are **database clients** providing
    type-safe access to the database containers, commands and messages  

In detail:
- **containers** - are fields or properties of type `EntitySet<TKey,T>`
- **commands**   - are methods returning a `CommandTask<TResult>`
- **messages**   - are methods returning a `MessageTask`

Instances of `AppClient` can be used on server and client side.

