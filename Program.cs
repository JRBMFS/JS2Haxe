using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jint;
using Jint.Expressions;

namespace JS2Haxe
{

    class Program
    {

        //public static Dictionary<String, Member> classes = new Dictionary<string, Member>();

        static void Main(string[] args)
        {


            System.IO.StreamReader sr = new System.IO.StreamReader(@"c:\code\jquerysocket.js");
            string code = sr.ReadToEnd();
            sr.Close();

            var res = JintEngine.Compile(code, true);

            /*Member baseclass = new Member();
            MemberSearch result = new MemberSearch();

            
            foreach (Statement statement in res.Statements)
            {
                result.member = baseclass;
                HandleStatement(statement, result);
            }

            // finally, generate haxe
            HaxeGenerator gen = new HaxeGenerator(classes);

            gen.Generate();*/


        }

       /* private static MemberSearch HandleStatement(Statement statement, MemberSearch currentclass)
        {
            if (statement is VariableDeclarationStatement)
            {
                var vardecstat = (VariableDeclarationStatement)statement;

                Member newclass = null;

                if (currentclass.member != null)
                    newclass = currentclass.member.FindMember(vardecstat.Identifier);

                if (newclass == null)
                {
                    newclass = new Member();
                    newclass.objectype = Member.MemberType.Field;
                    newclass.name = vardecstat.Identifier;
                    newclass.Namespace = currentclass.member.fullname;
                    newclass.parent = currentclass.member;
                    MakeFullname(currentclass.member, newclass);

                    if ((currentclass.member.objectype == Member.MemberType.Method || currentclass.member.objectype == Member.MemberType.None) && currentclass.member.name == "")
                    {
                        if (!classes.ContainsKey(vardecstat.Identifier))
                            classes.Add(vardecstat.Identifier, newclass);
                    }

                    currentclass.member.addMember(newclass);  
                }

                currentclass.member = newclass;

                // ignore empty expression declarations, not really useful to us
                if (vardecstat.Expression != null)
                {
                    HandleExpression(vardecstat.Expression, currentclass);

                }
            }
            else if (statement is ExpressionStatement)
            {
                var exstat = (ExpressionStatement)statement;

                return  HandleExpression(exstat.Expression, currentclass);
            }
            else if (statement is CommaOperatorStatement)
            {
                var comstat = (CommaOperatorStatement)statement;

                foreach (Statement newstatement in comstat.Statements)
                {
                    HandleStatement(newstatement, currentclass.Copy());
                }
                //return HandleExpression(exstat.Expression, currentclass);
            }
            else if (statement is ReturnStatement)
            {
                var retstate = (ReturnStatement)statement;

                // we have a return statement, so mark the method as NON void. later we could try to determine method type by return type...
                //TODO: determin method return type

                Member method = currentclass.member.FindMethod(currentclass.wantedMemebrName);

                if (method != null)
                    method.datatype = "Dynamic";

                return HandleExpression(retstate.Expression, currentclass);
            }
            else
            {
                int i = 0;
            }

            return new MemberSearch();
        }

        private static MemberSearch HandleExpression(Expression expression, MemberSearch currentclass)
        {
            MemberSearch memberResult = new MemberSearch();
            if (expression is MemberExpression)
            {
                // this is an expression that we perform on a member
                var memexpr = (MemberExpression)expression;

                if (!(memexpr.Previous is FunctionExpression))
                {
                    if (memexpr.Member is MethodCall)
                    {
                        memberResult = HandleExpression(memexpr.Previous, currentclass);

                        // this is a function call, we do nothing with it, but we could lookup for data type...
                        //TODO: find method and try to determine datatype
                    }
                    else if (memexpr.Member is PropertyExpression && !(memexpr.Previous is ArrayDeclaration))
                    {
                        SearchInfo result = new SearchInfo();
                        if (((PropertyExpression)memexpr.Member).Text == "prototype")
                        {
                            // we have a prototype, so we go one up
                            if (memexpr.Previous is Identifier)
                                result = GetMember(((Identifier)memexpr.Previous), currentclass.member);
                            else if (memexpr.Previous is MemberExpression)
                                result = GetMember(((MemberExpression)memexpr.Previous).Previous, currentclass.member);
                        }
                        else
                            result = GetMember(memexpr.Previous, currentclass.member);

                        memberResult.isthis = result.isthis;
                        memberResult.isprototype = result.isprototype;
                        memberResult.parent = result.foundclass;
                        memberResult.wanteParentName = result.searchedName;
                        memberResult.wantedMemebrName = ((PropertyExpression)memexpr.Member).Text;
                        if (((PropertyExpression)memexpr.Member).Text == "prototype")
                        {
                            if (memexpr.Previous is Identifier)
                                memberResult.wantedMemebrName = ((Identifier)memexpr.Previous).Text;
                            else if (memexpr.Previous is MemberExpression)
                                memberResult.wantedMemebrName = ((PropertyExpression)((MemberExpression)memexpr.Previous).Member).Text;
                            if (memberResult.parent != null)
                                memberResult.member = memberResult.parent.FindMember(memberResult.wantedMemebrName);

                        }
                        else if (memberResult.parent != null)
                            memberResult = HandleExpression(memexpr.Member, memberResult);
                    }
                }
                else
                {
                    memberResult = HandleExpression(memexpr.Previous, currentclass);
                }
            }
            else if (expression is MethodCall)
            {
                // we dont do much with method calls, but we could try to figure out data type
                // so we will, we find the method
                memberResult = currentclass;
                memberResult.wantedMemebrName = ((MethodCall)expression).Label;
                memberResult.member = currentclass.parent.FindMethod(memberResult.wantedMemebrName);
            }
            else if (expression is PropertyExpression)
            {
                // we dont do much with method calls, but we could try to figure out data type
                // so we will, we find the method

                memberResult = currentclass;
                memberResult.wantedMemebrName = ((PropertyExpression)expression).Text;
                memberResult.member = currentclass.parent.FindField(memberResult.wantedMemebrName);
            }
            else if (expression is Identifier)
            {
                // we dont do much with method calls, but we could try to figure out data type
                // so we will, we find the method
                Identifier id = expression as Identifier;

                Member foundMember = null;

                if( currentclass.parent != null)
                    foundMember = currentclass.parent.FindMember(id.Text);
                else
                    foundMember = currentclass.member.FindMember(id.Text);

                memberResult = currentclass;
                memberResult.member = foundMember;
            }
            else if (expression is AssignmentExpression)
            {
                var assignex = (AssignmentExpression)expression;

                memberResult = HandleExpression(assignex.Left, currentclass);

                if (memberResult == null || memberResult.parent == null)
                    return null;

                Member left = memberResult.member;

                if (memberResult.isprototype) {
                    // we have a prototype, so this is a class
                    memberResult.parent.changeMethodIntoClass();
                }

                if (left == null)
                {
                    Member newmember = new Member();
                    newmember.parent = memberResult.parent;
                    newmember.name = memberResult.wantedMemebrName;
                    newmember.Namespace = memberResult.parent.fullname;

                    MakeFullname(memberResult.parent, newmember);

                    memberResult.member = newmember;
                    memberResult.mustCreateMember = true;

                    memberResult.parent.addMember(newmember);
                }

                HandleExpression(assignex.Right, memberResult);
            }
            else if (expression is TernaryExpression)
            {
                // do nothing really...
                currentclass.member.objectype = Member.MemberType.Field;
            }
            else if (expression is FunctionExpression)
            {
                FunctionExpression funcex = expression as FunctionExpression;


                 currentclass.member.objectype = Member.MemberType.Method;
                 currentclass.member.methodtype = Member.MethodType.Dynamic;

                // its a prototype method
                 if (currentclass.isprototype)
                     currentclass.member.methodtype = Member.MethodType.Prototype;

                 currentclass.member.functionParameters.Add(funcex.Parameters.ToList());

                 if (funcex.Statement is BlockStatement)
                 {
                     BlockStatement block = funcex.Statement as BlockStatement;

                     foreach (var statement in block.Statements)
                     {
                         MemberSearch statementresult = HandleStatement(statement, currentclass.Copy());

                         if (statementresult != null && statementresult.isthis)
                         {
                             // we modified ourselves, so this is not a function but a class, so we change it
                             statementresult.parent.changeMethodIntoClass();
                         }
                     }
                 }
            }
            else if (expression is JsonExpression)
            {
                // here we declare members via prototype form
                JsonExpression json = expression as JsonExpression;
                currentclass.member.changeMethodIntoClass();
                foreach (var entry in json.Values.Values)
                {
                    HandleExpression(entry, currentclass);
                }
            }
            else if (expression is PropertyDeclarationExpression)
            {
                var assignex = (PropertyDeclarationExpression)expression;

                if (assignex.Name == "constructor")
                {
                    MemberSearch search = HandleExpression(assignex.Expression, currentclass.Copy());

                    if (search.member != null)
                    {
                        if (search.member.objectype == Member.MemberType.Class)
                        {
                            currentclass.member.CopyConstructors(search.member);
                        }
                    }
                }
                else
                {
                    Member foundmethod = currentclass.member.FindMember(assignex.Name);

                    if(foundmethod == null)
                    {
                        currentclass.member.changeMethodIntoClass();
                        Member newmember = new Member();
                        newmember.parent = currentclass.member;
                        newmember.name = assignex.Name;
                        newmember.Namespace = newmember.parent.fullname;

                        MakeFullname(newmember.parent, newmember);

                        memberResult.isprototype = true;
                        memberResult.parent = newmember.parent;
                        memberResult.member = newmember;
                        memberResult.mustCreateMember = true;

                        newmember.parent.addMember(newmember);

                        HandleExpression(assignex.Expression, memberResult);
                    }
                }

            }
            else if (expression is ValueExpression)
            {
                currentclass.member.objectype = Member.MemberType.Field;

                ValueExpression value = expression as ValueExpression;
                string type = "";
                // try to guess the type of a member
                switch (value.TypeCode)
                {
                    case TypeCode.Double:
                        type = "Float";
                        break;
                    case TypeCode.String:
                        type = "String";
                        break;
                    case TypeCode.Boolean:
                        type = "Bool";
                        break;
                    case TypeCode.Byte:
                        type = "Int";
                        break;
                    case TypeCode.Char:
                        type = "String";
                        break;
                    case TypeCode.DateTime:
                        type = "Date";
                        break;
                    case TypeCode.Decimal:
                        type = "Float";
                        break;
                    case TypeCode.Int16:
                        type = "Int";
                        break;
                    case TypeCode.Int32:
                        type = "Int";
                        break;
                    case TypeCode.Int64:
                        type = "Int";
                        break;
                    case TypeCode.Single:
                        type = "Float";
                        break;
 
                }

                if (type != "")
                    currentclass.member.datatype = type;
            }
            else
            {
                return null;
            }

            return memberResult;
        }

        public static SearchInfo GetMember(Expression ex, Member currentclass)
        {
            SearchInfo info = new SearchInfo();

            if (!(ex is MemberExpression || ex is Identifier))
                return info;

            string name = "";
            // get the full name by going through name hierarchy
            List<String> values = new List<string>();
            while (!(ex is Identifier))
            {
                if (!(ex is MemberExpression || ex is Identifier))
                    return info;

                if(!(((MemberExpression)ex).Member is PropertyExpression))
                    return info;
                string label = ((PropertyExpression)((MemberExpression)ex).Member).Text;
                if (label != "prototype")
                    values.Add(label);
                else
                    info.isprototype = true;

                ex = ((MemberExpression)ex).Previous;
            }

            name = ((Identifier)ex).Text;

            if (name == "this")
            {
                info.isthis = true;
                info.foundclass = currentclass;
            }
            else
            {

                values.Add(name);
                values.Reverse();

                string searchname = string.Join(".", values.ToArray());
                info.searchedName = searchname;

                if (classes.ContainsKey(searchname))
                    info.foundclass = classes[searchname];
            }

            return info;
        }

        public static CurrentInfo GetInfo(Expression ex, Member currentclass)
        {

            CurrentInfo info = new CurrentInfo();
            string name = "";
            // get the full name by going through name hierarchy
            List<String> values = new List<string>();
            int i = 0;
            bool prototypefirst = false;
            while(!(ex is Identifier)){
                string label = ((PropertyExpression)((MemberExpression)ex).Member).Text;
                if (label != "prototype")
                    values.Add(label);
                else
                {
                    info.isprototype = true;
                    if (i == 0)
                        prototypefirst = true;
                }

                ex = ((MemberExpression)ex).Previous;
                i++;
            }

            if (prototypefirst)
            {

            }

            name = ((Identifier)ex).Text;

            if (name == "this")
            {
                info.isthis = true;
                info.parent = currentclass;
            }
            else
            {

                values.Add(name);
                values.Reverse();

                if (classes.ContainsKey(string.Join(".", values.ToArray())))
                    info.parent = classes[string.Join(".", values.ToArray())];
            }

            return info;
        }

        public static void MakeFullname(Member parent, Member newclass)
        {
            if (parent.fullname != "")
                newclass.fullname = string.Format("{0}.{1}", parent.fullname, newclass.name);
            else
                newclass.fullname = newclass.name;
        }*/
    }
}
