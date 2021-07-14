// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Utils;

namespace Friflo.Json.Flow.UserAuth
{
    public class UserAuth : IUserAuth, IDisposable
    {
        private readonly UserStorePool   storePool;
        
        public UserAuth(EntityDatabase authDatabase, string clientId) {
            storePool = new UserStorePool(authDatabase, clientId);
        }

        public async Task<AuthenticateUserResult> AuthenticateUser(AuthenticateUser value) {
            using (var pooledStore = storePool.UserStore.Get()) {
                var store = pooledStore.instance;
                var result = await store.AuthenticateUser(value);
                return result;
            }
        }

        public void Dispose() {
            storePool.Dispose();
        }
    }
    
    
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