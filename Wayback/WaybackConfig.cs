using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WaybackMachine {
    public class WaybackConfig {
        public TrackingModes TrackingMode { get; set; } = TrackingModes.All;
        public Dictionary<Type, PropertyInfo> PropertyPrimaryFieldTrackingCache { get; set; } = new Dictionary<Type, PropertyInfo>(); 
        public enum TrackingModes {
            /// <summary>
            /// Everything is tracked
            /// </summary>
            All = 1,

            /// <summary>
            /// Classes with the audit attribute will be selected
            /// </summary>
            ExplicitClasses = 2,

            /// <summary>
            /// Properties with the audit attribute will be added
            /// </summary>
            ExplicitProperties = 4
        }
    }
}
