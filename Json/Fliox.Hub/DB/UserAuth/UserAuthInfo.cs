// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host.Auth;

namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    internal sealed class UserAuthInfo
    {
        internal readonly   TaskAuthorizer      taskAuthorizer;
        internal readonly   HubPermission       hubPermission;
        internal readonly   List<ShortString>   groups;
        internal readonly   List<string>        roles;
        
        internal UserAuthInfo(
            IList<TaskAuthorizer>   authorizers,
            IList<HubPermission>    hubPermissions,
            List<ShortString>       groups,
            List<string>            roles)
        {
            this.taskAuthorizer = CreateAuthorizer(authorizers);
            this.groups         = groups;
            this.roles          = roles;
            
            bool queueEvents = false;
            foreach (var permission in hubPermissions) {
                queueEvents |= permission.queueEvents;
            }
            hubPermission  = new HubPermission(queueEvents);
        }
        
        private static TaskAuthorizer CreateAuthorizer(IList<TaskAuthorizer> authorizers) {
            var taskAuthorizers = new List<TaskAuthorizer>();
            foreach (var authorizer in authorizers) {
                switch (authorizer) {
                    case AuthorizeDatabase          _:
                    case AuthorizeTaskType          _:
                    case AuthorizeContainer         _:
                    case AuthorizeSendMessage       _:
                    case AuthorizeSubscribeMessage  _:
                    case AuthorizeSubscribeChanges  _:
                    case AuthorizePredicate         _:
                    case AuthorizeAny               _:
                        taskAuthorizers.Add(authorizer);
                        break;
                    case AuthorizeGrant             _:
                        return TaskAuthorizer.Full;
                    case AuthorizeDeny _:
                        break;
                    default:
                        throw new InvalidOperationException($"unexpected authorizer: {authorizer}");
                }
            }
            return TaskAuthorizer.ToAuthorizer(taskAuthorizers);
        }
    }
}