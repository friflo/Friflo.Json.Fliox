// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class PatchTask<T> : SyncTask where T : class
    {
        public              IReadOnlyList<EntityPatchInfo>  Patches => patches;
        
        internal readonly   MemberSelection<T>              selection;
        [DebuggerBrowsable(Never)]
        internal readonly   List<EntityPatchInfo>           patches;
        private  readonly   SyncSetBase<T>                  syncSet;

        [DebuggerBrowsable(Never)]
        internal            TaskState                       state;
        internal override   TaskState                       State   => state;
        public   override   string                          Details => GetDetails();
        
        internal PatchTask(SyncSetBase<T> syncSet, MemberSelection<T> selection) {
            patches         = new List<EntityPatchInfo>();
            this.syncSet    = syncSet;
            this.selection  = selection;
        }

        public PatchTask<T> Add(T entity) {
            var entities = new List<T> { entity };
            syncSet.AddEntityPatches(this, entities);
            return this;
        }
        
        public PatchTask<T> AddRange(ICollection<T> entities) {
            syncSet.AddEntityPatches(this, entities);
            return this;
        }
        
        internal void SetResult(IDictionary<JsonKey, EntityError> errorsPatch) {
            var entityErrorInfo = new TaskErrorInfo();
            foreach (var patch in patches) {
                if (errorsPatch.TryGetValue(patch.entityPatch.id, out EntityError error)) {
                    entityErrorInfo.AddEntityError(error);
                }
            }
            if (entityErrorInfo.HasErrors) {
                state.SetError(entityErrorInfo);
            } else {
                state.Executed = true;
            }
        }
        
        private string GetDetails() { 
            var sb = new StringBuilder();
            sb.Append("PatchTask<");
            sb.Append(typeof(T).Name);
            sb.Append("> patches: ");
            sb.Append(patches.Count);
            sb.Append(", selection: [");
            selection.FormatToString(sb);
            sb.Append(']');
            return sb.ToString();
        }
    }
    
    public delegate void MemberSelectionBuilder<T>(MemberSelection<T> member) where T : class;
    
    public class MemberSelection<T> where T : class
    {
        public              IReadOnlyList<string>   Members     => members;
        private  readonly   List<string>            members     = new List<string>();
        private             MemberAccess            memberAccess;   // cached MemberAccess
        
        public   override   string                  ToString()  => FormatToString(new StringBuilder());

        public void Add(Expression<Func<T, object>> member) {
            if (member == null)
                throw new ArgumentNullException(nameof(member));
            var memberPath = Operation.PathFromLambda(member, EntitySet.RefQueryPath);
            memberAccess = null;
            members.Add(memberPath);
        }
        
        public void Add(MemberPath<T> memberPath) {
            if (memberPath == null)
                throw new ArgumentNullException(nameof(memberPath));
            memberAccess = null;
            members.Add(memberPath.path);
        }
        
        internal MemberAccess GetMemberAccess() {
            if (memberAccess != null)
                return memberAccess;
            return memberAccess = new MemberAccess(members);
        }

        internal string FormatToString(StringBuilder sb) {
            for (int n = 0; n < members.Count; n++) {
                if (n > 0)
                    sb.Append(", ");
                sb.Append(members[n]);
            }
            return sb.ToString();
        }
    }
    
    public sealed class MemberPath<T>
    {
        internal readonly string path;

        public override string ToString() => path;

        public MemberPath(Expression<Func<T, object>> member) {
            if (member == null)
                throw new ArgumentException($"MemberPath<{typeof(T).Name}>() member must not be null.");
            path = Operation.PathFromLambda(member, EntitySet.RefQueryPath);
        }
    }
}
