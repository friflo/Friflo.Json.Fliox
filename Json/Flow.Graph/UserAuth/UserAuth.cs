// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Utils;


namespace Friflo.Json.Flow.UserAuth
{
    /// <summary>
    /// Thread safe implementation of <see cref="IUserAuth"/> using a <see cref="EntityDatabase"/> for
    /// storing user tokens and roles.
    /// </summary>
    public class UserAuth : IUserAuth, IDisposable
    {
        private readonly SharedPool<UserStore>   storePool;
        
        public UserAuth(EntityDatabase authDatabase) {
            storePool = new SharedPool<UserStore>      (() => new UserStore(authDatabase, UserStore.AuthUser));
        }

        public async Task<AuthenticateUserResult> AuthenticateUser(AuthenticateUser value) {
            using (var pooledStore = storePool.Get()) {
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