import kotlinx.serialization.json.Json
import kotlinx.serialization.*

fun main(args: Array<String>) {
    println("Hello World!")

    // Try adding program arguments at Run/Debug configuration
    println("Program arguments: ${args.joinToString()}")


    var data = Data(42, "str")

    var xxx = Json.encodeToString(data)

}

@Serializable
data class Data(val a: Int, val b: String)