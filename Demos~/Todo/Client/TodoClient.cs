using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable All


/// <summary>
/// The <see cref="TodoClient"/> offer two functionalities: <br/>
/// 1. Defines a database <b>schema</b> by declaring its containers, commands and messages<br/>
/// 2. Is a database <b>client</b> providing type-safe access to its containers, commands and messages <br/>
/// </summary>
[NamingPolicy(NamingPolicyType.CamelCase)]
public class TodoClient : FlioxClient
{
    // --- containers
    public  readonly    EntitySet <long, Job>   Jobs;
    
    // --- commands
    /// <summary>Delete all jobs marked as completed / not completed</summary>
    public CommandTask<int>  ClearCompletedJobs (bool completed) => send.Command<bool,int>(completed);

    public TodoClient(FlioxHub hub, string dbName = null) : base (hub, dbName) { }
}

// ---------------------------------- entity models ----------------------------------
public class Job
{
    [Key]       public  long    id;
    [Required]  public  string  title;
                public  bool?   completed;
}

