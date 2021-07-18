

This folder contains an example user database used to configure user authentication and authorization by utilizing a
[UserAuthenticator.cs](../../../../Json/Flow.Graph/UserAuth/UserDatabaseAuthenticator.cs) instance.

folders (containers):

## [`Role`](./Role)

Store the roles used for task authorization. These records can be referenced by `roles` in `UserPermission`


## [`UserCredential`](./UserCredential)

Used to store data for each user to enable user authentication.


## [`UserPermission`](./UserPermission)

Store a set of `roles` for each user. If a user aspire to run a task the specified `rules` are evaluated
and if authorization is successful the task is executed.


