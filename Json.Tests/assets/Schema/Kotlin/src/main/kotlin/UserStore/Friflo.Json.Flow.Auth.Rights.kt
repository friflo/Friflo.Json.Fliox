
abstract class Right(
    open  val description : String? = null,
)

data class RightAllow(
              val grant       : Boolean,
    override  val description : String? = null,
) : Right (description)

data class RightTask(
              val types       : List<TaskType>,
    override  val description : String? = null,
) : Right (description)

data class RightMessage(
              val names       : List<String>,
    override  val description : String? = null,
) : Right (description)

data class RightSubscribeMessage(
              val names       : List<String>,
    override  val description : String? = null,
) : Right (description)

data class RightDatabase(
              val containers  : HashMap<String, ContainerAccess>,
    override  val description : String? = null,
) : Right (description)

data class ContainerAccess(
              val operations       : List<OperationType>? = null,
              val subscribeChanges : List<Change>? = null,
)

enum class OperationType {
    create,
    update,
    delete,
    patch,
    read,
    query,
    mutate,
    full,
}

data class RightPredicate(
              val names       : List<String>,
    override  val description : String? = null,
) : Right (description)

