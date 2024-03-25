// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>Contains the tuple of <see cref="userId"/>, <see cref="token"/> and <see cref="clientId"/></summary>
    public readonly struct UserInfo {
                                    public  readonly    ShortString     userId; 
        [DebuggerBrowsable(Never)]  public  readonly    ShortString     token;
                                    public  readonly    ShortString     clientId;

        public override                                 string          ToString() => $"userId: {userId}, clientId: {clientId}";

        public UserInfo (in ShortString userId, in ShortString token, in ShortString clientId) {
            this.userId     = userId;
            this.token      = token;
            this.clientId   = clientId;
        }
    }
}
