using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CastleProxiesTest.DbEntities {
    public class AuditEntry {
        [Key]
        public int ID { get; set; }
        public int EntityID { get; set; }
        public string TableName { get; set; }
        public string TransactionID { get; set; }
        public string PropertyName { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime ChangeDate { get; set; }
        public AuditEntryType ChangeType { get; set; }
    }

    public enum AuditEntryType {
        PropertyOrReferenceChange,
        CollectionAdd,
        CollectionRemove
    }
}
