using System;
using System.Collections.Generic;
using System.Text;

namespace JS2Haxe.Entities
{
    public class Method
    {
        public Method()
        {
            Parameters = new List<Parameter>();
            ReturnType = "void";
        }

        public string Name
        {
            get;
            set;
        }

        public List<Parameter> Parameters
        {
            get;
            set;
        }

        public string ReturnType
        {
            get;
            set;
        }
    }
}
