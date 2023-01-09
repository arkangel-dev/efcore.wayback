using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaybackMachine.FilterAttributes {
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = true)]
    public class DoNotAudit : Attribute { }

    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class CensorAudit : Attribute { }

    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class JunctionTable : Attribute { }

    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class Audit : Attribute { }

    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class SoftDelete : Attribute { }
}
