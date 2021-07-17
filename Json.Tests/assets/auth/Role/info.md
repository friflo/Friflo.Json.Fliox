
The records in a [Role](./) container are used to authorize (allow) execution of tasks.

The container is a set of roles which are references in [UserPermission](../UserPermission) roles[].
Each *Role* contains a set of rights[]. A Task execution is authorized if any (at least one) right allows execution.
