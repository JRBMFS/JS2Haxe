using System;
using System.Collections.Generic;
using System.Text;
using Jint;
using Jint.Expressions;
using System.Xml.Serialization;
using System.IO;

namespace JS2Haxe
{

    class Program
    {
        // the global registry of variables in their own context
        //static Dictionary<string, Assignment> variables = new Dictionary<string, Assignment>();
        static int AnoymCounter = 1;
        static Dictionary<Statement, Member> anonymous = new Dictionary<Statement, Member>();
        static void Main(string[] args)
        {

            Member baseMember = new Member("", null, MemberType.Global);

           // System.IO.StreamReader sr = new System.IO.StreamReader(@"c:\code\JS2Haxe\jquery-1.7.1.js");
            System.IO.StreamReader sr = new System.IO.StreamReader(@"c:\code\JS2Haxe\kendo.all.min.js");

            string code = sr.ReadToEnd();
            sr.Close();

            Jint.Expressions.Program res = JintEngine.Compile(code, true);

            AnoymCounter = 1;

            // first pass, we build a list of all variable declarations without any assignment information
            foreach (Statement statement in res.Statements)
            {
                HandleDeclarationStatement(statement, baseMember, 0);
            }

            // now that we have our declarations we loop again and gather the assignment hierarchy to see what is assigned to who
            foreach (Statement statement in res.Statements)
            {
               HandleAssignmentStatement(statement, baseMember, 0);
            }

            // now that we have our code analysis, we rebuild the classes and their datatypes by analysing assignments of variables
            // note: some objects are assigned the same object so we must merge their method and members declaration (ex: jQuery.fn)

            Consolidator.Consolidate(baseMember);

            XmlSerializer serializer = new XmlSerializer(Consolidator.classes.GetType());
            TextWriter textWriter = new StreamWriter(@"C:\temp\movie.xml");
            serializer.Serialize(textWriter, Consolidator.classes);
            textWriter.Close();

            /*ClassGenerator.Generate(baseMember);

            System.IO.StreamWriter sw = new System.IO.StreamWriter(@"c:\code\tt.txt");

            WriteMember(baseMember, sw, 0);
            sw.Close();*/
            /*XmlSerializer serializer = new XmlSerializer(typeof(Member));
            TextWriter textWriter = new StreamWriter(@"C:\temp\movie.xml");
            serializer.Serialize(textWriter, baseMember);
            textWriter.Close();*/

            #region Code
          
            /* List<Assignment> vars = new List<Assignment>();

            foreach (var r in variables)
            {
                vars.Add(r.Value);
            }

            XmlSerializer serializer = new XmlSerializer(typeof(List<Assignment>));
            TextWriter textWriter = new StreamWriter(@"C:\temp\movie.xml");
            serializer.Serialize(textWriter, vars);
            textWriter.Close();
            
            // now build the class interfaces and deduct types
            foreach (Statement statement in res.Statements)
            {
               // HandleDeclarationStatement(statement);
            }*/
            #endregion
           
        }
          #region temp
        static Dictionary<String, Member> written = new Dictionary<string, Member>();
        public static void WriteMember(Member member, System.IO.StreamWriter sw, int level, string key = "")
        {
            string TAB = "";
            for (int i = 0; i < level; i++)
            {
                TAB += "\t";
            }

            string extra = "";

            if (member.Type == MemberType.Parameter)
                extra = "  (parameter) ";
            else if (member.Type == MemberType.Member)
                extra = "  (member) ";
            else if (member.Type == MemberType.Function)
                extra = "  (function) ";
            else if (member.Type == MemberType.Method)
                extra = "  (Method) ";
            else if (member.Type == MemberType.Value)
                extra = "  (type: " + member.DataType.ToString() + ") ";
            else if (member.Type == MemberType.Regex)
                extra = "  (regex: " + member.BasicData.ToString() + ") ";
            else if (member.Type == MemberType.Property)
                extra = "  (property) ";
            else if (member.Type == MemberType.Array)
                extra = "  (array) ";

            if (written.ContainsKey(member.FullName))
                extra += "(reference) ";

            sw.WriteLine(TAB + member.FullNameReal + extra  + key);

            if (!written.ContainsKey(member.FullName))
            {

                written.Add(member.FullName, member);

                if(member.Constructor != null)
                    sw.WriteLine(TAB + "\t" + member.Constructor.Name + " (constructor)");

                if (member.Body.Count > 0)
                sw.WriteLine(TAB + "<<body values>>");
                foreach (var mem in member.Body.Values)
                {
                    WriteMember(mem, sw, level + 1);
                }

                if (member.Parameters.Count > 0)
                sw.WriteLine(TAB + "<<parameters>>");
                foreach (var mem in member.Parameters.Values)
                {
                    WriteMember(mem, sw, level + 1);
                }

                if (member.Members.Count > 0)
                sw.WriteLine(TAB + "<<class members>>");
                foreach (var mem in member.Members.Values)
                {
                    WriteMember(mem, sw, level + 1);
                }

                if (member.ReturnMembers.Count > 0)
                    sw.WriteLine(TAB + "<<return values>>");
                foreach (var mem in member.ReturnMembers.Values)
                {
                    WriteMember(mem, sw, level + 1, " (return value)");
                }

                if (member.Assignments.Count > 0)
                sw.WriteLine(TAB + "<<assignments>>");
                foreach (var mem in member.Assignments.Values)
                {
                    WriteMember(mem, sw, level + 1, " (assignment to " + member.FullNameReal + ")");
                }

                
            }

        }
          #endregion

