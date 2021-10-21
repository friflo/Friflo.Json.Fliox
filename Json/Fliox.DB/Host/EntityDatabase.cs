// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host.Internal;
using Friflo.Json.Fliox.DB.Protocol;

namespace Friflo.Json.Fliox.DB.Host
{
    public class DbOpt {
        /// <see cref="EntityDatabase.customContainerName"/>
        public  readonly    CustomContainerName     customContainerName;
        
        public readonly     TaskHandler         taskHandler;
        
        public DbOpt(CustomContainerName customContainerName = null, TaskHandler taskHandler = null) {
            this.customContainerName    = customContainerName   ?? (name => name);
            this.taskHandler            = taskHandler;
        }
        
        internal static readonly DbOpt Default = new DbOpt();
    }
    
    public delegate string CustomContainerName(string name);
    
    public abstract class EntityDatabase : IDisposable
    {
        /// <summary> map of of containers identified by their container name </summary>
        private readonly    Dictionary<string, EntityContainer> containers = new Dictionary<string, EntityContainer>();
        
        /// <summary>
        /// An optional <see cref="DatabaseSchema"/> used to validate the JSON payloads in all write operations
        /// performed on the <see cref="EntityContainer"/>'s of the database
        /// </summary>
        public              DatabaseSchema      Schema          { get; set; }
        
        /// <summary>
        /// A mapping function used to assign a custom container name.
        /// If using a custom name its value is assigned to the containers <see cref="EntityContainer.instanceName"/>. 
        /// By having the mapping function in <see cref="DatabaseHub"/> it enables uniform mapping across different
        /// <see cref="DatabaseHub"/> implementations.
        /// </summary>
        public readonly     CustomContainerName customContainerName;
        
        /// <summary>
        /// The <see cref="TaskHandler"/> execute all <see cref="SyncRequest.tasks"/> send by a client.
        /// Custom task (request) handler can be added to the <see cref="taskHandler"/> or
        /// the <see cref="taskHandler"/> can be replaced by a custom implementation.
        /// </summary>
        public readonly     TaskHandler         taskHandler;
        
        
        internal readonly   string              extensionName;
        internal readonly   DatabaseHub         extensionBase;

        public override     string              ToString() => extensionName != null ? $"'{extensionName}'" : "";

        protected EntityDatabase (DbOpt opt){
            customContainerName = (opt ?? DbOpt.Default).customContainerName;
            this.taskHandler    = opt?.taskHandler ?? new TaskHandler();
        }
        
        protected EntityDatabase (
            DatabaseHub     extensionBase,
            string          extensionName,
            DbOpt           opt) : this(opt)
        {
            this.extensionBase  = extensionBase ?? throw new ArgumentNullException(nameof(extensionBase));
            this.extensionName  = extensionName ?? throw new ArgumentNullException(nameof(extensionName));
        }

        public virtual void Dispose() {
            foreach (var container in containers ) {
                container.Value.Dispose();
            }
        }
        
        public virtual Task ExecuteSyncPrepare (SyncRequest syncRequest, MessageContext messageContext) {
            return Task.CompletedTask;
        }

        internal void AddContainer(EntityContainer container) {
            containers.Add(container.name, container);
        }
        
        protected bool TryGetContainer(string name, out EntityContainer container) {
            return containers.TryGetValue(name, out container);
        }

        public EntityContainer GetOrCreateContainer(string name)
        {
            if (containers.TryGetValue(name, out EntityContainer container))
                return container;
            containers[name] = container = CreateContainer(name, this);
            return container;
        }
        
        public abstract EntityContainer CreateContainer     (string name, EntityDatabase database);

    }
}