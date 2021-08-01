import CustomSerializer.BigIntegerSerializer
import kotlinx.datetime.Instant
import kotlinx.serialization.json.Json
import kotlinx.serialization.*
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put
import java.math.BigInteger
import kotlin.collections.HashMap



fun main(args: Array<String>) {
    println("Hello World!")

    // Try adding program arguments at Run/Debug configuration
    println("Program arguments: ${args.joinToString()}")

    var instant = Instant.parse("2021-07-22T06:00:00.000Z");
    var bigInt  = BigInteger("123");
    var jsonVal = buildJsonObject {
        put ("hello", "world")
    }

    var data = Data(42, "str", instant, bigInt, jsonVal)

    var json = Json.encodeToString(data)
    System.out.println("json: " + json);
}

@Serializable
class Data(
    val a:          Int,
    val b:          String,
    var dateTime:   Instant,
    @Serializable(with = BigIntegerSerializer::class)
    var bigInt:     BigInteger,
    var json:       JsonElement,
    var list:       List<String>?               = null,   // optional parameter
    var map:        HashMap<String, String>?    = null,
)


fun learnTypes () {
    var bool:       Boolean = true
    var str:        String = "hello"

    var byte:       Byte = 100;
    var short:      Short = 1000;
    var int:        Int = 10000000;
    var long:       Long = 2000000000000000000;

    var float:      Float = 1.1f;
    var double:     Double = 1.1;
    var dateTime:   Instant
}

enum class TestEnum {
    Test
}

