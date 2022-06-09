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