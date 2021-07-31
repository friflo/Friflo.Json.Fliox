
data class Right (
    val description : String? = null,
)

data class RightAllow (
    val grant       : Boolean,
)

data class RightTask (
    val types       : List<TaskType>,
)

data class RightMessage (
    val names       : List<String>,
)

data class RightSubscribeMessage (
    val names       : List<String>,
)

data class RightDatabase (
    val containers  : HashMap<String, ContainerAccess>,
)

data class ContainerAccess (
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

data class RightPredicate (
    val names       : List<String>,
)

