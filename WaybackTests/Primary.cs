using CastleProxiesTest;
using CastleProxiesTest.DbEntities;
using Microsoft.EntityFrameworkCore;

namespace WaybackTests {
    [TestClass]
    public class Primary {

        private User sam;
        private User yas;
        private User jim;
        private DatabaseContext context;

        public Primary() {
            sam = null;
            yas = null;
            jim = null;
        }

        [TestInitialize]
        public void Setup() {

            context = new DatabaseContext();
            context.Database.EnsureCreated();
            context.Messages.ExecuteDelete();
            context.Users.ExecuteDelete();
            context.AuditEntries.ExecuteDelete();
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
            context.SaveChanges();


            var wayback = WayBack.CreateWayBack(new DatabaseContext(), DateTime.Now.AddMinutes(-5));
            var oldsam = wayback.DbSetFirst<User>(x => x.Name == "John");

            Assert.AreEqual("Sammy", oldsam.Name);
        }

        [TestMethod]
        public void SingleReferenceReversal() {
            sam.BestFriend = yas;
            context.SaveChanges();


            var wayback = WayBack.CreateWayBack(new DatabaseContext(), DateTime.Now.AddMinutes(-5));
            var oldsam = wayback.DbSetFirst<User>(x => x.BestFriend == null);

            Assert.IsNull(oldsam.BestFriend);
        }

        [TestMethod("One to Many Reversal (New Entries)")]
        public void OneToManyReversal_NewEntries() {

            for (int i = 0; i < 10; i++) {
                sam.Sent.Add(new Message() {
                    Recipient = yas,
                    Contents = $"Hello World : {i}"
                });
            }
            context.SaveChanges();

            var wayback = WayBack.CreateWayBack(new DatabaseContext(), DateTime.Now.AddMinutes(-5));
            var oldsam = wayback.DbSetFirst<User>(x => x.Name == "Sammy");
            Assert.AreEqual(0, oldsam.Sent.Count());
        }

        [TestMethod("One to Many Reversal (Existing Entries)")]
        public void OneToManyReversal_ExistingEntries() {
            OneToManyReversal_NewEntries();
            var PreReversalTime = DateTime.Now;
            sam.Sent.Clear();
            context.SaveChanges();

            var wayback = WayBack.CreateWayBack(new DatabaseContext(), PreReversalTime);
            var oldsam = wayback.DbSetFirst<User>(x => x.Name == "Sammy");
            Assert.AreNotEqual(0, oldsam.Sent.Count());
            Assert.AreEqual(0, sam.Sent.Count());
        }

        [TestMethod("Many to Many (New Entries)")]
        public void ManyToManyReversal_NewEntries() {
            var PreReversalTime = DateTime.Now;

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
            var PreReversalTime = DateTime.Now;

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

            var PreReversalTime = DateTime.Now;

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

            var PreReversalTime = DateTime.Now;
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

            var PreReversalTime = DateTime.Now;

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

            jim.Name = "Jamothy";
            context.SaveChanges();

            jim.Name = "James";
            sam.Name = "Sam";
            yas.Name = "Yasmin";
            context.SaveChanges();

            var PreReversalTime = DateTime.Now;

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
    }
}