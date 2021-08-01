import kotlinx.datetime.Instant
import kotlinx.serialization.json.Json
import kotlinx.serialization.*
import kotlinx.serialization.descriptors.PrimitiveKind
import kotlinx.serialization.descriptors.PrimitiveSerialDescriptor
import kotlinx.serialization.descriptors.SerialDescriptor
import kotlinx.serialization.encoding.Decoder
import kotlinx.serialization.encoding.Encoder
import java.math.BigInteger
import kotlin.collections.HashMap


fun main(args: Array<String>) {
    println("Hello World!")

    // Try adding program arguments at Run/Debug configuration
    println("Program arguments: ${args.joinToString()}")

    var instant = Instant.parse("2021-07-22T06:00:00.000Z");
    var bigInt  = BigInteger("123");
    var data = Data(42, "str", instant, bigInt)

    var json = Json.encodeToString(data)
    System.out.println("json: " + json);
}

@Serializable
class Data(
    val a:          Int,
    val b:          String,
    // @Serializable(with = InstantComponentSerializer::class, )
    var dateTime:   Instant,
    @Serializable(with = BigIntegerSerializer::class, )
    var bigInt:     BigInteger,
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

object BigIntegerSerializer : KSerializer<BigInteger> {
    override val descriptor: SerialDescriptor = PrimitiveSerialDescriptor("BigInteger", PrimitiveKind.STRING)

    override fun serialize(encoder: Encoder, value: BigInteger) {
        val string = value.toString();
        encoder.encodeString(string)
    }

    override fun deserialize(decoder: Decoder): BigInteger {
        val string = decoder.decodeString()
        return BigInteger(string)
    }
}