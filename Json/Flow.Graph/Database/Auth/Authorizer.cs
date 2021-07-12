// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Auth
{
    public abstract class Authorizer
    {
        public abstract bool Authorize(DatabaseTask task, MessageContext messageContext);
    }
    
    public class AuthorizeAll : Authorizer {
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            return true;
        }
    }
    
    public class AuthorizeNone : Authorizer {
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            return false;
        }
    }
    
    public class AuthorizeReadOnly : Authorizer {
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            switch (task.TaskType) {
                case TaskType.query:
                case TaskType.read:
                case TaskType.message:
                case TaskType.subscribeChanges:
                case TaskType.subscribeMessage:
                    return true;
            }
            return false;
        }
    }
}