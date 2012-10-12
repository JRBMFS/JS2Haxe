using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace JS2Haxe.Entities
{
    [XmlInclude(typeof(BasicType))]
    [XmlInclude(typeof(Class))]
    public class JSType
    {
        public virtual string GetName()
        {
            return "";
        }
    }
}
