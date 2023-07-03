using Castle.DynamicProxy;
using CastleProxiesTest;
using CastleProxiesTest.DbEntities;
using System.Data;
using System.Diagnostics;

internal class Program {
    private static void Main(string[] args) {

        var context = new DatabaseContext();
        // Create a new entity and save it to the database
        var sam = new User() {
            Name = "Sammy"
        };
        context.Users.Add(sam);
        context.SaveChanges();

        // Save the revert time point and
        // then make some modifications and save
        var revertPoint = DateTime.Now;
        sam.Name = "Sam";
        context.SaveChanges();

        // Create a new wayback instance with
        // a fresh database context and the revert time
        // and get the old version of the entity
        var wayback = WayBack.CreateWayBack(new DatabaseContext(), revertPoint);
        var oldsam = wayback.DbSetFirst<User>(s => s.Name == "Sam");
        Console.WriteLine($"Old Name : {oldsam.Name}");

        // Prints : Old Name : Sammy

    }
}