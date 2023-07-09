using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WaybackMachine.Entities;

namespace WaybackMachine.Entities {
    public class AuditRecord {
        [Key]
        public int ID { get; set; }
        public string EntityID { get; set; }
        public string TableName { get; set; }
        public string? PropertyName { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        public string? J1Table { get; set; }
        public string? J1 { get; set; }
        public string? J2Table { get; set; }
        public string? J2 { get; set; }
        public AuditEntryType ChangeType { get; set; }
        public virtual AuditTransactionRecord ParentTransaction { get; set; }

        public object GetJunctionKeyForTable(string s, Type keyType) {
            if (s == J2Table) return J2.Deserialize(keyType);
            if (s == J1Table) return J1.Deserialize(keyType);
            return -1;
        } 
    }

    public enum AuditEntryType {
        PropertyOrReferenceChange,
        CollectionAdd,
        CollectionRemove,
        Created
    }
}
