using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Tests.Provider.Client;
using NUnit.Framework;

namespace Friflo.Json.Tests.Provider.Perf
{
    // Copy of Performance Test in Dapper
    // [DapperLib/Dapper: Dapper - a simple object mapper for .Net] https://github.com/DapperLib/Dapper
    // In Dapper this test is run with:
    // dotnet run --project .\benchmarks\Dapper.Tests.Performance\ -c Release -f netcoreapp3.1 -- -f HandCodedBenchmarks --join
    public static class TestPerfSqlServer
    {
        // [Test]
        public static async Task Perf_Read_OneHandCoded()
        {
            await TestPerf.SeedPosts("sqlserver_rel");
            using var connection = new SqlConnection("Data Source=.;Integrated Security=True;Database=test_rel");
            Console.WriteLine($"IsServerGC: {System.Runtime.GCSettings.IsServerGC}");
            connection.Open();
            ReadPosts(connection, TestPerf.WarmupCount);
            
            var count = TestPerf.ReadCount;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            ReadPosts(connection, count);
            var duration = stopWatch.Elapsed.TotalMilliseconds;
            Console.WriteLine($"Read. count: {count}, duration: {duration} ms");
        }

        private static void ReadPosts(SqlConnection connection, int count)
        {
            using var command = new SqlCommand("select Top 1 * from posts where Id = @Id", connection);
            var idParam = command.Parameters.Add("@Id", SqlDbType.Int);
            command.Prepare();
            
            for (int n = 0; n < count; n++) {
                idParam.Value = n % TestPerf.SeedCount;
                using (var reader = command.ExecuteReader(CommandBehavior.SingleResult | CommandBehavior.SingleRow))
                {
                    reader.Read();
                    new Post {
                        Id = reader.GetInt32(0),
                        Text = reader.GetNullableString(1),
                        CreationDate = reader.GetDateTime(2),
                        LastChangeDate = reader.GetDateTime(3),
                        Counter1 = reader.GetNullableValue<int>(4),
                        Counter2 = reader.GetNullableValue<int>(5),
                        Counter3 = reader.GetNullableValue<int>(6),
                        Counter4 = reader.GetNullableValue<int>(7),
                        Counter5 = reader.GetNullableValue<int>(8),
                        Counter6 = reader.GetNullableValue<int>(9),
                        Counter7 = reader.GetNullableValue<int>(10),
                        Counter8 = reader.GetNullableValue<int>(11),
                        Counter9 = reader.GetNullableValue<int>(12)
                    };
                }
            }
        }
        
        private static string GetNullableString(this SqlDataReader reader, int index)
        {
            object tmp = reader.GetValue(index);
            if (tmp != DBNull.Value)
            {
                return (string)tmp;
            }
            return null;
        }
        
        private static T? GetNullableValue<T>(this SqlDataReader reader, int index) where T : struct
        {
            object tmp = reader.GetValue(index);
            if (tmp != DBNull.Value)
            {
                return (T)tmp;
            }
            return null;
        }
    }
}