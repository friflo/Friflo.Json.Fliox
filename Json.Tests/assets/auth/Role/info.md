
The records in a [Role](./) container are used to authorize (allow) execution of tasks.

The container is a set of roles which are referenced in [UserPermission](../UserPermission) `roles[]`.
Each `Role` contains a set of `rights[]`. A Task execution is authorized if any (at least one) right allows execution.

The following `Right` types are available:

| `type`             | description                                                                              |
| ------------------ | ---------------------------------------------------------------------------------------- |
| `allow`            | Allow unrestricted access to a database if "grant" == true                               |
| `database`         | Allow read / write operations and change subscriptions to the specified `containers`     |
| `message`          | Allow sending the specified messages to a database by their `names`                      |
| `subscribeMessage` | Allow subscribing to the specified messages sent to a database by their `names`          |
| `task`             | Allow task execution of the specified task `types`.                                      |
| `predicate`        | Allow execution of arbitrary tasks by the given list of predicate function `names`.      |


## `database`

The `database` `Right` contains are map of `containers` referencing a specific container by key.
The value of each entry authorize container specific tasks by `operations` and `subscribeChanges`.

- `operations`  
                        Allow read and write operations on a container. These operations are:  
                        `create`, `update`, `delete`, `patch`, `read`, `query`, `mutate`, `full`  
                        `mutate` is shortcut for `create`, `update`, `delete`, `patch`  
                        `all` is a shortcut for all operations

- `subscribeChanges`  
                        Allow subscription to entity changes in a container. These change types are:  
                        `create`, `update`, `delete`, `patch`


## `message`

Sending a message to the database can be authorized by its name listed in `names`.  
Alternatively a group of messages can authorized by prefix filter using `*` in `names`. E.g. `"names": ["Command*"]`


## `subscribeMessage`

A message subscription can be authorized by its message name listed in `names`.  
Alternatively a group of message subscriptions can authorized by a prefix filter using `*` in `names`. E.g. `"names": ["Event*"]`


## `task`

The `task` `Right` contains an array of task `types`.
Task execution of a specific task type is allowed in case it is listed inside `types`.

The following task types can be used:

`read`, `query`, `create`, `update`, `delete`, `patch`, `message`, `subscribeChanges`, `subscribeMessage`


## `predicate`

Predicates are registered functions performing authorization via code and returning true to allow task execution.  
In contrast to all other `Right` types they enable filtering tasks by type and their specific properties.  
`predicate` rights are required if task authorization cannot be expressed by the common Rights like: `database`, 
`message`, `subscribeMessage` or `task`.

Examples:
- message / command tasks and their specific values

- specific database tasks. E.g.
    - *read* tasks are restricted to specific entity ids.

    - *query* task with a restriction that they contain some specific filters.

    - *create*, *change*, *delete* or *patch* are restricted to specific ids or other entity properties.
