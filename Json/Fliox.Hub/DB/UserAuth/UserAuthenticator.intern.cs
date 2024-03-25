// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Fliox.Hub.Host.Auth;

namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    /// <summary>
    /// Used to store the rights given by <see cref="Role.taskRights"/> and <see cref="Role.hubRights"/>
    /// </summary>
    internal sealed class UserAuthRole
    {
        internal readonly   string              id;
        /// <summary> assigned by <see cref="Role.taskRights"/> </summary>
        internal readonly   TaskAuthorizer[]    taskAuthorizers;
        /// <summary> assigned by <see cref="Role.hubRights"/> </summary>
        internal readonly   HubPermission       hubPermission;
        
        internal UserAuthRole(string id, TaskAuthorizer[] taskAuthorizers, HubPermission hubPermission) {
            this.id                 = id;
            this.taskAuthorizers    = taskAuthorizers;
            this.hubPermission      = hubPermission;
        }
    }
    
    /// <summary>
    /// Used to aggregated authorizers, permissions, groups and roles for a specific user. 
    /// </summary>
    internal sealed class UserAuthInfo
    {
        private  readonly   List<TaskAuthorizer>    taskAuthorizers = new List<TaskAuthorizer>();
        private  readonly   List<HubPermission>     hubPermissions  = new List<HubPermission>();
        private  readonly   HashSet<string>         groups          = new HashSet<string>();
        private  readonly   HashSet<string>         roles           = new HashSet<string>();
        
        internal void AddRole(UserAuthRole role) {
            if (roles.Contains(role.id)) {
                return;
            }
            roles.Add(role.id);
            taskAuthorizers.AddRange(role.taskAuthorizers);
            hubPermissions.Add(role.hubPermission);
        }
        
        internal void AddGroups(List<string> groups) {
            if (groups == null)
                return;
            this.groups.UnionWith(groups);
        }
        
        internal void AddUserAuth(User user) {
            roles.UnionWith(user.Roles);
            groups.UnionWith(user.GetGroups());
            taskAuthorizers.Add(user.taskAuthorizer);
            hubPermissions.Add(user.HubPermission);
        }
        
        internal string[] GetRoles() {
            if (roles.Count == 0) {
                return null;
            }
            return roles.ToArray();
        }
        
        internal IReadOnlyCollection<string> GetGroups() {
            return groups;
        }
        
        internal HubPermission GetHubPermission() {
            bool queueEvents = false;
            foreach (var permission in hubPermissions) {
                queueEvents |= permission.queueEvents;
            }
            return new HubPermission(queueEvents);
        }
        
        internal TaskAuthorizer GetTaskAuthorizer() {
            var authorizers = new List<TaskAuthorizer>();
            // convert hierarchy to flat list
            foreach (var authorizer in taskAuthorizers) {
                AddTaskAuthorizer(authorizers, authorizer);
            }
            foreach (var authorizer in authorizers) {
                if (authorizer is AuthorizeGrant) {
                    return TaskAuthorizer.Full;
                }
                if (authorizer is AuthorizeAny) throw new InvalidOperationException("expect flat list");
            }
            return TaskAuthorizer.ToAuthorizer(authorizers);
        }
        
        private static void AddTaskAuthorizer(List<TaskAuthorizer> authorizers, TaskAuthorizer authorizer) {
            switch (authorizer) {
                case AuthorizeDatabase          _:
                case AuthorizeTaskType          _:
                case AuthorizeContainer         _:
                case AuthorizeSendMessage       _:
                case AuthorizeSubscribeMessage  _:
                case AuthorizeSubscribeChanges  _:
                case AuthorizePredicate         _:
                case AuthorizeGrant             _:
                    authorizers.Add(authorizer);
                    break;
                case AuthorizeAny               any:
                    foreach (var item in any.list) {
                        AddTaskAuthorizer(authorizers, item);
                    }
                    break;
                case AuthorizeDeny _:
                    break;
                default:
                    throw new InvalidOperationException($"unexpected authorizer: {authorizer}");
            }
        }
    }
}