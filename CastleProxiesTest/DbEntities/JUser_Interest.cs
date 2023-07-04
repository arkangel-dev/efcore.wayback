using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WaybackMachine;
using WaybackMachine.FilterAttributes;

namespace CastleProxiesTest.DbEntities {
    [JunctionTable]
    [SoftDelete]
    public class JUser_Interest {
        [Key]
        public int ID { get; set; }
        public int UserID { get; set; }
        public virtual User User { get; set; }
        public int InterestID { get; set; }
        public virtual Interest Interest { get; set; }
        public bool IsDeleted { get; set; }
    }
}
