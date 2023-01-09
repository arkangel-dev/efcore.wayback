using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WaybackMachine.Entities;

namespace WaybackMachine {
    public interface IWaybackContext {

        DbContext InternalDbContext { get; }
        WaybackConfig WaybackConfiguration { get; set; } 
        

        int BaseSaveChanges();

    
    }
}
