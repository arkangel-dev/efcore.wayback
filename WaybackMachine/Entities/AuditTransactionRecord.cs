using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaybackMachine.Entities {
    public class AuditTransactionRecord {
        [Key]
        public int ID { get; set; }
        public Guid TransactionID { get; set; }
        public DateTime ChangeDate { get; set; }
        public virtual List<AuditRecord> Changes { get; set; } = new List<AuditRecord>();
    }
}
