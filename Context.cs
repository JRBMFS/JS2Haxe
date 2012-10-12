using System;
using System.Collections.Generic;
using System.Text;

namespace JS2Haxe
{

    public class Context
    {
        public Context()
        {
            Parent = null;
        }

        public Context Parent
        {
            get;
            set;
        }

        public Member Member
        {
            get;
            set;
        }

        public bool IsPrototype
        {
            get;
            set;
        }

        // if true, members not found will be created, else ignored
        public bool AllowMemberCreation
        {
            get;
            set;
        }
    }
}