        #region Declarations


        private static void HandleDeclarationStatement(Statement statement, Member member, int debug)
        {
            if (statement is Expression)
            {
                // its an expression, so lets treat is as should be
                Context context = new Context() { Member = member };
                ExpressionResult res = HandleDeclarationExpression((Expression)statement, member, context, debug + 1);
            }
            else if (statement is ExpressionStatement)
            {
                ExpressionStatement stat = (ExpressionStatement)statement;

                Context context = new Context() { Member = member };
                ExpressionResult res = HandleDeclarationExpression(stat.Expression, member, context, debug + 1);
            }
            else if (statement is IfStatement)
            {
                IfStatement stat = (IfStatement)statement;

                Context context = new Context() { Member = member };
                ExpressionResult res = HandleDeclarationExpression(stat.Expression, member, context, debug + 1);

                HandleDeclarationStatement(stat.Then, member, debug + 1);

                HandleDeclarationStatement(stat.Else, member, debug + 1);
            }
            else if (statement is VariableDeclarationStatement)
            {
                VariableDeclarationStatement stat = (VariableDeclarationStatement)statement;

                Member newMember = member.FindMember(stat.Identifier);

                if (newMember == null)
                {
                    newMember = new Member(stat.Identifier, member, MemberType.Member);
                    member.AddToFunctionBody(newMember);
                }

                Context context = new Context() { Member = newMember };
                ExpressionResult res = HandleDeclarationExpression(stat.Expression, member, context, debug + 1);

                if (res.Members.Count == 1 && res.SingleMember.Type == MemberType.AnonymousFunction)
                {
                    // its a function we are assigning, so lets make it the constructor
                    newMember.Constructor = res.SingleMember;
                }
            }
            else if (statement is ForEachInStatement)
            {
                ForEachInStatement stat = (ForEachInStatement)statement;

                Context context = new Context() { Member = member };
                ExpressionResult res = HandleDeclarationExpression(stat.Expression, member, context, debug + 1);

                HandleDeclarationStatement(stat.InitialisationStatement, member, debug + 1);

                HandleDeclarationStatement(stat.Statement, member, debug + 1);

            }
            else if (statement is SwitchStatement)
            {
                SwitchStatement stat = (SwitchStatement)statement;

                //TODO

            }
            else if (statement is WhileStatement)
            {
                WhileStatement stat = (WhileStatement)statement;

                Context context = new Context() { Member = member };
                ExpressionResult res = HandleDeclarationExpression(stat.Condition, member, context, debug + 1);

                HandleDeclarationStatement(stat.Statement, member, debug + 1);

            }
            else if (statement is ForStatement)
            {
                ForStatement stat = (ForStatement)statement;

                HandleDeclarationStatement(stat.InitialisationStatement, member, debug + 1);
                HandleDeclarationStatement(stat.ConditionExpression, member, debug + 1);

                HandleDeclarationStatement(stat.IncrementExpression, member, debug + 1);

                HandleDeclarationStatement(stat.Statement, member, debug + 1);

            }
            else if (statement is WithStatement)
            {
                WithStatement stat = (WithStatement)statement;

                HandleDeclarationStatement(stat.Statement, member, debug + 1);

                Context context = new Context() { Member = member };
                ExpressionResult res = HandleDeclarationExpression(stat.Expression, member, context, debug + 1);
            }
            else if (statement is ContinueStatement)
            {

            }
            else if (statement is FunctionDeclarationStatement)
            {
                FunctionDeclarationStatement stat = (FunctionDeclarationStatement)statement;

                if (stat.Name != null)
                {

                    // first create the function variable

                    Member functionMember = new Member(stat.Name, member, MemberType.Function);
                    member.AddToFunctionBody(functionMember);

                    int index = 0;
                    foreach (var para in stat.Parameters)
                    {
                        // the function has parameters, lets add them as variables
                        Member paramMember = new Member(para, functionMember, MemberType.Parameter);

                        if (functionMember.Parameters.ContainsKey(paramMember.FullName))
                        {
                            paramMember.Name += (index).ToString();
                        }
                        functionMember.Parameters.Add(paramMember.FullName, paramMember);

                        // add the index
                        functionMember.ParametersIndex.Add(index++, paramMember.FullName);
                    }

                    HandleDeclarationStatement(stat.Statement, functionMember, debug + 1);
                }
                else
                    throw new InvalidOperationException();

            }
            else if (statement is ThrowStatement)
            {

            }
            else if (statement is DoWhileStatement)
            {
                DoWhileStatement stat = (DoWhileStatement)statement;

                HandleDeclarationStatement(stat.Statement, member, debug + 1);

            }
            else if (statement is TryStatement)
            {

            }
            else if (statement is BlockStatement)
            {
                BlockStatement stat = (BlockStatement)statement;

                foreach (var substatement in stat.Statements)
                {
                    HandleDeclarationStatement(substatement, member, debug + 1);
                }

            }
            else if (statement is EmptyStatement)
            {

            }
            else if (statement is BreakStatement)
            {

            }
            else if (statement is ReturnStatement)
            {
                ReturnStatement stat = (ReturnStatement)statement;

                Context context = new Context() { Member = member };
                ExpressionResult res = HandleDeclarationExpression(stat.Expression, member, context, debug + 1);
            }

        }

