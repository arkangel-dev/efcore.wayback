[![test](https://img.shields.io/badge/Download_on_Nuget-blue)](https://www.nuget.org/packages/WaybackMachine/) ![Static Badge](https://img.shields.io/badge/Version-1.5.2-green)

# Wayback for Entity Framework Core 

A small library that implements a way to revert an `EFCore` object to a previous state. The entities returned by the `WayBack` class are `Castle.Core.Proxies` proxies. That allows lazy loading just like `EFCore`, however the lazy loaded entities are also reverted back in time to the same point as the parent entity.

## Installation

```
PM> NuGet\Install-Package WaybackMachine -Version 1.5.2
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
var revertPoint = DateTime.Now;
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

3. In the `OnModelCreating` method, call the extension method `WaybackMachine.WaybackDbContextExtensions.ConfigureWaybackModel`

```c#
this.ConfigureWaybackModel(modelBuilder);
```

4. Override the `SaveChanges` method of the database context to call the extension method `WaybackMachine.WaybackDbContextExtension.SaveChangesWithTracking` like so. This step is optional

```C#
public override int SaveChanges() => this.SaveChangesWithTracking();
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

`SoftDelete` : This indicates that entities have to be soft deleted instead of hard deletes. If this attribute is added to an entity, then it implemented a `bool IsDeleted` parameter. If this attribute is implemented, the `OnModelCreating` method will also implement a Query filter on it. Entities with this attribute also have to implement the `IWaybackSoftDeletable` interface



