using Sample;
using Sample.DbEntities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WaybackMachine;
using System.Data;

namespace WaybackMachineTests {
    [TestClass]
    public class Primary {

        private User sam;
        private User yas;
        private User jim;
        private DatabaseContext context;

        public Primary() {

        }
        [TestInitialize]
        public void Setup() {
            sam = null;
            yas = null;
            jim = null;

            var wbcontext = new WaybackDbContext();
            wbcontext.Database.EnsureCreated();
            wbcontext.AuditEntries.ExecuteDelete();
            wbcontext.AuditProperties.ExecuteDelete();
            wbcontext.AuditTables.ExecuteDelete();

            context = new DatabaseContext();
            context.Database.EnsureCreated();
            context.Messages.IgnoreQueryFilters().ExecuteDelete();
            context.Messages.ExecuteDelete();
            context.Junction_Interests_Users.IgnoreQueryFilters().ExecuteDelete();
            context.Interests.ExecuteDelete();
            context.Users.ExecuteDelete();
            context.Interests.ExecuteDelete();
            context.SaveChanges();

            // Create the users
            sam = new User("Sammy");
            yas = new User("Yas");
            jim = new User("Jimmy");

            // Add the users to the database
            context.Users.AddRange(sam, yas, jim);
            context.SaveChanges();

        }


        [TestMethod]
        public void PropertyRevseral() {
            sam.Name = "John";
            context.SaveChangesWithTracking();


            var wayback = WayBack.CreateWayBack(new DatabaseContext(), DateTime.UtcNow.AddMinutes(-5));
            var oldsam = wayback.DbSetFirst<User>(x => x.Name == "John");

            Assert.AreEqual("Sammy", oldsam.Name);
        }

        [TestMethod]
        public void SingleReferenceReversal() {
            sam.BestFriend = yas;
            context.SaveChanges();


            var wayback = WayBack.CreateWayBack(new DatabaseContext(), DateTime.UtcNow.AddMinutes(-5));
            var oldsam = wayback.DbSetFirst<User>(x => x.BestFriend == null);

            Assert.IsNull(oldsam.BestFriend);
        }

        [TestMethod("One to Many Reversal (New Entries)")]
        public void OneToManyReversal_NewEntries() {

            for (int i = 0; i < 100; i++) {
                sam.Sent.Add(new Message() {
                    Recipient = yas,
                    Contents = $"Hello World : {i}"
                });
            }
            context.SaveChanges();

            var wayback = WayBack.CreateWayBack(new DatabaseContext(), DateTime.UtcNow.AddMinutes(-5));
            var oldsam = wayback.DbSetFirst<User>(x => x.Name == "Sammy");
            Assert.AreEqual(0, oldsam.Sent.Count());
        }

        [TestMethod("One to Many Reversal (Preserve Existing) (New Entries)")]
        public void OneToManyReversal_PreserveExisting_NewEntries() {

            sam.Sent.Add(new Message() {
                Recipient = yas,
                Contents = "Do not delete me"
            });
            context.SaveChanges();

            var snapshottime = DateTime.UtcNow;

            for (int i = 0; i < 100; i++) {
                sam.Sent.Add(new Message() {
                    Recipient = yas,
                    Contents = $"Hello World : {i}"
                });
            }
            context.SaveChanges();

            var wayback = WayBack.CreateWayBack(new DatabaseContext(), snapshottime);
            var oldsam = wayback.DbSetFirst<User>(x => x.Name == "Sammy");
            Assert.AreEqual(1, oldsam.Sent.Count());
        }

        [TestMethod("One to Many Reversal (Existing Entries)")]
        public void OneToManyReversal_ExistingEntries() {

            for (int i = 0; i < 2000; i++) {
                sam.Sent.Add(new Message() {
                    //Recipient = yas,
                    Contents = $"Msg : {i}"
                });
            }
            var write_sw = new Stopwatch();
            write_sw.Start();
            context.SaveChanges();
            write_sw.Stop();

            Console.WriteLine($"Write Operation 1 Completed in {write_sw.ElapsedMilliseconds}ms");

            var PreReversalTime = DateTime.UtcNow;
            sam.Sent.Clear();
            write_sw.Restart();
            context.SaveChanges();
            write_sw.Stop();
            Console.WriteLine($"Delete Operation 2 Completed in {write_sw.ElapsedMilliseconds}ms");



            var wayback = WayBack.CreateWayBack(new DatabaseContext(), PreReversalTime);
            var oldsam = wayback.DbSetFirst<User>(x => x.Name == "Sammy");

            var read_sw = new Stopwatch();
            read_sw.Start();

            var x_count = oldsam.Sent.Count();
            var y_count = sam.Sent.Count();
            read_sw.Stop();
            Console.WriteLine($"Read Operation Completed in {read_sw.ElapsedMilliseconds}ms");

            Assert.AreNotEqual(0, x_count);
            Assert.AreEqual(0, y_count);

        }

