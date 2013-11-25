using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using CSharpSMCLtoPython.ASTbuilder;
using Microsoft.SqlServer.Server;


namespace CSharpSMCLtoPython.Visitors
{
    [Serializable]
    public class TypeCheckingException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public TypeCheckingException() { }
        public TypeCheckingException(string message) : base(message) { }
        public TypeCheckingException(string message, Exception inner) : base(message, inner) { }

        protected TypeCheckingException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }


    internal class FuncEnv
    {
        public Function Function;
        public Dictionary<string, SmclType> SymbolTable = new Dictionary<string, SmclType>();
        public Dictionary<string, PartEnv> IdToClient = new Dictionary<string, PartEnv>(); // for method invocation


        public void ArgsMatchParamsType(IList<Exp> @args)
        {
            if (@args.Count != Function.Params.Count)
                throw new TypeCheckingException(
                    String.Format(
                            "the function {0}(...) has {1} parameters. Calling with {2} arguments.",
                            Function.Name, Function.Params.Count, @args.Count)
                            );
            int i = 0;
            foreach (var p in Function.Params)
            {
                if (!p.SmclType.Equals(@args[i++].SmclType))
                    throw new TypeCheckingException(
                        String.Format(
                            "the type of the {0} argument when calling function {1}(...) is wrong.\nUsing {2} but needed {3}.",
                            i, Function.Name, @args[--i].SmclType, p.SmclType
                            ));
            }
        }

        public FuncEnv(Function fun)
        {
            Function = fun;
        }

        public SmclType GetTypeFromId(string id)
        {
            if (SymbolTable.ContainsKey(id))
                return SymbolTable[id];
            return new NoneType();
        }

        public SmclType GetReturn()
        {
            return Function.SmclType;
        }
    }

    internal class PartEnv
    {
        // name -> env
        public string PartName { get; private set; }
        public Dictionary<string, FuncEnv> Functions = new Dictionary<string, FuncEnv>();
        public Dictionary<string, SmclType> Tunnels = new Dictionary<string, SmclType>();
        public Dictionary<string, PartEnv> Groups;

        public readonly Multipart Mp;

        public PartEnv(Multipart mp)
        {
            Mp = mp;
            PartName = mp.Name;
            if (mp.GetType() == typeof(Server))
            {
                Groups = new Dictionary<string, PartEnv>();
            }           
        }

        public void Add(Function fun)
        {
            foreach (var f in Functions.Values)
            {
                if (f.Function.Name == fun.Name)
                    throw new TypeCheckingException("There are two functions with the same name: " + fun.Name);
            }
            Functions.Add(fun.Name, new FuncEnv(fun));

            foreach (var p in fun.Params)
            {
                // when add a function all its params are inserted in the symbol table
                Functions[fun.Name].SymbolTable.Add(p.Id.Name, p.SmclType);
            }

        }

        public void CheckTunnelMethod(string id, SmclType expType)
        {
            if (Mp.GetType() != typeof(Client))
                throw new TypeCheckingException("tunnel methods can be used only within Clients.");

            if (!Tunnels.ContainsKey(id))
                throw new TypeCheckingException(id + " isn't defined as tunnel.");

            if (! Tunnels[id].Equals(expType))
                throw new TypeCheckingException(
                    String.Format(
                        "the tunnel {0} has type {1} but is invoked with a {2} expression.",
                        id, Tunnels[id], expType
                        )
                );
            
        }

    }


    internal class Env
    {
        public List<PartEnv> Clients = new List<PartEnv>();
        public PartEnv Server { get; set; }

        public PartEnv TmpPartEnv { get; set; }
        public PartEnv InvokingOn { get; set; }
        public string TmpFunName { get; set; }

        public void Add(Client c)
        {
            PartEnv ce = new PartEnv(c);
            Clients.Add(ce);
            TmpPartEnv = ce;
        }

        public void Add(Server s)
        {
            PartEnv ce = new PartEnv(s);
            Server = ce;
            TmpPartEnv = ce;
        }

        public bool IsVisitingServer()
        {
            return TmpPartEnv.Mp.GetType() == typeof(Server);
        }

        public bool VariableAlreadyDefined(string idName)
        {
            return TmpPartEnv.Functions[TmpFunName].SymbolTable.ContainsKey(idName);
        }

        public SmclType GetMyTypeFromId(string idName)
        {
            return TmpPartEnv.Functions[TmpFunName].GetTypeFromId(idName);
        }

        public void AddSymbolInFunction(string id, SmclType type)
        {
            if (IsVisitingServer())
            {
                Server s = (Server)TmpPartEnv.Mp;
                foreach (var group in s.Groups)
                {
                    if (group.Id.Name == id)
                        throw new TypeCheckingException("GroupName conflict group"); //TODO
                }
                foreach (var tunnel in Server.Tunnels.Keys)
                {
                    if (tunnel == id)
                        throw new TypeCheckingException("GroupName in server conflict tunnel"); //TODO
                }
            }
            else
            {
                
                Client c = (Client)TmpPartEnv.Mp;
                foreach (var tunnel in c.Tunnels)
                {
                    if (tunnel.Typed.Id.Name == id)
                        throw new TypeCheckingException("GroupName conflict tunnel"); //TODO
                }
            }
            if (!TmpPartEnv.Functions.ContainsKey(TmpFunName))
                throw new TypeCheckingException("function not found"); //TODO
            if (TmpPartEnv.Functions[TmpFunName].SymbolTable.ContainsKey(id))
                throw new TypeCheckingException("duplicate variable"); //TODO
            TmpPartEnv.Functions[TmpFunName].SymbolTable.Add(id, type);
        }

        public void RemoveSymbolInFunction(string id)
        {
            if (!TmpPartEnv.Functions.ContainsKey(TmpFunName))
                throw new TypeCheckingException("function not found"); //TODO
            TmpPartEnv.Functions[TmpFunName].SymbolTable.Remove(id);
        }

        public void AddClientForMethodInvocation(Typed var, PartEnv client)
        {
            if (!IsVisitingServer())
                throw new TypeCheckingException("you can invoke methods only from a Server instance --> " + var.Id.Name);
            AddSymbolInFunction(var.Id.Name, var.SmclType);
            TmpPartEnv.Functions[TmpFunName].IdToClient.Add(var.Id.Name, client);
        }

        public void RemoveClientForMethodInvocation(Typed var)
        {
            RemoveSymbolInFunction(var.Id.Name);
            TmpPartEnv.Functions[TmpFunName].IdToClient.Remove(var.Id.Name);
        }
    }


    internal class TypecheckVisitor : ITreeNodeVisitor
    {
        private Env _env = new Env();
        private static readonly IntType IntType = new IntType();
        private static readonly SintType SintType = new SintType();
        private static readonly BoolType BoolType = new BoolType();
        private static readonly SboolType SboolType = new SboolType();
        private static readonly StringType StringType = new StringType();
        private static readonly VoidType VoidType = new VoidType();
        private static readonly ClientType ClientType = new ClientType();
        private static readonly SclientType SclientType = new SclientType();
        private static readonly ServerType ServerType = new ServerType();
        private static readonly GroupType GroupType = new GroupType();
        private static readonly TunnelType TunnelType = new TunnelType();

        public TypecheckVisitor()
        {

        }

        private static SmclType ConvertToSecret (SmclType t)
        {
            var ttype = t.GetSecret();
            if (ttype == null)
                return null;
            if (ttype == typeof(SintType))
            {
                return SintType;
            }
            if (ttype == typeof(SboolType))
            {
                return SboolType;
            }
            if (ttype == typeof(SclientType))
            {
                return SclientType;
            }
            return null;
        }

        private static SmclType ConvertToPublic(SmclType t)
        {
            var ttype = t.GetPublic();
            if (ttype == null)
                return null;
            if (ttype == typeof(IntType))
            {
                return IntType;
            }
            if (ttype == typeof(BoolType))
            {
                return BoolType;
            }
            if (ttype == typeof(ClientType))
            {
                return ClientType;
            }
            return null;
        }
        
        private void MustBe(SmclType t, SmclType mustBe, string msg)
        {
            if (!t.Equals(mustBe))
                throw new TypeCheckingException(msg);
        }

        
        private void MustBeSintOrSubtype(SmclType t, string msg)
        {
            MustBe(t, IntType, msg);
        }


        private void MustBeSboolOrSubtype(SmclType t, string msg)
        {
            MustBe(t, BoolType, msg);
        }
        

        private void ResultIsSintAndBothOperandsMustBeSintOrSubtype(BinaryOp binaryOp)
        {
            binaryOp.Left.Accept(this);
            MustBeSintOrSubtype(binaryOp.Left.SmclType, "The type of the left operand in the expression\n (" + binaryOp + ")\n must be integer but is a " + binaryOp.Left.SmclType);
            binaryOp.Right.Accept(this);
            MustBeSintOrSubtype(binaryOp.Right.SmclType, "The type of the right operand in the expression\n (" + binaryOp + ")\n must be integer but is a " + binaryOp.Right.SmclType);
            binaryOp.SmclType = IntType;
        }

        private void ResultIsSboolAndBothOperandsMustBeSboolOrSubtype(BinaryOp binaryOp)
        {
            binaryOp.Left.Accept(this);
            MustBeSboolOrSubtype(binaryOp.Left.SmclType, "The type of the left operand in the expression\n (" + binaryOp + ")\n must be bool but is a " + binaryOp.Left.SmclType);
            binaryOp.Right.Accept(this);
            MustBeSboolOrSubtype(binaryOp.Right.SmclType, "The type of the right operand in the expression\n (" + binaryOp + ")\n must be bool but is a " + binaryOp.Right.SmclType);
            binaryOp.SmclType = BoolType;
        }

        private void ResultIsSboolAndBothOperandsMustBeSintOrSubtype(BinaryOp binaryOp)
        {
            binaryOp.Left.Accept(this);
            MustBeSintOrSubtype(binaryOp.Left.SmclType, "The type of the left operand in the expression\n (" + binaryOp + ")\n must be integer but is a " + binaryOp.Left.SmclType);
            binaryOp.Right.Accept(this);
            MustBeSintOrSubtype(binaryOp.Right.SmclType, "The type of the right operand in the expression\n (" + binaryOp + ")\n must be integer but is a " + binaryOp.Right.SmclType);
            binaryOp.SmclType = BoolType;
        }

        public void Visit(Sum sum)
        {
            ResultIsSintAndBothOperandsMustBeSintOrSubtype(sum);
        }

        public void Visit(Subtraction sub)
        {
            ResultIsSintAndBothOperandsMustBeSintOrSubtype(sub);
        }

        public void Visit(Product prod)
        {
            ResultIsSintAndBothOperandsMustBeSintOrSubtype(prod);
        }

        public void Visit(Division div)
        {
            ResultIsSintAndBothOperandsMustBeSintOrSubtype(div);
        }

        public void Visit(Module rem)
        {
            ResultIsSintAndBothOperandsMustBeSintOrSubtype(rem);
        }

        public void Visit(And and)
        {
            ResultIsSboolAndBothOperandsMustBeSboolOrSubtype(and);
        }

        public void Visit(Or or)
        {
            ResultIsSboolAndBothOperandsMustBeSboolOrSubtype(or);
        }

        public void Visit(Not not)
        {
            not.Operand.Accept(this);
            MustBeSboolOrSubtype(not.Operand.SmclType, string.Format("The operand in the expression\n ({0} must be bool", not));
            not.SmclType = BoolType;
        }

        public void Visit(Equal eq)
        {
            eq.Left.Accept(this);
            eq.Right.Accept(this);
            if (!eq.Left.SmclType.Equals(eq.Right.SmclType))
                throw new TypeCheckingException("Both operands of == must have the same type");
            eq.SmclType = BoolType;
        }


        public void Visit(LessThan lt)
        {
            ResultIsSboolAndBothOperandsMustBeSintOrSubtype(lt);
        }


        public void Visit(GreaterThan greaterThan)
        {
            ResultIsSboolAndBothOperandsMustBeSintOrSubtype(greaterThan);
        }


        public void Visit(Id id)
        {
            id.SmclType = _env.GetMyTypeFromId(id.Name);
        }

        public void Visit(Display print)
        {
            print.Exp.Accept(this);
            if (!print.Exp.SmclType.Equals(StringType))
            {
                throw new TypeCheckingException("You can only print strings, not " + print.Exp.SmclType);
            }
        }

        public void Visit(EvalExp eval)
        {
            eval.Exp.Accept(this);
        }


        public void Visit(If ifs)
        {
            ifs.Guard.Accept(this);
            MustBeSboolOrSubtype(ifs.Guard.SmclType, "The if guard must be bool");
            ifs.Body.Accept(this);
        }


        public void Visit(While whiles)
        {
            whiles.Guard.Accept(this);
            MustBeSboolOrSubtype(whiles.Guard.SmclType, "The while guard");
            whiles.Body.Accept(this);
        }

        public void Visit(Block block)
        {
            foreach (var s in block.Statements)
            {
                s.Accept(this);
            }
        }

        public void Visit(Prog prog)
        {
            foreach (var c in prog.Clients)
            {
                c.Accept(this);
            }
            prog.Server.Accept(this);
        }

        public void Visit(Function function)
        {
            _env.TmpPartEnv.Add(function);
            _env.TmpFunName = function.Name;
            foreach (var s in function.Stmts)
            {
                s.Accept(this);
            }
        }

        public void Visit(Else eelse)
        {
            eelse.Body.Accept(this);
        }

        public void Visit(Typed typed)
        {
            if (_env.VariableAlreadyDefined(typed.Id.Name))
                throw new TypeCheckingException("Variable already defined --> " + typed.Id.Name);
            typed.Id.SmclType = typed.SmclType;

        }

        public void Visit(Assignment assignment)
        {
            assignment.Id.Accept(this);
            if (_env.VariableAlreadyDefined(assignment.Id.Name))
            {
                assignment.Id.SmclType = _env.GetMyTypeFromId(assignment.Id.Name);
                assignment.Exp.Accept(this);
                if (!assignment.Id.SmclType.Equals(assignment.Exp.SmclType))
                    throw new TypeCheckingException(
                        "wrong assignment. "
                        + assignment.Id.SmclType
                        + " = "
                        + assignment.Exp.SmclType
                        + "\nThey must be the same type."
                        );
            }
            else
            {
                throw new TypeCheckingException("variable never declared --> " + assignment.Id.Name);
            }
        }

        public void Visit(Tunnel tunnel)
        {
            if (_env.TmpPartEnv.Tunnels.ContainsKey(tunnel.Typed.Id.Name))
                throw new TypeCheckingException("there's another tunnel with the same name --> " + tunnel.Typed.Id.Name);
            _env.TmpPartEnv.Tunnels.Add(tunnel.Typed.Id.Name, tunnel.Typed.SmclType);
        }

        public void Visit(Client client)
        {
            _env.Add(client);
            foreach (var t in client.Tunnels)
            {
                t.Accept(this);
            }
            foreach (var f in client.Functions)
            {
                f.Accept(this);
            }
        }

        public void Visit(Server server)
        {
            _env.Add(server);
            foreach (var g in server.Groups)
            {
                g.Accept(this);
            }
            foreach (var client in _env.TmpPartEnv.Groups.Values)
            {
                _env.TmpPartEnv.Tunnels = _env.TmpPartEnv.Tunnels.Concat(client.Tunnels).ToDictionary(e => e.Key, e => e.Value);
            }
            foreach (var f in server.Functions)
            {
                f.Accept(this);
            }
        }

        public void Visit(For ffor)
        {
            if (!_env.IsVisitingServer())
                throw new TypeCheckingException("you can use for cycle only within server.");
            ffor.Typed.Accept(this);
            MustBe(ffor.Typed.SmclType, ClientType, "for each only with Client, not --> " + ffor.Typed.SmclType);
            ffor.Id.Accept(this);
            foreach (var g in _env.TmpPartEnv.Groups.Keys)
            {
                if (g == ffor.Id.Name) // (group, id, env)
                {
                    _env.AddClientForMethodInvocation(ffor.Typed, _env.TmpPartEnv.Groups[g]);
                    ffor.Body.Accept(this);
                    _env.RemoveClientForMethodInvocation(ffor.Typed);
                    return;
                }
            }
            throw new TypeCheckingException("This is an undefined group --> " + ffor.Id.Name);
        }


        public void Visit(FunctionCall functionCall)
        {
            foreach (var p in functionCall.Params)
            {
                p.Accept(this);
            }

            FuncEnv targetFunction = null;
            if (_env.InvokingOn == null)
            {
                if(_env.TmpPartEnv.Functions.Keys.All(fName => functionCall.Name != fName))
                {
                    throw new TypeCheckingException(functionCall.Name + " isn't defined.\n");
                }
                targetFunction = _env.TmpPartEnv.Functions[functionCall.Name];
                targetFunction.ArgsMatchParamsType(functionCall.Params);
            }
            if (_env.InvokingOn != null)
            {
                targetFunction = _env.InvokingOn.Functions[functionCall.Name];
                targetFunction.ArgsMatchParamsType(functionCall.Params);
            }
            if (targetFunction!=null)
                functionCall.SmclType = targetFunction.GetReturn();
            else
                throw new TypeCheckingException("typechecker logic fails in functionCall visit.");
        }

        public void Visit(BoolLiteral bl)
        {
            bl.SmclType = BoolType;
        }

        public void Visit(IntLiteral il)
        {
            il.SmclType = IntType;
        }

        public void Visit(Declaration declaration)
        {
            declaration.Typed.Accept(this);
            _env.AddSymbolInFunction(declaration.Typed.Id.Name, declaration.Typed.Id.SmclType);
            if (declaration.Assignment != null)
            {
                declaration.Assignment.Accept(this);
                /*
                if (!declaration.Typed.SmclType.Equals(declaration.Assignment.SmclType))
                    throw new TypeCheckingException(
                        "declaration followed by a wrong assignment. "
                        + declaration.Typed.SmclType
                        + " = "
                        + declaration.Assignment.SmclType
                        + "\nThey must be the same type."
                        );
                 */
            }
        }

        public void Visit(Group group)
        {
            foreach (var c in _env.Clients)
            {
                if (c.PartName == group.Name)
                {
                    _env.TmpPartEnv.Groups.Add(group.Id.Name, c);
                    return;
                }
            }
            throw new TypeCheckingException("your server doesn't belong to a valid group --> " + group.Name);
        }

        public void Visit(ExpStmt expStmt)
        {
            expStmt.Exp.Accept(this);
        }

        public void Visit(Take take)
        {
            take.Exp.Accept(this);
            _env.TmpPartEnv.CheckTunnelMethod(take.Id.Name, take.Exp.SmclType);
            take.SmclType = take.Exp.SmclType;
        }

        public void Visit(Get get)
        {
            get.Exp.Accept(this);
            _env.TmpPartEnv.CheckTunnelMethod(get.Id.Name, get.Exp.SmclType);
            get.SmclType = get.Exp.SmclType;
        }

        public void Visit(Put put)
        {
            put.Exp.Accept(this);
            _env.TmpPartEnv.CheckTunnelMethod(put.Id.Name, put.Exp.SmclType);
        }

        public void Visit(Return rreturn)
        {
            if (rreturn.Exp != null)
            {
                rreturn.Exp.Accept(this);
                rreturn.SmclType = rreturn.Exp.SmclType;
            }
            else
            {
                rreturn.SmclType = VoidType;
            }
            SmclType expectedRetType = _env.TmpPartEnv.Functions[_env.TmpFunName].GetReturn();
            MustBe(rreturn.SmclType, expectedRetType, "return a " + rreturn.SmclType + " in a " + expectedRetType + " function");
        }

        public void Visit(ReadInt readInt)
        {
            readInt.SmclType = SintType;
        }

        public void Visit(Open open)
        {
            if (!_env.IsVisitingServer())
                throw new TypeCheckingException("Open(..|..) can be used only within server");
            
            foreach (var id in open.Args)
            {
                SmclType tmpt = _env.TmpPartEnv.Functions[_env.TmpFunName].SymbolTable[id.Name];
                if (! tmpt.IsSecret())
                    throw new TypeCheckingException("open only on secret variables");
                tmpt = ConvertToPublic(tmpt);
                _env.TmpPartEnv.Functions[_env.TmpFunName].SymbolTable[id.Name] = tmpt;
            }
            open.Exp.Accept(this);
            open.SmclType = open.Exp.SmclType;
            foreach (var id in open.Args)
            {
                SmclType tmpt = _env.TmpPartEnv.Functions[_env.TmpFunName].SymbolTable[id.Name];
                tmpt = ConvertToSecret(tmpt);
                _env.TmpPartEnv.Functions[_env.TmpFunName].SymbolTable[id.Name] = tmpt;
            }
        }

        public void Visit(MethodInvocation mi)
        {
            if (!_env.IsVisitingServer())
                throw new TypeCheckingException("FunctionCallyou can invoke methods only from a Server instance.");
            if (_env.TmpPartEnv.Functions[_env.TmpFunName].IdToClient.ContainsKey(mi.Id.Name))
            {
                _env.InvokingOn = _env.TmpPartEnv.Functions[_env.TmpFunName].IdToClient[mi.Id.Name];
                if (!_env.InvokingOn.Functions.ContainsKey(mi.FunctionCall.Name))
                {
                    throw new TypeCheckingException(
                        String.Format(
                            "the client {0} doesn't contain the method {1}",
                            _env.InvokingOn.PartName, mi.FunctionCall.Name
                            ));
                }
                mi.FunctionCall.Accept(this);
                mi.SmclType = mi.FunctionCall.SmclType;
            }
            else
            {
                throw new TypeCheckingException("invoking a method from an undefined variable --> "+mi.Id.Name);
            }
            _env.InvokingOn = null;
        }

        public void Visit(SString sstring)
        {
            sstring.SmclType = new StringType();
        }
    }
}
