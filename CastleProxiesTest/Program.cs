using Castle.DynamicProxy;
using CastleProxiesTest;
using CastleProxiesTest.DbEntities;
using System.Data;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using WaybackMachine;

internal class Program {
    private static void Main(string[] args) {

        //var context = new DatabaseContext();
        //// Create a new entity and save it to the database
        //var sam = new User() {
        //    Name = "Sammy"
        //};
        //context.Users.Add(sam);
        //context.SaveChanges();

        //// Save the revert time point and
        //// then make some modifications and save
        //var revertPoint = DateTime.Now;
        //sam.Name = "Sam";
        //context.SaveChanges();

        //// Create a new wayback instance with
        //// a fresh database context and the revert time
        //// and get the old version of the entity
        //var wayback = WayBack.CreateWayBack(new DatabaseContext(), revertPoint);
        //var oldsam = wayback.DbSetFirst<User>(s => s.Name == "Sam");
        //Console.WriteLine($"Old Name : {oldsam.Name}");

        //// Prints : Old Name : Sammy
        ///

        var count = 5000;


        var guid_data = Enumerable.Range(0, count).Select(s => Guid.NewGuid())
            .Cast<object>()
            .ToArray();
        RunBenchmark(guid_data, typeof(Guid));

        var date_data = Enumerable.Range(0, count).Select(s => DateTime.Now.AddMinutes(s))
            .Cast<object>()
            .ToArray();
        RunBenchmark(date_data, typeof(DateTime));


        var int_data = Enumerable.Range(0, count).Select(s => s)
            .Cast<object>()
            .ToArray();
        RunBenchmark(int_data, typeof(int));



    }

    private static void RunBenchmark(object[] list, Type t) {
        var dataStore = new List<string>();

        var write_sw = new Stopwatch();
        write_sw.Start();
        for (int i = 0; i < list.Length; i++) { 
            dataStore.Add(JsonSerializer.Serialize(list[i]));
        }
        write_sw.Stop();

        var convertBack = new List<object>();

        dataStore.ForEach(x => Console.WriteLine(x));

        var read_sw = new Stopwatch();
        read_sw.Start();
        for (int i = 0; i < list.Length; i++) {
            var raw = dataStore[i];
            convertBack.Add(JsonSerializer.Deserialize(raw, t));
        }
        read_sw.Stop();

        Console.WriteLine($"Benchmark Results for typeof {t.Name}");
        Console.WriteLine($"\t Write completed in {write_sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"\t Ready completed in {read_sw.ElapsedMilliseconds}ms\n");


    }
}