using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.DbEntities {
    [Table("UserL")]
    public class User {

        public User() {
            Sent = new List<Message>();
            Inbox = new List<Message>();
            Interests = new List<Interest>();
            Name = string.Empty;
        }

        public User(string name) {
            Sent = new List<Message>();
            Inbox = new List<Message>();
            Interests = new List<Interest>();
            Name = name;
        }

        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public virtual List<Message> Sent { get; set; }
        public virtual List<Message> Inbox { get; set; }
        public virtual User? BestFriend { get; set; }
        public virtual List<Interest> Interests { get; set; }
    }
}
