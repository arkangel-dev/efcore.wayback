using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaybackMachine {
    public interface IWaybackSoftDeletable {
        public DateTime? DeleteDate { get; set; }
    }
}
