
The container [UserPermission](./) is used to authorize (allow / deny) execution of tasks for a specific user.

Each user contains a set of roles. These roles are stored in the [Role](../Role/) container.
If any (at least one) of the assigned roles allows task execution, the task will be executed.
In other words the user roles are OR-ed.