        [TestMethod("One To Many Relationship Integrity Check")]
        public void OneToManyReversal_RelationshipIntegrityCheck() {

            var message = new Message() {
                Recipient = yas,
                Contents = $"Hello World"
            };
            sam.Sent.Add(message);
            var write_sw = new Stopwatch();
            write_sw.Start();
            context.SaveChanges();
            write_sw.Stop();

            Console.WriteLine($"Write Operation 1 Completed in {write_sw.ElapsedMilliseconds}ms");

            var PreReversalTime = DateTime.UtcNow;

            sam.Sent.Clear();
            context.SaveChanges();

            var read_sw = new Stopwatch();
            read_sw.Start();
            var wayback = WayBack.CreateWayBack(new DatabaseContext(), PreReversalTime);
            var oldsam = wayback.DbSetFirst<User>(x => x.Name == "Sammy");



            Assert.AreNotEqual(0, oldsam.Sent.Count());
            var revmessage = oldsam.Sent.First();
            Assert.IsNotNull(revmessage.Sender);

            read_sw.Stop();
            Console.WriteLine($"Read Operation Completed in {read_sw.ElapsedMilliseconds}ms");
        }

        [TestMethod("Many to Many (New Entries)")]
        public void ManyToManyReversal_NewEntries() {
            var PreReversalTime = DateTime.UtcNow;

            var softwareInterest = new Interest() {
                InterestName = "Software Engineering"
            };
            var gamingInterest = new Interest() {
                InterestName = "Gaming"
            };

            context.Interests.AddRange(gamingInterest, softwareInterest);
            context.SaveChanges();

            sam.Interests.Add(gamingInterest);
            sam.Interests.Add(softwareInterest);
            yas.Interests.Add(gamingInterest);
            context.SaveChanges();


            var wayback = WayBack.CreateWayBack(new DatabaseContext(), PreReversalTime);
            var oldsam = wayback.DbSetFirst<User>(x => x.Name == "Sammy");
            var oldgaming = wayback.DbSetFirst<Interest>(x => x.InterestName == "Gaming");

            Assert.AreEqual(0, oldgaming.Users.Count);
            Assert.AreEqual(0, oldsam.Interests.Count);
            Assert.AreNotEqual(0, sam.Interests.Count);
        }

        [TestMethod("Many to Many (Existing)")]
        public void ManyToManyReversal_ExistingEntries() {
            ManyToManyReversal_NewEntries();
            var PreReversalTime = DateTime.UtcNow;

            sam.Interests.Remove(sam.Interests.First());
            context.SaveChanges();

            var wayback = WayBack.CreateWayBack(new DatabaseContext(), PreReversalTime);
            var oldsam = wayback.DbSetFirst<User>(x => x.Name == "Sammy");
            var oldgaming = wayback.DbSetFirst<Interest>(x => x.InterestName == "Gaming");

            Assert.AreEqual(2, oldgaming.Users.Count);
            Assert.AreEqual(2, oldsam.Interests.Count);
            Assert.AreNotEqual(2, sam.Interests.Count);
        }

        [TestMethod("Many to Many (Extending Existing)")]
        public void ManyToManyReversal_ExtendExisting() {
            ManyToManyReversal_NewEntries();

            var PreReversalTime = DateTime.UtcNow;

            var sports_interest = new Interest() { InterestName = "Sports" };
            context.Interests.Add(sports_interest);
            context.SaveChanges();

            sam.Interests.Add(sports_interest);

            context.SaveChanges();


            var wayback = WayBack.CreateWayBack(new DatabaseContext(), PreReversalTime);
            var oldsam = wayback.DbSetFirst<User>(x => x.Name == "Sammy");

            Assert.AreEqual(2, oldsam.Interests.Count);
            Assert.AreEqual(3, sam.Interests.Count);
        }
        [TestMethod("Many to Many (Dual Jump Back)")]
        public void ManyToManyReversal_ExtendExistingJumpBack() {

            var PreReversalTime = DateTime.UtcNow;
            ManyToManyReversal_NewEntries();


            var sports_interest = new Interest() { InterestName = "Sports" };
            context.Interests.Add(sports_interest);
            context.SaveChanges();

            sam.Interests.Add(sports_interest);
            context.SaveChanges();


            var wayback = WayBack.CreateWayBack(new DatabaseContext(), PreReversalTime);
            var oldsam = wayback.DbSetFirst<User>(x => x.Name == "Sammy");

            Assert.AreEqual(0, oldsam.Interests.Count);
            Assert.AreEqual(3, sam.Interests.Count);
        }

