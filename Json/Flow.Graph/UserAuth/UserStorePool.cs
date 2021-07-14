// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Utils;

namespace Friflo.Json.Flow.UserAuth
{
    public struct UserStorePoolUsage {
        internal int    userStoreCount;
        
        public void AssertEqual(in UserStorePoolUsage other) {
            if (userStoreCount            != other.userStoreCount)          throw new InvalidOperationException("detect UserStore leak");
        }
    }
    
    public interface IUserStorePool
    {
        ObjectPool<UserStore>       UserStore           { get; }
        UserStorePoolUsage          UserStorePoolUsage  { get; }
    }
    
    public class UserStorePool : IUserStorePool, IDisposable
    {
        public              ObjectPool<UserStore>   UserStore     { get; }
        
        internal UserStorePool(EntityDatabase authDatabase, string clientId) {
            UserStore      = new SharedPool<UserStore>      (() => new UserStore(authDatabase, clientId));
        }
        
        internal UserStorePool(UserStorePool sharedPools) {
            UserStore      = new LocalPool<UserStore>       (sharedPools.UserStore,      "UserStore");
        }

        public void Dispose() {
            UserStore.    Dispose();
        }

        public UserStorePoolUsage UserStorePoolUsage { get {
            var usage = new UserStorePoolUsage {
                userStoreCount            = UserStore       .Usage,
            };
            return usage;
        } }
    }
}