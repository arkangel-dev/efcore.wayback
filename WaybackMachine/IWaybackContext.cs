using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WaybackMachine.Entities;

namespace WaybackMachine {
    public interface IWaybackContext {
        DbSet<AuditRecord> AuditEntries { get; set; }
        DbSet<AuditTransactionRecord> AuditTransactions { get; set; }
        DbSet<AuditTable> AuditTables { get; set; }
        DbSet<AuditProperty> AuditProperties { get; set; }
        DbContext InternalDbContext { get; }
        WaybackConfig WaybackConfiguration { get; set; } 
        
        List<AuditTable> _tempAuditTables { get; set; }
        List<AuditProperty> _tempAuditProperties { get; set; }

        int BaseSaveChanges();

    
    }
}
