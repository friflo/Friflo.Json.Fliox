
The container [credentials](./) is used to store and validate credentials of a user.
These credentials are a hashed password and a user token.
Tokens are used to authenticate users.
If user authentication is successful the user is permitted to execute tasks which are permitted by its [permissions](../permissions).

Note:
Performing user authentication is an optional feature.
Alternative user authentication systems can be implemented and used. E.g. by facebook, Google, Amazon, Microsoft, ...

By default the user database access is set to the minimum required permissions.
The user database enables executing the listed tasks depending on the user (userId's).

- `AuthUser`
    - command:   "AuthenticateUser"
    - container: "UserPermission": `read`
    - container: "Role":           `read`, `query`

- `Server`
    - container: "UserCredential": read

These rights given at: 
[UserDatabaseAuthenticator.cs](../../../../../Json/Fliox.Hub/DB/UserAuth/UserDatabaseAuthenticator.cs)

Additional users can be added to this list if required via `UserDatabaseAuthenticator.userRights`.
