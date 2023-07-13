using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.DbEntities {
    public class Interest {
        [Key]
        public int ID { get; set; }
        public string InterestName { get; set; }
        public virtual List<User> Users { get; set; } = new List<User>();
    }
}
