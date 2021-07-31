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
data class Data(
    val a:      Int,
    val b:      String,
    var list:   List<String>?               = null,   // optional parameter
    var map:    HashMap<String, String>?    = null
)


fun learnTypes () {
    var bool:   Boolean = true
    var str:    String = "hello"

    var byte:   Byte = 100;
    var short:  Short = 1000;
    var int:    Int = 10000000;
    var long:   Long = 2000000000000000000;

    var float:  Float = 1.1f;
    var double: Double = 1.1;
}



enum class TestEnum {
    Test
}