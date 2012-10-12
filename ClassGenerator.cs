using System;
using System.Collections.Generic;
using System.Text;
using JS2Haxe.Entities;

namespace JS2Haxe
{
    public class ClassGenerator
    {
        public static void Generate(Member member)
        {
            List<Class> classes = new List<Class>();
            Parse(member, classes);

            int i = 0;
        }

        private static void Parse(Member member, List<Class> classes)
        {
            if (!member.Visited)
            {
                member.Visited = true;
                if (member.Type != MemberType.Global)
                {
                    if (member.Members.Count != 0)
                    {
                        // it has members, so its probably a class
                        classes.Add(new Class() { Name = member.GetOfficialName() });
                    }


                    /*foreach (var mem in member.Members.Values)
                    {
                        Parse(mem, classes);
                    }*/
                }


                foreach (var mem in member.Body.Values)
                {
                    Parse(mem, classes);
                }
            }
        }
    }
}
