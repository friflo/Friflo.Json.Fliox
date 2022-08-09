// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Client
{
    public delegate void MemberSelectionBuilder<T>(MemberSelection<T> selection) where T : class;
    
    public sealed class MemberSelection<T> where T : class
    {
        public              IReadOnlyList<string>   Members     => members;
        private  readonly   List<string>            members     = new List<string>();
        private             MemberAccess            memberAccess;   // cached MemberAccess
        private             bool                    isFrozen;
        
        public   override   string                  ToString() => FormatToString();

        public MemberSelection<T> Add(Expression<Func<T, object>> member) {
            if (member == null)     throw new ArgumentNullException(nameof(member));
            if (isFrozen)           throw new InvalidOperationException("MemberSelection is already frozen");
            var memberPath = Operation.PathFromLambda(member, ClientStatic.RefQueryPath);
            memberAccess = null;
            members.Add(memberPath);
            return this;
        }
        
        public MemberSelection<T> Add(MemberPath<T> memberPath) {
            if (memberPath == null) throw new ArgumentNullException(nameof(memberPath));
            if (isFrozen)           throw new InvalidOperationException("MemberSelection is already frozen");
            memberAccess = null;
            members.Add(memberPath.path);
            return this;
        }
        
        public MemberSelection<T> Freeze() {
            isFrozen = true;
            return this;
        }
        
        internal MemberAccess GetMemberAccess() {
            if (memberAccess != null)
                return memberAccess;
            return memberAccess = new MemberAccess(members);
        }
        
        private string FormatToString() {
            var sb = new StringBuilder();
            AppendTo(sb);
            return sb.ToString();
        }

        internal void AppendTo(StringBuilder sb) {
            sb.Append('[');
            for (int n = 0; n < members.Count; n++) {
                if (n > 0)
                    sb.Append(", ");
                sb.Append(members[n]);
            }
            sb.Append(']');
        }
    }
    
    public sealed class MemberPath<T>
    {
        internal readonly string path;

        public override string ToString() => path;

        public MemberPath(Expression<Func<T, object>> member) {
            if (member == null)
                throw new ArgumentException($"MemberPath<{typeof(T).Name}>() member must not be null.");
            path = Operation.PathFromLambda(member, ClientStatic.RefQueryPath);
        }
    }
}