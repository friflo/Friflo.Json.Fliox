using System;
using System.Linq;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;

namespace Todo
{
    internal  static class  Program
    {
        public static void Main(string[] args) {
            var hub         = CreateHub(args);
            var client      = new TodoClient(hub);
            var todos       = client.tasks.QueryAll();
            client.SyncTasks().Wait();
            
            foreach (var todo in todos.Result) {
                Console.WriteLine($"id: {todo.id}, title: {todo.title}, completed: {todo.completed}");
            }
        }
        
        private static FlioxHub CreateHub(string[] args) {
            var option = args.FirstOrDefault();
            switch (option) {
                case null:
                case "http":
                    return new HttpClientHub("main_db", "http://localhost:8010/fliox/");
                case "ws":
                    var wsHub = new WebSocketClientHub("main_db", "ws://localhost:8010/fliox/");
                    wsHub.Connect().Wait();
                    return wsHub;
                case "file":
                    var db = new FileDatabase("main_db", "../TodoHub/DB~/main_db");
                    return new FlioxHub(db);
            }
            throw new InvalidOperationException($"unknown option: '{option}' use: [http, ws, file]");
        }
    }
}
