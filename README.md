# EFCore Wayback

A small library that implements a way to revert an EFCore object to a previous state. The entities returned by the wayback class are `Castle.Core.Proxies` proxies. That allows lazy loading just like EFCore, however the lazy loaded entities are also reverted back in time to the same point as the parent entity.

### Usage

```c#
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
var oldsam = wayback.GetEntity<User>(s => s.Name == "Sam");
Console.WriteLine($"Old Name : {oldsam.Name}");

// Prints : Old Name : Sammy
```

