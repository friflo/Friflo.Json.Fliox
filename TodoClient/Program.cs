using System;
using System.Linq;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;

namespace TodoClient
{
    internal  static class  Program
    {
        public static void Main(string[] args) {
            var hub         = CreateClient(args);
            var todoStore   = new TodoStore(hub);
            var todos       = todoStore.todos.QueryAll();
            todoStore.SyncTasks().Wait();
            
            foreach (var todo in todos.Result) {
                Console.WriteLine($"id: {todo.id}, title: {todo.title}, completed: {todo.completed}");
            }
        }
        
        private static FlioxHub CreateClient(string[] args) {
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
