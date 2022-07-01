

# ![logo](../../../docs/images/Json-Fliox.svg)     **Fliox Hub DB**      ![SPLASH](../../../docs/images/paint-splatter.svg)

[![.NET Tests](https://github.com/friflo/Friflo.Json.Fliox/workflows/.NET/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions)


## Hub support databases
namespace **`Friflo.Json.Fliox.Hub.DB`**

This namespace provide a set of administrative databases when using a `FlioxHub` as a server - a `HttpHost`.  
Using these database in a `FlioxHub` is optional.  
If adding these databases to a `FlioxHub` the are available in the [**Hub Explorer**](../../Fliox.Hub.Explorer/README.md)

- **`cluster`** - [ClusterStore](../DB/Cluster/ClusterStore.cs) -
    Expose information about hosted databases their containers, commands and schema.  

- **`monitor`** - [MonitorStore](../DB/Monitor/MonitorStore.cs) -
    Expose server **Monitoring** to get statistics about requests and tasks executed by users and clients.  

- **`user_db`** - [UserStore](../DB/UserAuth/UserStore.cs) -
    Access and change user **permissions** and **roles** required for authorization.  


## Examples
To utilize these databases add them as an extension database to a `FlioxHub`.  
*Prerequisite*
``` csharp
var hub = new FlioxHub(database); // Create a FlioxHub with given default database
```

### **`cluster`**
``` csharp
// expose info of hosted databases. cluster is required by Hub Explorer
hub.AddExtensionDB (new ClusterDB("cluster", hub));
```

### **`monitor`**
``` csharp
// expose monitor stats as extension database
hub.AddExtensionDB (new MonitorDB("monitor", hub));
```

### **`user_db`**
``` csharp
var userDB          = new FileDatabase("user_db", c.userDbPath, new UserDBHandler(), null, false);
hub.Authenticator   = new UserAuthenticator(userDB)
    .SubscribeUserDbChanges(hub.EventDispatcher);   // optional - apply user_db changes instantaneously
hub.AddExtensionDB(userDB);                         // expose user_db as extension database
```




