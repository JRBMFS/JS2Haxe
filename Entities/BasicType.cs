using System;
using System.Collections.Generic;
using System.Text;

namespace JS2Haxe.Entities
{
    public class BasicType : JSType
    {
        public BasicType()
        {
            Type = DataType.Dynamic;
        }

        public override string GetName()
        {
            return Type.ToString();
        }

        public DataType Type
        {
            get;
            set;
        }
    }
}
