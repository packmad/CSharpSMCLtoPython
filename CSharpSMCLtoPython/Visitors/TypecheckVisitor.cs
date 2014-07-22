using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using CSharpSMCLtoPython.ASTbuilder;


namespace CSharpSMCLtoPython.Visitors
{
    [Serializable]
    public class TypeCheckingException : Exception
    {
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
            throw new TypeCheckingException("'" + id + "' is never defined.");
        }

        public SmclType GetReturn()
        {
            return Function.SmclType;
        }
    }


    internal abstract class PartEnv
    {
        public string PartName { get; private set; }
        public Dictionary<string, FuncEnv> Functions = new Dictionary<string, FuncEnv>(); // name -> env
        public Dictionary<string, SmclType> Tunnels;
        public Dictionary<string, PartEnv> Groups; // e.g. mills -> Millionaries
        
        public readonly Multipart Mp;

        protected PartEnv(Multipart mp)
        {
            Mp = mp;
            PartName = mp.Name;
        }

        public void Add(Function fun)
        {
            if (Functions.Values.Any(f => f.Function.Name == fun.Name))
            {
                throw new TypeCheckingException("There are two functions with the same name: " + fun.Name);
            }
            Functions.Add(fun.Name, new FuncEnv(fun));

            foreach (var p in fun.Params)
            {
                // when add a function all its params are inserted in the symbol table
                Functions[fun.Name].SymbolTable.Add(p.Id.Name, p.SmclType);
            }

        }
    }

    internal class PartEnvClient : PartEnv
    {
        public PartEnvClient(Multipart mp) : base(mp)
        {
            Tunnels = new Dictionary<string, SmclType>();
        }

        public void CheckTunnelMethod(string id, SmclType expType)
        {
            if (!Tunnels.ContainsKey(id))
                throw new TypeCheckingException(id + " isn't defined as tunnel.");

            if (!Tunnels[id].Equals(expType))
                throw new TypeCheckingException(
                    String.Format(
                        "the tunnel {0} has type {1} but is invoked with a {2} expression.",
                        id, Tunnels[id], expType
                        )
                );
        }
    }

    internal class PartEnvServer : PartEnv
    {
        public PartEnvServer(Multipart mp) : base(mp)
        {
            Groups = new Dictionary<string, PartEnv>();
        }
    }



    internal class Env
    {
        public List<PartEnv> Clients = new List<PartEnv>();
        public PartEnv Server { get; set; }

        public PartEnv VisitPartEnv { get; set; }
        public PartEnv InvokingOn { get; set; } // used for method invocation
        public string VisitFunName { get; set; }
        public bool FakeEnvForTunnel { get; set; } // set when is visiting server but env is changed for typecheck

        public void Add(Client c)
        {
            PartEnv ce = new PartEnvClient(c);
            Clients.Add(ce);
            VisitPartEnv = ce;
        }

        public void Add(Server s)
        {
            PartEnv ce = new PartEnvServer(s);
            Server = ce;
            VisitPartEnv = ce;
        }

        public bool IsVisitingServer()
        {
            return VisitPartEnv.Mp.GetType() == typeof(Server);
        }

        public bool VariableAlreadyDefined(string idName)
        {
            if (VisitFunName == null)
                return false;
            return VisitPartEnv.Functions[VisitFunName].SymbolTable.ContainsKey(idName);
        }

        public SmclType GetMyTypeFromId(string idName)
        {
            if (IsVisitingServer())
            {
                if (VisitPartEnv.Groups.ContainsKey(idName))
                {
                    return new ClientType();
                }
            }
            else
            {
                if (VisitPartEnv.Tunnels.ContainsKey(idName))
                    return new TunnelType();
            }
            return VisitPartEnv.Functions[VisitFunName].GetTypeFromId(idName);
        }

        public void AddSymbolInFunction(string id, SmclType type)
        {
            if (IsVisitingServer())
            {
                Server s = (Server)VisitPartEnv.Mp;
                foreach (var group in s.Groups)
                {
                    if (group.Id.Name == id)
                        throw new TypeCheckingException("there's a group with the same id --> '" + id + "'");
                }
            }
            else
            {

                Client c = (Client)VisitPartEnv.Mp;
                foreach (var tunnel in c.Tunnels)
                {
                    if (tunnel.Typed.Id.Name == id)
                        throw new TypeCheckingException("there's a tunnel with the same id --> '" + id + "'");
                }
            }
            if (!VisitPartEnv.Functions.ContainsKey(VisitFunName))
                throw new TypeCheckingException("typechecker logic fails in AddSymbolInFunction function.");
            if (VisitPartEnv.Functions[VisitFunName].SymbolTable.ContainsKey(id))
                throw new TypeCheckingException("this id is already defined --> '" + id + "'");
            VisitPartEnv.Functions[VisitFunName].SymbolTable.Add(id, type);
        }

        public void RemoveSymbolInFunction(string id)
        {
            if (!VisitPartEnv.Functions.ContainsKey(VisitFunName))
                throw new TypeCheckingException("typechecker logic fails in RemoveSymbolInFunction function.");
            VisitPartEnv.Functions[VisitFunName].SymbolTable.Remove(id);
        }

        public void AddClientForMethodInvocation(Typed var, PartEnv client)
        {
            if (!IsVisitingServer())
                throw new TypeCheckingException("you can invoke methods only from a Server instance --> " + var.Id.Name);
            AddSymbolInFunction(var.Id.Name, var.SmclType);
            VisitPartEnv.Functions[VisitFunName].IdToClient.Add(var.Id.Name, client);
        }

        public void RemoveClientForMethodInvocation(Typed var)
        {
            RemoveSymbolInFunction(var.Id.Name);
            VisitPartEnv.Functions[VisitFunName].IdToClient.Remove(var.Id.Name);
        }
    }


    internal class TypecheckVisitor : ITreeNodeVisitor
    {
        private readonly Env _env = new Env();
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


        private static SmclType ConvertToSecret(SmclType t)
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

        private static void MustBe(SmclType t, SmclType mustBe, string msg)
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
            /*
            if (!print.Exp.SmclType.Equals(StringType))
            {
                throw new TypeCheckingException("You can only print strings, not " + print.Exp.SmclType);
            }
             */
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
            _env.VisitPartEnv.Add(function);
            _env.VisitFunName = function.Name;
            foreach (var s in function.Stmts)
            {
                s.Accept(this);
            }
            _env.VisitFunName = null;
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
            tunnel.Typed.Accept(this);
            if (_env.VisitPartEnv.Tunnels.ContainsKey(tunnel.Typed.Id.Name))
                throw new TypeCheckingException("there's another tunnel with the same name --> " + tunnel.Typed.Id.Name);
            _env.VisitPartEnv.Tunnels.Add(tunnel.Typed.Id.Name, tunnel.Typed.SmclType);
            tunnel.SmclType = new TunnelType(tunnel.Typed.SmclType);
        }

        public void Visit(Client client)
        {
            _env.Add(client);
            client.SmclType = ClientType;
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
            server.SmclType = ServerType;
            foreach (var g in server.Groups)
            {
                g.Accept(this);
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
            foreach (var g in _env.VisitPartEnv.Groups.Keys)
            {
                if (g == ffor.Id.Name)
                {
                    _env.AddClientForMethodInvocation(ffor.Typed, _env.VisitPartEnv.Groups[g]);
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
                if (_env.VisitPartEnv.Functions.Keys.All(fName => functionCall.Name != fName))
                {
                    throw new TypeCheckingException(functionCall.Name + " isn't defined.\n");
                }
                targetFunction = _env.VisitPartEnv.Functions[functionCall.Name];
                targetFunction.ArgsMatchParamsType(functionCall.Params);
            }
            if (_env.InvokingOn != null)
            {
                targetFunction = _env.InvokingOn.Functions[functionCall.Name];
                targetFunction.ArgsMatchParamsType(functionCall.Params);
            }
            if (targetFunction != null)
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
            }
        }

        public void Visit(Group group)
        {
            group.SmclType = GroupType;
            foreach (var c in _env.Clients)
            {
                if (c.PartName == group.Name)
                {
                    _env.VisitPartEnv.Groups.Add(group.Id.Name, c);
                    return;
                }
            }
            throw new TypeCheckingException("your server doesn't belong to a valid group --> " + group.Name);
        }

        public void Visit(ExpStmt expStmt)
        {
            expStmt.Exp.Accept(this);
        }

        // is blocking and if the tunnel is empty waits until a value becomes available
        public void Visit(Take take)
        {
            // nothing to do!
        }

        // is non-blocking and if the tunnel is empty returns the special value Null
        public void Visit(Get get)
        {
            // nothing to do!
        }

        // Values may be placed in the tunnel using
        public void Visit(Put put)
        {
            put.Exp.Accept(this);
            put.SmclType = put.Exp.SmclType;
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
            SmclType expectedRetType = _env.VisitPartEnv.Functions[_env.VisitFunName].GetReturn();
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
                SmclType tmpt = _env.GetMyTypeFromId(id.Name);

                if (!tmpt.IsSecret())
                    throw new TypeCheckingException("open only on secret variables --> " + id.Name);
                tmpt = ConvertToPublic(tmpt);
                _env.VisitPartEnv.Functions[_env.VisitFunName].SymbolTable[id.Name] = tmpt;
            }
            open.Exp.Accept(this);
            open.SmclType = open.Exp.SmclType;
            foreach (var id in open.Args)
            {
                SmclType tmpt = _env.VisitPartEnv.Functions[_env.VisitFunName].SymbolTable[id.Name];
                tmpt = ConvertToSecret(tmpt);
                _env.VisitPartEnv.Functions[_env.VisitFunName].SymbolTable[id.Name] = tmpt;
            }
        }

        public void Visit(MethodInvocation mi)
        {
            if (!_env.IsVisitingServer())
                throw new TypeCheckingException("you can invoke methods only from a Server instance.");
            if (_env.VisitPartEnv.Functions[_env.VisitFunName].IdToClient.ContainsKey(mi.Id.Name))
            {
                _env.InvokingOn = _env.VisitPartEnv.Functions[_env.VisitFunName].IdToClient[mi.Id.Name];
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
                throw new TypeCheckingException("invoking a method from an undefined variable --> " + mi.Id.Name);
            }
            _env.InvokingOn = null;
        }

        public void Visit(SString sstring)
        {
            sstring.SmclType = new StringType();
        }

        public void Visit(DotClient dotClient)
        {
            if (!_env.IsVisitingServer())
                throw new TypeCheckingException("can't access a client '" + dotClient.ClientId + "' from a client '" + _env.VisitPartEnv.PartName + "'.");
            var x = _env.VisitPartEnv.Functions[_env.VisitFunName].IdToClient[dotClient.ClientId.Name];
            foreach (var c in _env.Clients)
            {
                if (c.PartName == x.PartName)
                {
                    PartEnv tmp = _env.VisitPartEnv;
                    _env.VisitPartEnv = c;
                    _env.FakeEnvForTunnel = true;
                    dotClient.TunMethodCall.Accept(this);
                    _env.VisitPartEnv = tmp;
                    _env.FakeEnvForTunnel = false;
                    dotClient.SmclType = dotClient.TunMethodCall.TunMethod.SmclType;
                    return;
                }

            }
            throw new TypeCheckingException("can't use a dot operator on a non-client '" + dotClient.ClientId.Name + "'");
        }

        public void Visit(TunMethodCall tunMethodCall)
        {
            tunMethodCall.TunMethod.Accept(this);
            if (tunMethodCall.TunMethod.GetType() == typeof (Put))
            {
                if (_env.FakeEnvForTunnel)
                    throw new TypeCheckingException(
                        "Tunnels are mono-directional: you can't use the put(...) method within server");
                tunMethodCall.SmclType = tunMethodCall.TunMethod.SmclType;
            }
            else
                tunMethodCall.SmclType =
                    tunMethodCall.TunMethod.SmclType = _env.VisitPartEnv.Tunnels[tunMethodCall.Id.Name];
            ((PartEnvClient)_env.VisitPartEnv).CheckTunnelMethod(tunMethodCall.Id.Name, tunMethodCall.TunMethod.SmclType);
        }
    }
}
