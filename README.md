[![test](https://img.shields.io/badge/Download_on_Nuget-blue)](https://www.nuget.org/packages/WaybackMachine/) ![Static Badge](https://img.shields.io/badge/Version-1.8-green)

# Wayback for Entity Framework Core 
This is a small library that implements a way to revert an `EFCore` object to a previous state. The entities returned by the `WayBack` class are `Castle.Core.Proxies` proxies. That allows lazy loading just like `EFCore`, however the lazy loaded entities are also reverted back in time to the same point as the parent entity. The tracking data is offloaded to another database to improve efficiency

## Installation

```
PM> NuGet\Install-Package WaybackMachine -Version 1.8
```

## Usage

```c#
// Create a new entity and save it to the database
var context = new DatabaseContext();
var sam = new User() {
    Name = "Sammy"
};
context.Users.Add(sam);
context.SaveChanges();

// Save the revert time point and
// then make some modifications and save
var revertPoint = DateTime.UtcNow;
sam.Name = "Sam";
context.SaveChanges();

// Create a new wayback instance with
// a fresh database context and the revert time
// and get the old version of the entity
var wayback = WayBack.CreateWayBack(new DatabaseContext(), revertPoint);
var oldsam = wayback.GetEntity<User>(s => s.Name == "Sam");
Console.WriteLine($"Old Name : {oldsam.Name}");
// Old Name : Sammy
```



## Getting Started

1. The database context will have to implement the `IWaybackContext` interface. This allows Wayback to assimilate an instance of that context and 

```csharp
public class DatabaseContext : DbContext, IWaybackContext  
```

2. It will also implement the `BaseSaveChanges` method. This method is the method that Wayback uses as a the base method to save changes. Ideally this method should call the native `SaveChanges()` method in the Entity Framework context.

```C#
public int BaseSaveChanges() => base.SaveChanges();
```

3. Override the `SaveChanges` method of the database context to call the extension method `WaybackMachine.WaybackDbContextExtension.SaveChangesWithTracking()` like so. If you don't want the tracking to be enabled by default, you can skip this step

```C#
public override int SaveChanges() => this.SaveChangesWithTracking();
```


4. You finally need to define the database connection string for the auditing database in `appsettings.json`. The connection string name should be `WaybackTracking`. Note that version updates may include migrations to the database and they will not be automatically applied unless the backup path for the Wayback database is defined in `appsettings.json` at `Wayback:Migration:BackupPath`

```json
{
	"ConnectionStrings": {
		"WaybackTracking": "Connection string to the wayback database"
	},
    "Wayback": {
        "Migration": {
            "BackupPath": "/path/to/backup"
        }
    }
}
```

5. To implement soft deletion, you need to implement the interface `IWaybackSoftDelete` on the entities you wish to soft delete. This interface will implement a `datetime?` property that allows Wayback to determine when the entity was deleted. Of course it is important to note that soft deleting entities will not 

```c#
[SoftDelete]
public class Message : IWaybackSoftDeletable {
    public Message() {
        Guid = Guid.NewGuid();
    }
    [Key]
    public int ID { get; set; }
    public string Contents { get; set; }
    public virtual User? Sender { get; set; }
    public virtual User? Recipient { get; set; }

    [NotMapped]
    public Guid Guid { get; set; }
    public DateTime? DeleteDate { get; set; }
}
```
4. Call `IWaybackContext.ConfigureWaybackModel(ModelBuilder mb)` in the `OnModelCreating(ModelBuilder mb)` method in your database context. This is done so that Wayback can add a query filter to exclude soft deleted results from queries by default. You can skip this step if you want to handle query filtering on your own

```c#
protected override void OnModelCreating(ModelBuilder modelBuilder) {
    this.ConfigureWaybackModel(modelBuilder);
}
```


# :exclamation: Limitations

- This implementing this library will slow down the process of saving your changes to the database considerably, because additional records will have to created each time. Read speeds of this library is unimpressive as well and you should consider alternatives where the read speed is critical
- Reversal will not work properly when the reversed entity is making references to a deleted entity (duh). For those cases please implement soft deletion for those entities
- I've only tested with TPT inheritance so far and I'm not entirely sure if TPH will work properly with it



## Attributes

`DoNotAudit` : Properties with this attribute will not be audited and tracked

`CensorAudit` : Properties with this attribute will only have the hashes saved. Only works for `byte[]` and `string`

`JunctionTable` : This indicates to Wayback that a class is a junction for handling Many-To-Many relationships

`Audit` : This indicates to Wayback that an entity should be audited and tracked

`SoftDelete` : This indicates that entities have to be soft deleted instead of hard deleted. If this attribute is added to an entity, then it implemented a `bool IsDeleted` parameter. If this attribute is implemented, the `OnModelCreating` method will also implement a Query filter on it. Entities with this attribute also have to implement the `IWaybackSoftDeletable` interface



