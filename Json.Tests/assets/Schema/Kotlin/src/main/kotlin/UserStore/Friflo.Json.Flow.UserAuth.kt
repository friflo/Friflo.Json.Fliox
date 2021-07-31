
data class Role (
    val rights       : List<Right>,
    val description? : String | null,
)

data class UserCredential (
    val passHash? : String | null,
    val token?    : String | null,
)

data class UserPermission (
    val roles? : List<String> | null,
)

