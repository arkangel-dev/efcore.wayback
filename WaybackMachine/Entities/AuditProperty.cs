using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaybackMachine.Entities {
    public class AuditProperty {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public virtual AuditTable ParentTable { get; set; }
    }
}
