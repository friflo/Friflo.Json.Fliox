
The records in a [Role](./) container are used to authorize (allow) execution of tasks.

The container is a set of roles which are references in [UserPermission](../UserPermission) roles[].
Each *Role* contains a set of rights[]. A Task execution is authorized if any (at least one) right allows execution.

The following *Right* types are available:

| type               | description                                                                              |
| ------------------ | ---------------------------------------------------------------------------------------- |
| "allow"            | Allow unrestricted access to a database if "grant" == true                               |
| "database"         | Allow read / write operations and change subscriptions to the specified "containers"     |
| "message"          | Allow sending the specified messages to a database by their "names"                      |
| "subscribeMessage" | Allow subscribing to the specified messages sent to a database by their "names"          |
| "predicate         | Allow execution of arbitrary tasks by the given list predicate function "names.          |


## Predicate

Predicates are registered functions performing authorization via code and returning true to allow task execution.
In contrast to all other "Right" types they enable filtering tasks by type and their specific properties.

Examples:
- message / command tasks and their specific values

- specific database tasks. E.g.
    - *read* tasks are restricted to specific entity ids.

    - *query" task with the restriction that they contain some specific filters.

    - *create*, *change*, *delete* or *patch* are restricted to specific ids or other entity properties.
