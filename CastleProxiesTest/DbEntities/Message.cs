using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CastleProxiesTest.DbEntities {
    public class Message {
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
    }
}
