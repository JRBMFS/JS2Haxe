using System;
using System.Collections.Generic;
using System.Text;
using JS2Haxe.Entities;

namespace JS2Haxe
{
    public class Consolidator
    {
        public static List<Class> classes = new List<Class>();

        public static void Consolidate(Member member)
        {
            Parse(member);
        }

        private static void Parse(Member member)
        {
            if (!member.Consolidated)
            {
                member.Consolidated = true;
                JSType type = null;

                // parse the assignments
                foreach (var mem in member.Assignments.Values)
                {
                    if (!mem.Consolidated)
                    {
                        Parse(mem);
                    }

                    if (type == null)
                        type = mem.ConsolidatedType;
                    else
                    {
                        // here we must dfecide which type has priority if they are different
                        if (mem.ConsolidatedType != null && type.GetName() != mem.ConsolidatedType.GetName())
                        {
                            type = new BasicType() { Type = DataType.Dynamic };
                        }
                    }
                }
                // done parsing children, lets create this type

                if (type == null && member.Type != MemberType.Global && member.Type != MemberType.Parameter && member.Type != MemberType.AnonymousFunction && member.Type != MemberType.Value && member.Type != MemberType.Method && member.Type != MemberType.Function)
                {
                    // nothing was assigned to this type, so it IS the base type, lets create it
                    type = new Class() { Name = member.FullNameReal };
                    classes.Add((Class)type);
                }
                else if (type == null && member.Type == MemberType.Value)
                {
                    // nothing was assigned to this type, so it IS the base type, lets create it
                    type = new BasicType() { Type = member.DataType };
                }

                // and now children types
                if (member.Constructor != null)
                {
                    Parse(member.Constructor);

                    if (type != null && type is Class)
                        ((Class)type).Constructors.Add(GenerateMethod(member.Constructor));
                }

                foreach (var mem in member.Parameters.Values)
                {
                    Parse(mem);
                }


                foreach (var mem in member.Members.Values)
                {
                    Parse(mem);

                    if (type != null && type.GetType() != typeof(BasicType))
                    {
                        if (mem.Type == MemberType.Function || mem.Type == MemberType.Method)
                        {
                            Method meth = GenerateMethod(mem);
                            if (!((Class)type).Methods.ContainsKey(meth.Name))
                                ((Class)type).Methods.Add(meth.Name, meth);
                        }
                        else if (mem.Type == MemberType.Member && mem.ConsolidatedType != null)
                        {
                            CMember memb = new CMember() { Name = mem.GetOfficialName(), Type = mem.ConsolidatedType.GetName() };

                            if (!((Class)type).Members.ContainsKey(memb.Name))
                                ((Class)type).Members.Add(memb.Name, memb);
                        }
                    }
                }
                


                foreach (var mem in member.Body.Values)
                {
                    Parse(mem);
                }
                

                

                foreach (var mem in member.ReturnMembers.Values)
                {
                    Parse(mem);

                    type = mem.ConsolidatedType;
                }

                member.ConsolidatedType = type;

                
            }
        }

        private static Method GenerateMethod(Member member)
        {
            Method meth = new Method() { Name = member.GetOfficialName() };

            if (member.ConsolidatedType != null)
            {
                meth.ReturnType = member.ConsolidatedType.GetName();
            }

            foreach (var para in member.Parameters)
            {
                Parameter par = par = new Parameter(){Name = para.Value.GetOfficialName()};
                if (para.Value.DataType == DataType.Reference)
                {
                    par.Type = GetType(para.Value.Assignments).GetName();
                }

                meth.Parameters.Add(par);
            }

            return meth;
        }

        private static JSType GetType(SerializableDictionary<string, Member> members)
        {
            JSType type = new BasicType();

            foreach (var mem in members.Values)
            {
                if (mem.DataType == DataType.Reference)
                {
                    if (mem.ConsolidatedType != null)
                        type = mem.ConsolidatedType;
                }
            }

            return type;
        }
    }
}