        private static ExpressionResult HandleDeclarationExpression(Expression expression, Member member, Context context, int debug)
        {
            ExpressionResult res = new ExpressionResult();

            if (expression is MemberExpression)
            {
                MemberExpression ex = (MemberExpression)expression;

                ExpressionResult res2 = new ExpressionResult();
                ExpressionResult res3 = new ExpressionResult();

                if (ex.Previous != null)
                {
                    

                    res2 = HandleDeclarationExpression(ex.Previous, member, context, debug + 1);

                    Context subcontext = new Context() { Member = null };

                    if (res2.Members.Count != 0)
                    {
                        subcontext.Member = res2.SingleMember;

                        if (subcontext.Member == null)
                            subcontext.Member = member;
                        subcontext.AllowMemberCreation = true;
                    }

                    res3 = HandleDeclarationExpression(ex.Member, member, subcontext, debug + 1);
                }
                else
                    res3 = HandleDeclarationExpression(ex.Member, member, context, debug + 1);

                if (res3.IsPrototype)
                {
                    res.IsPrototype = true;
                    res.AddMembers(res2.Members);
                }
                else
                {
                    // add the resulting members
                    res.AddMembers(res3.Members);
                }
            }
            else if (expression is ArrayDeclaration)
            {
                ArrayDeclaration ex = (ArrayDeclaration)expression;

                Member arraymember = new Member("Array" + (AnoymCounter++).ToString(), member, MemberType.Array);
                if (!anonymous.ContainsKey(ex))
                    anonymous.Add(ex, arraymember);

                foreach (var item in ex.Parameters)
                {
                    if (item is Expression)
                    {
                        ExpressionResult res3 = HandleDeclarationExpression((Expression)item, member, context, debug + 1);
                    }
                    else
                        throw new InvalidOperationException();
                    //HandleDeclarationStatement(item, arraymember, debug + 1);
                }

                res.Members.Add(arraymember);
            }
            else if (expression is MethodCall)
            {
                MethodCall ex = (MethodCall)expression;

                foreach (var arg in ex.Arguments)
                {
                    ExpressionResult res2 = HandleDeclarationExpression(arg, member, context, debug + 1);
                }
            }
            else if (expression is CommaOperatorStatement)
            {
                CommaOperatorStatement ex = (CommaOperatorStatement)expression;

                foreach (var substatement in ex.Statements)
                {
                    HandleDeclarationStatement(substatement, member, debug + 1);
                }
            }
            else if (expression is AssignmentExpression)
            {
                AssignmentExpression ex = (AssignmentExpression)expression;
                ExpressionResult res2 = HandleDeclarationExpression(ex.Left, member, context, debug + 1);

                Context subcontext = new Context() { Member = member };
                if (res2.IsPrototype)
                {
                    subcontext.IsPrototype = true;
                    subcontext.Member = res2.SingleMember;
                }

                ExpressionResult res3 = HandleDeclarationExpression(ex.Right, member, subcontext, debug + 1);

                if (res3.IsFunctionDecl && res2.SingleMember != null)
                {
                    // since it was assigned a function, we can set its type to a function
                    res2.SingleMember.Type = MemberType.Method;
                }

                res.AddMembers(res2.Members);
            }
            else if (expression is ClrIdentifier)
            {
                ClrIdentifier ex = (ClrIdentifier)expression;

            }
            else if (expression is NewExpression)
            {
                NewExpression ex = (NewExpression)expression;

                ExpressionResult res3 = HandleDeclarationExpression(ex.Expression, member, context, debug + 1);

                foreach (var arg in ex.Arguments)
                {
                    ExpressionResult res2 = HandleDeclarationExpression(arg, member, context, debug + 1);
                }
            }

            else if (expression is TernaryExpression)
            {
                TernaryExpression ex = (TernaryExpression)expression;

                // these are assignments to lets add it to current context variable

                // lets parse this part in case it has important code
                ExpressionResult res2 = HandleDeclarationExpression(ex.LeftExpression, member, context, debug + 1);

                ExpressionResult res3 = HandleDeclarationExpression(ex.MiddleExpression, member, context, debug + 1);

                ExpressionResult res4 = HandleDeclarationExpression(ex.RightExpression, member, context, debug + 1);
            }
            else if (expression is BinaryExpression)
            {
                BinaryExpression ex = (BinaryExpression)expression;

                // these are assignments to lets add it to current context variable
                ExpressionResult res2 = HandleDeclarationExpression(ex.LeftExpression, member, context, debug + 1);

                ExpressionResult res3 = HandleDeclarationExpression(ex.RightExpression, member, context, debug + 1);
            }
            else if (expression is PropertyDeclarationExpression)
            {
                PropertyDeclarationExpression ex = (PropertyDeclarationExpression)expression;

                if (ex.Mode == PropertyExpressionType.Data)
                {

                    ExpressionResult res2 = HandleDeclarationExpression(ex.Expression, member, context, debug + 1);

                    res.IsFunctionDecl = res2.IsFunctionDecl;

                    res.AddMembers(res2.Members);
                    
                }
                else
                {
                    throw new InvalidOperationException("get/set not yet implemented");
                }
                // TODO get set

                /*ExpressionResult res3 = HandleDeclarationExpression(ex.GetExpression, member);

                ExpressionResult res4 = HandleDeclarationExpression(ex.SetExpression, member);*/

            }
            else if (expression is FunctionExpression)
            {
                FunctionExpression ex = (FunctionExpression)expression;

                if (anonymous.ContainsKey(ex))
                {
                    res.Members.Add(anonymous[ex]);
                    res.IsFunctionDecl = true;
                }
                else
                {
                    Member functionMember = new Member(ex.Name, member, MemberType.Function);

                    if (functionMember.Name == null)
                    {
                        functionMember.Name = "Anonym" + (AnoymCounter++).ToString();
                        functionMember.Type = MemberType.AnonymousFunction;
                        anonymous.Add(ex, functionMember);
                    }

                    member.AddToFunctionBody(functionMember);

                    int index = 0;
                    foreach (var para in ex.Parameters)
                    {
                        // the function has parameters, lets add them as variables
                        Member paramMember = new Member(para, functionMember, MemberType.Parameter);

                        if (functionMember.Parameters.ContainsKey(paramMember.FullName))
                        {
                            paramMember.Name += (index).ToString();
                        }

                        functionMember.Parameters.Add(paramMember.FullName, paramMember);
                        // add the index
                        functionMember.ParametersIndex.Add(index++, paramMember.FullName);
                    }

                    HandleDeclarationStatement(ex.Statement, functionMember, debug + 1);

                    res.Members.Add(functionMember);
                    res.IsFunctionDecl = true;
                }
            }
            else if (expression is Indexer)
            {
                Indexer ex = (Indexer)expression;

                ExpressionResult res2 = HandleDeclarationExpression(ex.Index, member, context, debug + 1);
            }
            else if (expression is UnaryExpression)
            {
                UnaryExpression ex = (UnaryExpression)expression;

                ExpressionResult res2 = HandleDeclarationExpression(ex.Expression, member, context, debug + 1);

                res.AddMembers(res2.Members);
            }
            else if (expression is Identifier)
            {
                Identifier ex = (Identifier)expression;

                if (ex.Text == "null")
                {
                    // its a null type
                    Member basicmember = new Member((AnoymCounter++).ToString(), member, MemberType.Value) { DataType = DataType.Null };
                    basicmember.BasicData = null;

                    res.Members.Add(basicmember);
                }
                else if (ex.Text == "prototype")
                {
                    res.IsPrototype = true;

                    res.Members.Add(context.Member);
                }
                else
                {
                    if (context.Member != null)
                    {
                        Member temp = context.Member.FindMember(ex.Text);

                        if (temp != null)
                        {
                            res.Members.Add(temp);
                        }
                        else if(context.AllowMemberCreation)
                        {
                            // we create a new member
                            Member newMember = new Member(ex.Text, context.Member, MemberType.Member);
                            context.Member.AddToClassMembers(newMember);

                            res.Members.Add(newMember);
                        }
                    }
                }

            }
            else if (expression is PropertyExpression)
            {
                PropertyExpression ex = (PropertyExpression)expression;
            }
            else if (expression is JsonExpression)
            {
                JsonExpression ex = (JsonExpression)expression;

                // create a new anonymous class member. if its a prototype, we do a trick and swap with the assigned member
                Member jsonMember = null;
                if (context.IsPrototype)
                    jsonMember = context.Member;
                else
                {
                    if (anonymous.ContainsKey(ex))
                    {
                        res.Members.Add(anonymous[ex]);
                        return res;
                    }
                    jsonMember = new Member("Json" + (AnoymCounter++).ToString(), member, MemberType.Class);
                    anonymous.Add(ex, jsonMember);
                }

                if (ex.Values.Count == 0)
                {
                    // an empty json body, do nothing
                }
                else
                {
                    foreach (var value in ex.Values)
                    {
                        if (value.Key == "constructor")
                        {
                            // special case, its a constructor
                            ExpressionResult res2 = HandleDeclarationExpression(value.Value, member, context, debug + 1);

                            if (res2.SingleMember != null)
                            {
                                if (res2.SingleMember.Constructor != null)
                                    jsonMember.Constructor = res2.SingleMember.Constructor;
                                else
                                    jsonMember.Constructor = res2.SingleMember;
                            }
                        }
                        else
                        {
                            ExpressionResult res2 = HandleDeclarationExpression(value.Value, member, context, debug + 1);

                            if (res2.Members.Count == 0)
                            {
                                // add a new empty member
                                Member classmember = new Member(value.Key, member, MemberType.Member);
                                jsonMember.AddToClassMembers(classmember);
                            }
                            else
                            {
                                // it must have been added to a parent, remove it so we can read it with this name

                                /*if (res2.SingleMember.Type == MemberType.AnonymousFunction)
                                    res2.SingleMember.Parent.Remove(res2.SingleMember);*/


                                res2.SingleMember.OverrideName = value.Key;
                                jsonMember.AddToClassMembers(res2.SingleMember);
                            }
                        }
                    }
                }

                res.Members.Add(jsonMember);
            }
            else if (expression is RegexpExpression)
            {
                RegexpExpression ex = (RegexpExpression)expression;

                Member regexmember = new Member((AnoymCounter++).ToString(), member, MemberType.Regex);
                regexmember.BasicData = ex.Regexp;
                res.Members.Add(regexmember);

            }
            else if (expression is ValueExpression)
            {
                ValueExpression ex = (ValueExpression)expression;
                Member basicmember = new Member((AnoymCounter++).ToString(), member, MemberType.Value);
                basicmember.BasicData = ex.Value;

                switch (ex.TypeCode)
                {
                    case TypeCode.Double:
                        basicmember.DataType = DataType.Float;
                        break;
                    case TypeCode.String:
                        basicmember.DataType = DataType.String;
                        break;
                    case TypeCode.Boolean:
                        basicmember.DataType = DataType.Bool;
                        break;
                    case TypeCode.Byte:
                        basicmember.DataType = DataType.Int;
                        break;
                    case TypeCode.Char:
                        basicmember.DataType = DataType.String;
                        break;
                    case TypeCode.DateTime:
                        basicmember.DataType = DataType.Date;
                        break;
                    case TypeCode.Decimal:
                        basicmember.DataType = DataType.Float;
                        break;
                    case TypeCode.Int16:
                        basicmember.DataType = DataType.Int;
                        break;
                    case TypeCode.Int32:
                        basicmember.DataType = DataType.Int;
                        break;
                    case TypeCode.Int64:
                        basicmember.DataType = DataType.Int;
                        break;
                    case TypeCode.Single:
                        basicmember.DataType = DataType.Float;
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                res.Members.Add(basicmember);

            }
            else if (expression == null)
            {
                // do nothing
            }
            else
            {
                // not found, lets try to handle it as a statement
                HandleDeclarationStatement(expression, member, debug + 1);
            }

            return res;
        }
        #endregion

        #region assignements
        private static void HandleAssignmentStatement(Statement statement, Member member, int debug)
        {
            if (statement is Expression)
            {
                // its an expression, so lets treat is as should be
                Context context = new Context() { Member = member };
                ExpressionResult res = HandleAssignmentExpression((Expression)statement, member, context, debug + 1);
            }
            else if (statement is ExpressionStatement)
            {
                ExpressionStatement stat = (ExpressionStatement)statement;

                Context context = new Context() { Member = member };
                ExpressionResult res = HandleAssignmentExpression(stat.Expression, member, context, debug + 1);
            }
            else if (statement is IfStatement)
            {
                IfStatement stat = (IfStatement)statement;

                Context context = new Context() { Member = member };
                ExpressionResult res = HandleAssignmentExpression(stat.Expression, member, context, debug + 1);

                HandleAssignmentStatement(stat.Then, member, debug + 1);

                HandleAssignmentStatement(stat.Else, member, debug + 1);
            }
            else if (statement is VariableDeclarationStatement)
            {
                VariableDeclarationStatement stat = (VariableDeclarationStatement)statement;

                Member newMember = member.FindMember(stat.Identifier);

                if (newMember == null)
                {
                    throw new InvalidOperationException("member not found: " + stat.Identifier);
                }

                Context context = new Context() { Member = member };
                ExpressionResult res = HandleAssignmentExpression(stat.Expression, member, context, debug + 1);

                if (res.Members.Count == 1 && res.SingleMember.Type == MemberType.AnonymousFunction)
                {
                    // its a function we are assigning, so lets make it the constructor
                    newMember.Constructor = res.SingleMember;
                }
                else
                    newMember.AddAssignments(res.Members);
            }
            else if (statement is ForEachInStatement)
            {
                ForEachInStatement stat = (ForEachInStatement)statement;

                Context context = new Context() { Member = member };
                ExpressionResult res = HandleAssignmentExpression(stat.Expression, member, context, debug + 1);

                HandleAssignmentStatement(stat.InitialisationStatement, member, debug + 1);

                HandleAssignmentStatement(stat.Statement, member, debug + 1);

            }
            else if (statement is SwitchStatement)
            {
                SwitchStatement stat = (SwitchStatement)statement;

                //TODO

            }
            else if (statement is WhileStatement)
            {
                WhileStatement stat = (WhileStatement)statement;

                Context context = new Context() { Member = member };
                ExpressionResult res = HandleAssignmentExpression(stat.Condition, member, context, debug + 1);

                HandleAssignmentStatement(stat.Statement, member, debug + 1);

            }
            else if (statement is ForStatement)
            {
                ForStatement stat = (ForStatement)statement;

                HandleAssignmentStatement(stat.InitialisationStatement, member, debug + 1);
                HandleAssignmentStatement(stat.ConditionExpression, member, debug + 1);

                HandleAssignmentStatement(stat.IncrementExpression, member, debug + 1);

                HandleAssignmentStatement(stat.Statement, member, debug + 1);

            }
            else if (statement is WithStatement)
            {
                WithStatement stat = (WithStatement)statement;

                HandleAssignmentStatement(stat.Statement, member, debug + 1);

                Context context = new Context() { Member = member };
                ExpressionResult res = HandleAssignmentExpression(stat.Expression, member, context, debug + 1);
            }
            else if (statement is ContinueStatement)
            {

            }
            else if (statement is FunctionDeclarationStatement)
            {
                FunctionDeclarationStatement stat = (FunctionDeclarationStatement)statement;

                if (stat.Name != null)
                {
                    Member funcMember = member.FindMember(stat.Name);
                    HandleAssignmentStatement(stat.Statement, funcMember, debug + 1);
                }
            }
            else if (statement is ThrowStatement)
            {

            }
            else if (statement is DoWhileStatement)
            {
                DoWhileStatement stat = (DoWhileStatement)statement;

                HandleAssignmentStatement(stat.Statement, member, debug + 1);

            }
            else if (statement is TryStatement)
            {

            }
            else if (statement is BlockStatement)
            {
                BlockStatement stat = (BlockStatement)statement;

                foreach (var substatement in stat.Statements)
                {
                    HandleAssignmentStatement(substatement, member, debug + 1);
                }

            }
            else if (statement is EmptyStatement)
            {

            }
            else if (statement is BreakStatement)
            {

            }
            else if (statement is ReturnStatement)
            {
                ReturnStatement stat = (ReturnStatement)statement;

                Context context = new Context() { Member = member };
                ExpressionResult res = HandleAssignmentExpression(stat.Expression, member, context, debug + 1);

                member.AddReturnMembers(res.Members);

            }

        }

        private static ExpressionResult HandleAssignmentExpression(Expression expression, Member member, Context context, int debug)
        {
            ExpressionResult res = new ExpressionResult();

            if (expression is MemberExpression)
            {
                MemberExpression ex = (MemberExpression)expression;

                ExpressionResult res2 = new ExpressionResult();
                ExpressionResult res3 = new ExpressionResult();

                if (ex.Previous != null)
                {


                    res2 = HandleAssignmentExpression(ex.Previous, member, context, debug + 1);

                    Context subcontext = new Context() { Member = null };

                    if (res2.Members.Count != 0)
                    {
                        subcontext.Member = res2.SingleMember;

                        if (subcontext.Member == null)
                            subcontext.Member = member;
                        subcontext.AllowMemberCreation = true;
                    }

                    res3 = HandleDeclarationExpression(ex.Member, member, subcontext, debug + 1);
                }
                else
                    res3 = HandleAssignmentExpression(ex.Member, member, context, debug + 1);

                if (res3.IsPrototype)
                {
                    res.IsPrototype = true;
                    res.AddMembers(res2.Members);
                }
                else
                {
                    // add the resulting members
                    res.AddMembers(res3.Members);
                }
            }
            else if (expression is ArrayDeclaration)
            {
                ArrayDeclaration ex = (ArrayDeclaration)expression;

                // required to keep nonym counter in synch

                foreach (var item in ex.Parameters)
                {
                    if (item is Expression)
                    {
                        ExpressionResult res3 = HandleDeclarationExpression((Expression)item, member, context, debug + 1);
                    }
                    else
                        throw new InvalidOperationException();
                    //HandleDeclarationStatement(item, arraymember, debug + 1);
                }
            }
            else if (expression is MethodCall)
            {
                MethodCall ex = (MethodCall)expression;

                Member activeMember = context.Member;
                if (context.Member.Type == MemberType.Method || context.Member.Type == MemberType.AnonymousFunction || context.Member.Type == MemberType.Function)
                {
                    activeMember = context.Member;
                }
                else if (context.Member.Constructor != null)
                {
                    activeMember = context.Member.Constructor;
                }

                int index = 0;
                foreach (var arg in ex.Arguments)
                {
                    ExpressionResult res2 = HandleDeclarationExpression(arg, member, context, debug + 1);

                    string name = activeMember.ParametersIndex[index];
                    activeMember.Parameters[name].AddAssignments(res2.Members);
                    index++;
                }
                res.Members.Add(context.Member);
            }
            else if (expression is CommaOperatorStatement)
            {
                CommaOperatorStatement ex = (CommaOperatorStatement)expression;

                foreach (var substatement in ex.Statements)
                {
                    HandleAssignmentStatement(substatement, member, debug + 1);
                }
            }
            else if (expression is AssignmentExpression)
            {
                AssignmentExpression ex = (AssignmentExpression)expression;
                ExpressionResult res2 = HandleAssignmentExpression(ex.Left, member, context, debug + 1);

                Context subcontext = new Context() { Member = member };
                if (res2.IsPrototype)
                {
                    subcontext.IsPrototype = true;
                    subcontext.Member = res2.SingleMember;
                }

                ExpressionResult res3 = HandleAssignmentExpression(ex.Right, member, subcontext, debug + 1);

                if (res3.IsFunctionDecl && res2.SingleMember != null)
                {
                    // since it was assigned a function, we can set its type to a function
                    res2.SingleMember.Type = MemberType.Method;
                }

                // these are being assigned to these members
                foreach (var mem in res2.Members)
                    mem.AddAssignments(res3.Members);

                res.AddMembers(res2.Members);
            }
            else if (expression is ClrIdentifier)
            {
                ClrIdentifier ex = (ClrIdentifier)expression;

            }
            else if (expression is NewExpression)
            {
                NewExpression ex = (NewExpression)expression;

                ExpressionResult res3 = HandleAssignmentExpression(ex.Expression, member, context, debug + 1);

                Member activeMember = res3.SingleMember;
                if (activeMember.Constructor != null)
                {
                    activeMember = activeMember.Constructor;
                }

                int index = 0;
                // a new expression is a constructor function call
                foreach (var arg in ex.Arguments)
                {
                    ExpressionResult res2 = HandleAssignmentExpression(arg, member, context, debug + 1);

                    //foreach (var mem in res3.Members)
                    //{
                        if (activeMember.ParametersIndex.ContainsKey(index))
                        {
                            string paramkey = activeMember.ParametersIndex[index];
                            activeMember.Parameters[paramkey].AddAssignments(res2.Members);
                        }
                        index++;
                   // }
                }

                //TODO check this is valid becasue a new needs to instance the result of this method
                // for now we just pass the expression newed on
                res.AddMembers(res3.Members);
            }

            else if (expression is TernaryExpression)
            {
                TernaryExpression ex = (TernaryExpression)expression;

                // these are assignments to lets add it to current context variable

                // lets parse this part in case it has important code
                ExpressionResult res2 = HandleAssignmentExpression(ex.LeftExpression, member, context, debug + 1);

                //Context subContext = new Context() { Name = "[Ternary]", Parent = context, Type = ContextType.Expression };
                ExpressionResult res3 = HandleAssignmentExpression(ex.MiddleExpression, member, context, debug + 1);

                //subContext.Assignments.Add();

                ExpressionResult res4 = HandleAssignmentExpression(ex.RightExpression, member, context, debug + 1);

                res.AddMembers(res3.Members);
                res.AddMembers(res4.Members);
            }
            else if (expression is BinaryExpression)
            {
                BinaryExpression ex = (BinaryExpression)expression;

                // these are assignments to lets add it to current context variable
                ExpressionResult res2 = HandleAssignmentExpression(ex.LeftExpression, member, context, debug + 1);

                ExpressionResult res3 = HandleAssignmentExpression(ex.RightExpression, member, context, debug + 1);

                // binary always returns bool type
                Member boolMember = new Member((AnoymCounter++).ToString(), member, MemberType.Value) { DataType = DataType.Bool, BasicData = false };
                res.Members.Add(boolMember);
            }
            else if (expression is PropertyDeclarationExpression)
            {
                PropertyDeclarationExpression ex = (PropertyDeclarationExpression)expression;

                if (ex.Mode == PropertyExpressionType.Data)
                {
                    ExpressionResult res2 = HandleAssignmentExpression(ex.Expression, member, context, debug + 1);

                    res.IsFunctionDecl = res2.IsFunctionDecl;

                    res.AddMembers(res2.Members);
                }
                else
                {
                    throw new InvalidOperationException("get/set not yet implemented");
                }
                // TODO get set

                /*ExpressionResult res3 = HandleAssignmentExpression(ex.GetExpression, member, context);

                ExpressionResult res4 = HandleAssignmentExpression(ex.SetExpression, member, context);*/

            }
            else if (expression is FunctionExpression)
            {
                FunctionExpression ex = (FunctionExpression)expression;

                string functionName = ex.Name;

                Member functionMember = null;

                if (functionName == null)
                {
                    functionMember = anonymous[ex];
                }
                else
                    functionMember = member.FindMember(functionName);
                

                HandleAssignmentStatement(ex.Statement, functionMember, debug + 1);

                res.Members.Add(functionMember);
            }
            else if (expression is Indexer)
            {
                Indexer ex = (Indexer)expression;

                ExpressionResult res2 = HandleAssignmentExpression(ex.Index, member, context, debug + 1);

                res.Members.Add(context.Member);
            }
            else if (expression is UnaryExpression)
            {
                UnaryExpression ex = (UnaryExpression)expression;

                ExpressionResult res2 = HandleAssignmentExpression(ex.Expression, member, context, debug + 1);

                res.AddMembers(res2.Members);
            }
            else if (expression is Identifier)
            {
                Identifier ex = (Identifier)expression;

                if (ex.Text == "null")
                {
                    // its a null type
                    Member basicmember = new Member((AnoymCounter++).ToString(), member, MemberType.Value) { DataType = DataType.Null };
                    basicmember.BasicData = null;

                    res.Members.Add(basicmember);
                }
                else if (ex.Text == "prototype")
                {
                    res.IsPrototype = true;
                }
                else
                {
                    if (context.Member != null)
                    {
                        Member temp = context.Member.FindMember(ex.Text);

                        if (temp != null)
                        {
                            res.Members.Add(temp);
                        }
                        else
                        {
                            // we create missing members every time at this step
                            Member newMember = new Member(ex.Text, context.Member, MemberType.Member);
                            context.Member.AddToClassMembers(newMember);

                            res.Members.Add(newMember);
                        }
                    }
                }

            }
            else if (expression is PropertyExpression)
            {
                PropertyExpression ex = (PropertyExpression)expression;
            }
            else if (expression is JsonExpression)
            {
                JsonExpression ex = (JsonExpression)expression;

                // create a new anonymous class member. if its a prototype, we do a trick and swap with the assigned member
                Member jsonMember = null;
                if (context.IsPrototype)
                    jsonMember = context.Member;
                else
                    jsonMember = anonymous[ex];

                if (ex.Values.Count == 0)
                {
                    // an empty json body, do nothing
                }
                else
                {
                    foreach (var value in ex.Values)
                    {
                        if (value.Key == "constructor")
                        {
                            // special case, its a constructor
                            ExpressionResult res2 = HandleAssignmentExpression(value.Value, member, context, debug + 1);

                            if (res2.SingleMember != null && jsonMember != context.Member)
                            {
                                if (res2.SingleMember.Constructor != null)
                                    jsonMember.Constructor = res2.SingleMember.Constructor;
                                else
                                    jsonMember.Constructor = res2.SingleMember;
                            }
                        }
                        else
                        {
                            ExpressionResult res2 = HandleAssignmentExpression(value.Value, member, context, debug + 1);

                            if (jsonMember != context.Member)
                                if (res2.Members.Count == 0)
                                {
                                    // add a new empty member
                                    Member classmember = new Member(value.Key, member, MemberType.Member);
                                    jsonMember.AddToClassMembers(classmember);
                                }
                                else
                                {
                                    // it must have been added to a parent, remove it so we can read it with this name
                                    //res2.SingleMember.Parent.Remove(res2.SingleMember);
                                    res2.SingleMember.OverrideName = value.Key;
                                    jsonMember.AddToClassMembers(res2.SingleMember);
                                }
                        }
                    }
                }

                if (jsonMember != context.Member)
                    res.Members.Add(jsonMember);
            }
            else if (expression is RegexpExpression)
            {
                RegexpExpression ex = (RegexpExpression)expression;

                Member regexmember = new Member((AnoymCounter++).ToString(), member, MemberType.Regex);
                regexmember.BasicData = ex.Regexp;
                res.Members.Add(regexmember);

            }
            else if (expression is ValueExpression)
            {
                ValueExpression ex = (ValueExpression)expression;
                Member basicmember = new Member((AnoymCounter++).ToString(), member, MemberType.Value);
                basicmember.BasicData = ex.Value;

                switch (ex.TypeCode)
                {
                    case TypeCode.Double:
                        basicmember.DataType = DataType.Float;
                        break;
                    case TypeCode.String:
                        basicmember.DataType = DataType.String;
                        break;
                    case TypeCode.Boolean:
                        basicmember.DataType = DataType.Bool;
                        break;
                    case TypeCode.Byte:
                        basicmember.DataType = DataType.Int;
                        break;
                    case TypeCode.Char:
                        basicmember.DataType = DataType.String;
                        break;
                    case TypeCode.DateTime:
                        basicmember.DataType = DataType.Date;
                        break;
                    case TypeCode.Decimal:
                        basicmember.DataType = DataType.Float;
                        break;
                    case TypeCode.Int16:
                        basicmember.DataType = DataType.Int;
                        break;
                    case TypeCode.Int32:
                        basicmember.DataType = DataType.Int;
                        break;
                    case TypeCode.Int64:
                        basicmember.DataType = DataType.Int;
                        break;
                    case TypeCode.Single:
                        basicmember.DataType = DataType.Float;
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                res.Members.Add(basicmember);

            }
            else if (expression == null)
            {
                // do nothing
            }
            else
            {
                // not found, lets try to handle it as a statement
                HandleAssignmentStatement(expression, member, debug + 1);
            }

            return res;
        }
        #endregion
    }
}
