# Entity Framework Core : Wayback 🕝

A small library that implements a way to revert an `EFCore` object to a previous state. The entities returned by the `WayBack` class are `Castle.Core.Proxies` proxies. That allows lazy loading just like `EFCore`, however the lazy loaded entities are also reverted back in time to the same point as the parent entity.

## Usage

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



## Setup

1. The database context will have to implement the `IWaybackContext` interface

```csharp
public class DatabaseContext : DbContext, IWaybackContext  
```

2. The interface will require you to implement three fields. Implement them like so

```c#
public DbContext InternalDbContext => this;
public DbSet<AuditRecord> AuditEntries { get; set; }
public DbSet<AuditTransactionRecord> AuditTransactions { get; set; }
```

3. It will also implement the `BaseSaveChanges` method. Implement that method like so

```C#
public int BaseSaveChanges() => base.SaveChanges();		
```

3. In the `OnModelCreating` method, call the extension method `WaybackMachine.WaybackDbContextExtension.ConfigureWaybackModel`

```c#
this.ConfigureWaybackModel(modelBuilder);
```

4. Override the `SaveChanges` method of the database context to call the extension method `WaybackMachine.WaybackDbContextExtension.SaveChangesWithTracking` like so

```C#
public override int SaveChanges() => this.SaveChangesWithTracking();
```



## Attributes

`DoNotAudit` : Properties with this attribute will not be audited and tracked
`CensorAudit` : Properties with this attribute will only have the hashes saved. Only works for `byte[]` and `string`
`JunctionTable` : This indicates to Wayback that a class is a junction for handling Many-To-Many relationships
`Audit` : This indicates to Wayback that an entity should be audited and tracked

