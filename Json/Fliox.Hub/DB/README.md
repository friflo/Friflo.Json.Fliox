

# ![logo](../../../docs/images/Json-Fliox.svg)     **Fliox Hub DB**      ![SPLASH](../../../docs/images/paint-splatter.svg)

[![.NET Tests](https://github.com/friflo/Friflo.Json.Fliox/workflows/.NET/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions)


## Hub support databases
namespace **`Friflo.Json.Fliox.Hub.DB`**

This namespace provide a set of administrative databases when using a `FlioxHub` as a server - a `HttpHost`.  
Using these database in a `HttpHost` is optional.

- **`cluster`** - [ClusterStore](../DB/Cluster/ClusterStore.cs) -
    Expose information about hosted databases their containers, commands and schema.  

- **`monitor`** - [MonitorStore](../DB/Monitor/MonitorStore.cs) -
    Expose server **Monitoring** to get statistics about requests and tasks executed by users and clients.  

- **`user_db`** - [UserStore](../DB/UserAuth/UserStore.cs) -
    Access and change user **permissions** and **roles** required for authorization.  







