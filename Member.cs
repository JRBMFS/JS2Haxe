using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using JS2Haxe.Entities;

namespace JS2Haxe
{
    public enum DataType { Float, Int, String, Bool, Dynamic, Date,Void,  Null, Reference };
    public enum MemberType { Global, Function, AnonymousFunction, Method, Class, Parameter, Member, Property, Value, Unknown, Regex, Array };

    public class Member
    {
        public Member()
        {
            DataType = JS2Haxe.DataType.Reference;
        }

        public Member(String name, Member parent, MemberType type)
        {
            Name = name;
            OverrideName = "";
            Type = type;
            Parent = parent;

            Body = new SerializableDictionary<string, Member>();
            Members = new SerializableDictionary<string, Member>();
            Parameters = new SerializableDictionary<string, Member>();
            ParametersIndex = new SerializableDictionary<int, string>();
            Assignments = new SerializableDictionary<string, Member>();
            ReturnMembers = new SerializableDictionary<string, Member>();
            DataType = JS2Haxe.DataType.Reference;
        }

        public void AddToFunctionBody(Member member)
        {
            string name = member.FullName;
            if (!Body.ContainsKey(name))
                Body.Add(name, member);
        }

        public void AddToClassMembers(Member member)
        {
            string name = member.FullName;
            if (!Members.ContainsKey(name))
            {
                if (member.Type == MemberType.AnonymousFunction || member.Type == MemberType.Function)
                    member.Type = MemberType.Method;

                Members.Add(name, member);
            }
        }

        [XmlIgnore]
        public Member Parent
        {
            get;
            set;
        }

        public Member Constructor
        {
            get;
            set;
        }

        /// <summary>
        /// This is the children content of the member if it has a statement declaration (a body). Members here make this a function with a context change
        /// </summary>
        public SerializableDictionary<string, Member> Body
        {
            get;
            set;
        }

        // the function parameters to this function if applicable
        public SerializableDictionary<int, string> ParametersIndex
        {
            get;
            set;
        }

        // the function parameters to this function if applicable
        public SerializableDictionary<string, Member> Parameters
        {
            get;
            set;
        }

        /// <summary>
        /// if this member ends up having sub members (a class with members and functions, they will be here). Members here make this a class
        /// </summary>
        public SerializableDictionary<string, Member> Members
        {
            get;
            set;
        }

        /// <summary>
        /// variabels assigned to this member
        /// </summary>
        public SerializableDictionary<string, Member> Assignments
        {
            get;
            set;
        }

        public void AddAssignments(List<Member> assignments)
        {
            foreach (var item in assignments)
            {
                string fullname = item.FullName;
                if (fullname == this.FullName)
                {
                    int i = 0;
                }
                if (!Assignments.ContainsKey(fullname))
                    Assignments.Add(fullname, item);
            }
        }

        /// <summary>
        /// the memebrs returned by the function
        /// </summary>
        public SerializableDictionary<string, Member> ReturnMembers
        {
            get;
            set;
        }

        public void Remove(Member member)
        {
            string fullname = member.FullName;

            if (Body.ContainsKey(fullname))
                Body.Remove(fullname);

            if (Members.ContainsKey(fullname))
                Members.Remove(fullname);
        }

        public void AddReturnMembers(List<Member> members)
        {
            foreach (var item in members)
            {
                string fullname = item.FullName;
                if (!ReturnMembers.ContainsKey(fullname))
                    ReturnMembers.Add(fullname, item);
            }
        }

        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// In case a real name was found later on
        /// </summary>
        public String OverrideName
        {
            get;
            set;
        }

        public string GetOfficialName()
        {
            return (OverrideName == ""?Name:OverrideName);
        }

        public MemberType Type
        {
            get;
            set;
        }

        public DataType DataType
        {
            get;
            set;
        }

        public object BasicData
        {
            get;
            set;
        }
        public Member FindMember(string name)
        {
            Member foundMember = null;
            Member currentMember = this;
            while (currentMember != null)
            {
                string potentialName = currentMember.BuildFullName(name);

                // see if we have it in our body first, the parameters, then if not as a class member
                if (currentMember.Body.ContainsKey(potentialName))
                    return currentMember.Body[potentialName];

                if (currentMember.Parameters.ContainsKey(potentialName))
                    return currentMember.Parameters[potentialName];

                if (currentMember.Members.ContainsKey(potentialName))
                    return currentMember.Members[potentialName];

                currentMember = currentMember.Parent;
            }

            return foundMember;
        }

        [XmlIgnore]
        public string FullName
        {
            get
            {
                string fullname = Name;
                int i = 0;
                Member parentCOntext = Parent;
                while (parentCOntext != null)
                {
                    if (parentCOntext.Name != "")
                        fullname = parentCOntext.Name + "." + fullname;

                    parentCOntext = parentCOntext.Parent;
                    if (i == 5)
                        break;
                }

                return fullname;
            }
        }

        [XmlIgnore]
        public string FullNameReal
        {
            get
            {
                string fullname = Name;
                if (OverrideName != "")
                    fullname = OverrideName;

                int i = 0;
                Member parentCOntext = Parent;
                while (parentCOntext != null)
                {
                    if (parentCOntext.Name != "")
                        fullname = parentCOntext.Name + "." + fullname;

                    parentCOntext = parentCOntext.Parent;
                    if (i == 5)
                        break;
                }

                return fullname;
            }
        }

        public string BuildFullName(string elemname)
        {
            string fullname = FullName;

            if (fullname == "")
                return elemname;

            return FullName + "." + elemname;
        }

        public bool Consolidated
        {
            get;
            set;
        }

        public JSType ConsolidatedType
        {
            get;
            set;
        }

        public bool Visited
        {
            get;
            set;
        }
    }
}
