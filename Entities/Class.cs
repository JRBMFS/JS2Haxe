using System;
using System.Collections.Generic;
using System.Text;

namespace JS2Haxe.Entities
{
    public class Class : JSType
    {
        public Class()
        {
            Methods = new SerializableDictionary<string, Method>();
            Members = new SerializableDictionary<string, CMember>();
            Constructors = new List<Method>();
        }

        public string Name
        {
            get;
            set;
        }

        public SerializableDictionary<String, Method> Methods
        {
            get;
            set;
        }

        public List<Method> Constructors
        {
            get;
            set;
        }

        public SerializableDictionary<String, CMember> Members
        {
            get;
            set;
        }

        public override string GetName()
        {
            return Name;
        }
    }
}
