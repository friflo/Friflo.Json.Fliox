
The container "UserCredential" is used to store and validate credentials of a user.
These credentials are a hashed password and a user token.
Tokens are used to authenticate users.
If user authentication is successful the user is permitted to execute tasks which are permitted by its "UserPermission".

Note:
Performing user authentication is an optional feature.
Alternative user authentication systems can be implemented and used. E.g. by facebook, Google, Amazon, Microsoft, ...

By default the database access a set to the minimum required permissions.
The user database enables executing the listed tasks depending on the user (clientId's).

- "Server"
    - command:   "AuthenticateUser"
    - container: "UserPermission": read
    - container: "Role":           read, query

- "AuthUser"
    - container: "UserCredential": read

These rights given at: 
[UserDatabaseAuthenticator.c](../../../../Json/Flow.Graph/UserAuth/UserDatabaseAuthenticator.cs)
