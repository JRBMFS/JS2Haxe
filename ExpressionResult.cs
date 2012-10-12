using System;
using System.Collections.Generic;
using System.Text;

namespace JS2Haxe
{
    class ExpressionResult
    {
        public ExpressionResult()
        {
            Members = new List<Member>();
        }

        public void AddMembers(List<Member> members)
        {
            foreach (var item in members)
            {
                Members.Add(item);
            }
        }

        public List<Member> Members
        {
            get;
            set;
        }

        public Member SingleMember
        {
            get
            {
                if (Members.Count == 0)
                    return null;

                return Members[0];
            }
        }

        public bool IsPrototype
        {
            get;
            set;
        }

        public bool IsFunctionDecl
        {
            get;
            set;
        }
    }
}
