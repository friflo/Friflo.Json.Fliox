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
            var command = args.FirstOrDefault();
            if (command == null) {
                return new HttpClientHub("main_db", "http://localhost:8010/fliox/");
            }
            var hub = new WebSocketClientHub("main_db", "ws://localhost:8010/fliox/");
            hub.Connect().Wait();
            return hub;
        }
    }
}