        [TestMethod("Combined Set 1")]
        public void CombinedChanges1() {


            sam.BestFriend = yas;
            yas.BestFriend = jim;
            jim.BestFriend = sam;

            jim.Name = "Jamothy";
            context.SaveChanges();

            jim.Name = "James";
            sam.Name = "Sam";
            yas.Name = "Yasmin";
            context.SaveChanges();

            var PreReversalTime = DateTime.UtcNow;

            sam.BestFriend = null;
            yas.BestFriend = null;
            jim.BestFriend = null;

            context.SaveChanges();

            jim.Name = "Jim";
            sam.Name = "Sammy";
            yas.Name = "Yas";
            context.SaveChanges();



            var wayback = WayBack.CreateWayBack(new DatabaseContext(), PreReversalTime);
            var oldsam = wayback.DbSetFirst<User>(x => x.Name == "Sammy");
            Assert.AreEqual("James", oldsam.BestFriend?.BestFriend?.Name);
            Assert.AreEqual("Sam", oldsam.BestFriend?.BestFriend?.BestFriend?.Name);

        }

        [TestMethod("Combined Set 2")]
        public void CombinedChanges2() {

            var codingInterest = new Interest() { InterestName = "Software Development" };
            context.Interests.Add(codingInterest);
            context.SaveChanges();

            codingInterest.Users.AddRange(new[] { sam, yas, jim });

            sam.BestFriend = yas;
            yas.BestFriend = jim;
            jim.BestFriend = sam;

            jim.Name = "James";
            sam.Name = "Sam";
            yas.Name = "Yasmin";
            context.SaveChanges();

            var PreReversalTime = DateTime.UtcNow;

            sam.BestFriend = null;
            yas.BestFriend = null;
            jim.BestFriend = null;

            context.SaveChanges();

            jim.Name = "Jim";
            sam.Name = "Sammy";
            yas.Name = "Yas";
            context.SaveChanges();



            var wayback = WayBack.CreateWayBack(new DatabaseContext(), PreReversalTime);
            var oldsam = wayback.DbSetFirst<User>(x => x.Name == "Sammy");
            var oldCodingIntestest = wayback.DbSetFirst<Interest>(x => x.InterestName == "Software Development");

            Assert.AreEqual("James", oldsam.BestFriend?.BestFriend?.Name);
            Assert.AreEqual("Sam", oldsam.BestFriend?.BestFriend?.BestFriend?.Name);
            Assert.AreEqual(3, oldCodingIntestest.Users.Count);
            Assert.IsTrue(oldCodingIntestest.Users.Contains(oldsam));

        }

        [TestMethod("Soft Deletion")]
        public void SoftDelete() {
            var message = new Message() {
                Contents = "This is a deleted message",
                Recipient = yas
            };
            sam.Sent.Add(message);
            context.SaveChanges();

            context.Messages.Remove(message);
            context.SaveChanges();


            // Emulate resetting a collection
            context.Entry(sam).Collection(x => x.Sent).Reload();

            Assert.AreEqual(0, sam.Sent.Count());
            Assert.AreEqual(0, context.Messages.Count());
            Assert.AreEqual(1, context.Messages.IgnoreQueryFilters().Count());
        }

        [TestMethod("Single Transaction Insertions")]
        public void SingleCallInsertions() {
            var dave = new User("Dave");

            dave.Sent.Add(new Message() {
                Contents = "Hello Sam",
                Recipient = sam
            });
            dave.Inbox.Add(new Message() {
                Contents = "Hi Dave",
                Sender = sam
            });
            dave.Sent.Add(new Message() {
                Contents = "How are you?",
                Recipient = sam
            });
            dave.Inbox.Add(new Message() {
                Contents = "I am good",
                Sender = sam
            });
            context.Users.Add(dave);
            context.SaveChanges();
        }

        [TestMethod("Soft Delete Reversal")]
        public void SoftDeleteReversal() {
            var message = new Message() {
                Contents = "This is a deleted message",
                Recipient = yas
            };
            sam.Sent.Add(message);
            context.SaveChanges();

            var revertPoint = DateTime.UtcNow;

            context.Messages.Remove(message);
            context.SaveChanges();


            var wayback = WaybackMachine.WayBack.CreateWayBack(new DatabaseContext(), revertPoint);


            var oldSam = wayback.DbSetFirst<User>(x => x.Name == "Sammy");
            Assert.AreEqual(1, oldSam.Sent.Count());
        }
    }
}