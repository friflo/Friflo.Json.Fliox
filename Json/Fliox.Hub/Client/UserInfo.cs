// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Friflo.Json.Fliox.Mapper;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Client
{
    public readonly struct UserInfo {
                                    public  readonly    JsonKey     userId; 
        [DebuggerBrowsable(Never)]  public  readonly    string      token;
                                    public  readonly    JsonKey     clientId;

        public override                                 string      ToString() => $"userId: {userId}, clientId: {clientId}";

        public UserInfo (in JsonKey userId, string token, in JsonKey clientId) {
            this.userId     = userId;
            this.token      = token;
            this.clientId   = clientId;
        }
    }
}
