using Sample.DbEntities;
using Sample;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using WaybackMachine;

namespace WaybackMachineTests {
    [TestClass]
    public class ReadSpeedTests {
        private User sam;
        private User yas;
        private User jim;
        private DatabaseContext context;

        [TestInitialize]
        public void Setup() {

            context = new DatabaseContext();
            context.Database.EnsureCreated();

            context.Junction_Interests_Users.Where(x => true).ExecuteDelete();
            context.Messages.IgnoreQueryFilters().ExecuteDelete();
            context.Users.Where(x => true).ExecuteDelete();
            context.Interests.Where(x => true).ExecuteDelete();
            context.SaveChanges();

            // Create the users
            sam = new User("Sammy");
            yas = new User("Yas");
            jim = new User("Jimmy");

            // Add the users to the database
            context.Users.AddRange(sam, yas, jim);
            context.SaveChanges();
        }

        [TestMethod("Direct Nav Property : Null : 10000 cycles")]
        public void TestReadSpeedNull() {
            sam.BestFriend = null;
            context.SaveChanges();
            var wayback = WayBack.CreateWayBack(new DatabaseContext(), DateTime.UtcNow.AddMinutes(-5));
            var oldsam = wayback.DbSetFirst<User>(x => x.Name == "Sammy");

            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 10000; i++) {
                var x = oldsam.BestFriend;
            }
            sw.Stop();
            Console.WriteLine($"Read Cycle Completed in {sw.ElapsedMilliseconds}ms");
        }

        [TestMethod("Direct Nav Property : Not Null : 10000 cycles")]
        public void TestReadSpeed() {
            sam.BestFriend = yas;
            context.SaveChanges();
            var wayback = WayBack.CreateWayBack(new DatabaseContext(), DateTime.UtcNow.AddMinutes(-5));
            var oldsam = wayback.DbSetFirst<User>(x => x.Name == "Sammy");
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 10000; i++) {
                var x = oldsam.BestFriend;
            }
            sw.Stop();
            Console.WriteLine($"Read Cycle Completed in {sw.ElapsedMilliseconds}ms");
        }


        [TestMethod("Many To Many Col Property : Null : 10000 cycles")]
        public void ManyToManyReadSpeedNull() {
            var wayback = WayBack.CreateWayBack(new DatabaseContext(), DateTime.UtcNow.AddMinutes(-5));
            var oldsam = wayback.DbSetFirst<User>(x => x.Name == "Sammy");
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 10000; i++) {
                var x = oldsam.Interests;
            }
            sw.Stop();
            Console.WriteLine($"Read Cycle Completed in {sw.ElapsedMilliseconds}ms");
        }

        [TestMethod("Many To Many Col Property : Not Null : 10000 cycles")]
        public void ManyToManyReadSpeedNotNull() {


            var interests = new List<Interest>();
            for (int i = 0; i < 1000; i++) {
                var _int = new Interest() {
                    InterestName = Guid.NewGuid().ToString()
                };
                context.Interests.Add(_int);
            }
            context.SaveChanges();
            sam.Interests.AddRange(interests);
            context.SaveChanges();

            var wayback = WayBack.CreateWayBack(new DatabaseContext(), DateTime.UtcNow.AddMinutes(-5));
            var oldsam = wayback.DbSetFirst<User>(x => x.Name == "Sammy");
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 10000; i++) {
                var x = oldsam.Interests;
            }
            sw.Stop();
            Console.WriteLine($"Read Cycle Completed in {sw.ElapsedMilliseconds}ms");
        }

        
        [TestMethod("Base Line Save Operation (2000 Records)")]
        public void BaseLineTest() {
            for (int i = 0; i < 2000; i++) {
                sam.Sent.Add(new Message() {
                    Recipient = yas,
                    Contents = $"Hello World : {i}"
                });
            }
            var write_sw = new Stopwatch();
            write_sw.Start();
            context.BaseSaveChanges();
            write_sw.Stop();

            Console.WriteLine($"Write Operation Completed in {write_sw.ElapsedMilliseconds}ms");
        }
    }
}
