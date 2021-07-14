// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;


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
}