
data class Role(
    override  val id          : String,
              val rights      : List<Right>,
              val description : String? = null,
) : Entity (id)

data class UserCredential(
    override  val id       : String,
              val passHash : String? = null,
              val token    : String? = null,
) : Entity (id)

data class UserPermission(
    override  val id    : String,
              val roles : List<String>? = null,
) : Entity (id)

