using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CSharpSMCLtoPython.ASTbuilder;


namespace CSharpSMCLtoPython.Visitors
{
    internal class ToPythonVisitor : ITreeNodeVisitor {
        private readonly StringBuilder _sb = new StringBuilder();
        private int Level { get; set; }
        private const string _separator =
            "#-----------------------------------------------------------------------------#\n";
        private string _className = "";
        private readonly string _xmlConfigPath;
        private readonly string _clientPySourcePath;
        private readonly string _clientMainSourcePath;
        private readonly string _serverPySourcePath;
        private readonly string _serverMainSourcePath;
        private bool _visitingServer = false;
        private bool _methInvocation = false;
        private bool _noTypeFlag; // set it for don't print var type

        public string Result 
        {
            get
            {
                return _sb.ToString();
            }
        }

        public ToPythonVisitor(
            string xmlConfigPath, 
            string clientPySourcePath, 
            string clientMainSourcePath, 
            string serverPySourcePath,
            string serverMainSourcePath)
        {
            _xmlConfigPath = xmlConfigPath;
            _clientPySourcePath = clientPySourcePath;
            _clientMainSourcePath = clientMainSourcePath;
            _serverPySourcePath = serverPySourcePath;
            _serverMainSourcePath = serverMainSourcePath;
        }


        private void Indent() 
        {
            for (int i=0; i<Level; i++)
            {
                _sb.Append("    ");
            }
        }

        public void Visit(Sum sum) 
        {
            sum.Left.Accept(this);
            _sb.Append("+");
            sum.Right.Accept(this);
        }

        public void Visit(Subtraction sub)
        {
            sub.Left.Accept(this);
            _sb.Append("-");
            sub.Right.Accept(this);
        }

        public void Visit(Product prod) 
        {
            prod.Left.Accept(this);
            _sb.Append("*");
            prod.Right.Accept(this);
        }

        public void Visit(Division div) 
        {
            div.Left.Accept(this);
            _sb.Append("/");
            div.Right.Accept(this);
        }

        public void Visit(Module rem) 
        {
            rem.Left.Accept(this);
            _sb.Append("%");
            rem.Right.Accept(this);
        }

        public void Visit(And and) 
        {
            and.Left.Accept(this);
            _sb.Append(" and ");
            and.Right.Accept(this);
        }

        public void Visit(Or or)
        {
            or.Left.Accept(this);
            _sb.Append(" or ");
            or.Right.Accept(this);
        }

        public void Visit(Not not) 
        {
            _sb.Append(" not ");
            not.Operand.Accept(this);
        }

        public void Visit(Equal eq) 
        {
            eq.Left.Accept(this);
            _sb.Append("==");
            _noTypeFlag = true;
            Debug.Assert(eq.Left.SmclType.Equals(eq.Right.SmclType));
            if (!_noTypeFlag)
                _sb.Append(eq.Left.SmclType).Append(' ');
            eq.Right.Accept(this);
            _noTypeFlag = false;
        }

        public void Visit(GreaterThan gt)
        {
            gt.Left.Accept(this);
            _sb.Append(" > ");
            gt.Right.Accept(this);
        }

        public void Visit(LessThan lt) 
        {
            lt.Left.Accept(this);
            _sb.Append(" < ");
            lt.Right.Accept(this);
        }


        public void Visit(Id id) 
        {
            _sb.Append(id.Name);
        }

        public void Visit(Display print) 
        {
            _sb.Append("print(");
            print.Exp.Accept(this);
            _sb.Append(")");
        }

        public void Visit(EvalExp eval) 
        {
            eval.Exp.Accept(this);
        }

        public void Visit(If ifs) {
            _sb.Append("if (");
            ifs.Guard.Accept(this);
            _sb.Append(") :");
            ifs.Body.Accept(this);
        }

        public void Visit(While whiles)
        {
            _sb.Append("while (");
            whiles.Guard.Accept(this);
            _sb.Append(") :");
            whiles.Body.Accept(this);

        }

        public void Visit(Block block) 
        {
            //_sb.Append(" {");
            ++Level;
            foreach (var stmt in block.Statements)
            {
                _sb.Append("\n");
                Indent();
                stmt.Accept(this);
                
            }
            _sb.Append("\n");
            --Level;  
            Indent();
            //_sb.Append("}");
                    
        }

        private void appendClientPyMain()
        {
            _sb.Append("xmlConfigPath = r'" + _xmlConfigPath + "'\n");
            try
            {
                using (StreamReader sr = new StreamReader(_clientMainSourcePath))
                {
                    _sb.Append(sr.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The client's python main file could not be read:");
                Console.WriteLine(e.Message);
            }
        }

        private void appendServerPyMain()
        {
            _sb.Append("xmlConfigPath = r'" + _xmlConfigPath + "'\n");
            try
            {
                using (StreamReader sr = new StreamReader(_serverMainSourcePath))
                {
                    _sb.Append(sr.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The server's python main file could not be read:");
                Console.WriteLine(e.Message);
            }
        }


        public void Visit(Prog prog) 
        {
            try
            {
                using (StreamReader sr = new StreamReader(_clientPySourcePath))
                {
                    _sb.Append(sr.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The client python support file could not be read:");
                Console.WriteLine(e.Message);
            }
            _sb.Append("#BEGIN AUTO-GEN CLASS FOR CLIENT\n" + _separator);

            foreach (var c in prog.Clients)
            {
                c.Accept(this);
                _sb.Append("\n\n");
            }
            _sb.Append(_separator + "#END AUTO-GEN CLASS FOR CLIENT\n\n");

            appendClientPyMain();
            _sb.Append("\n\n#ENDOFCLIENTSDEF\n\n");
            try
            {
                using (StreamReader sr = new StreamReader(_serverPySourcePath))
                {
                    _sb.Append(sr.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The server python support file could not be read:");
                Console.WriteLine(e.Message);
            }
            _sb.Append("#BEGIN AUTO-GEN CLASS FOR SERVER\n" + _separator);
            _visitingServer = true;
            prog.Server.Accept(this);
            _sb.Append(_separator + "#END AUTO-GEN CLASS FOR SERVER\n\n");
            appendServerPyMain();
        }


        public void Visit(Function function)
        {
            _sb.AppendFormat("\n");
            Indent();
            _sb.AppendFormat("def " + function.Name + "(self");
            if (function.Params.Any())
            {
                _sb.AppendFormat(", ");
                _noTypeFlag = true;
                foreach (Typed p in function.Params)
                {
                    p.Accept(this);
                    _sb.Append(", ");
                }
                _sb.Remove(_sb.Length-2, 2);
                _noTypeFlag = false;
            }
            _sb.Append("):");
            ++Level;
            if (function.Stmts.Any())
            {
                foreach (Stmt s in function.Stmts)
                {
                    _sb.Append("\n");
                    Indent();
                    s.Accept(this);
                }
            }
            _sb.Append("\n");
            Indent();
            _sb.Append("pass\n");
            --Level;
        }

        public void Visit(Else eelse)
        {
            _sb.Append("else: ");
            eelse.Body.Accept(this);
        }

        public void Visit(Typed typed)
        {
            //if (_noTypeFlag)
            typed.Id.Accept(this);
        }

        public void Visit(Assignment assignment)
        {
            assignment.Id.Accept(this);
            _sb.Append(" = ");
            _noTypeFlag = true;
            assignment.Exp.Accept(this);
        }

        public void Visit(Tunnel tunnel)
        {
            _sb.Append("self.addTunnel(Tunnel('" + tunnel.Typed.Id);
            switch (tunnel.Typed.SmclType.SmclT)
            {
                case SmclT.IntT:
                case SmclT.SintT:
                    _sb.Append("', 0))");
                    break;
                case SmclT.BoolT:
                case SmclT.SboolT:
                    _sb.Append("', False))");
                    break;
                default:
                    throw new Exception("Visitor logic fails.");
            }
            _sb.Append("\n");
        }

        public void Visit(Client client)
        {
            _className = client.Name;
            _sb.Append("class " + _className + "(Client): \n");
            Level++;
            Indent();
            _sb.Append("def __init__(self, cliAddr, srvAddr):\n");
            Level++;
            Indent(); 
            _sb.Append("super(" + _className + ", self).__init__(cliAddr, srvAddr)\n");


            foreach (var t in client.Tunnels)
            {
                Indent();
                t.Accept(this);
                //_sb.Append("\n\t");
            }
            Level--;


            foreach (var f in client.Functions)
            {
                f.Accept(this);
            }
            Level--;
        }

        public void Visit(Server server)
        {
            _className = server.Name;
            _sb.Append("class " + _className + "(Server): \n");
            ++Level;
            Indent();
            _sb.Append("def __init__(self, srvAddr, nPlayers, xmlConfigPath):\n");
            ++Level;
            Indent();
            _sb.Append("gpl = [");

            foreach (var g in server.Groups)
            {
                g.Accept(this);
                _sb.Append(", ");
            }
            _sb.Remove(_sb.Length - 2, 2);
            _sb.Append("]\n");
            Indent();
            _sb.Append("super(" + _className + ", self).__init__(srvAddr, gpl, xmlConfigPath, nPlayers)\n");
            --Level;
            foreach (var f in server.Functions)
            {
                f.Accept(this);
            }
            --Level;
        }

        public void Visit(For ffor)
        {
            _sb.Append("for ");
            _noTypeFlag = true;
            ffor.Typed.Accept(this);
            _noTypeFlag = false;
            _sb.Append(" in self.groups['");
            ffor.Id.Accept(this);
            _sb.Append("']:");
            ffor.Body.Accept(this);
        }

        public void Visit(FunctionCall functionCall)
        {
            Level++;


            if (!_methInvocation)
            {
                _sb.Append("self.");
            }
            if (_methInvocation &&_visitingServer)
            {
                _noTypeFlag = false;
                _sb.Append("remoteMethodCall('" + functionCall.Name + ",");

                if (functionCall.Params.Any())
                {
                    foreach (Exp arg in functionCall.Params)
                    {
                        arg.Accept(this);
                        _sb.Append(",");
                    }
                }
                _sb.Append("')");
            }
            else
            {
                _sb.Append(functionCall.Name).Append('(');
                if (functionCall.Params.Any())
                {
                    foreach (Exp arg in functionCall.Params)
                    {
                        arg.Accept(this);
                        _sb.Append(", ");
                    }
                    _sb.Remove(_sb.Length - 2, 2);
                }
                if (functionCall.Params.Count == 1)
                    _sb.Remove(_sb.Length - 2, 2);
                _sb.Append(')');
            }

            Level--;
        }

        public void Visit(BoolLiteral boolLiteral)
        {
            if (!_noTypeFlag)
                _sb.Append(boolLiteral.SmclType + " ");
            _sb.Append(boolLiteral.Value);
        }

        public void Visit(IntLiteral intLiteral)
        {
            if (!_noTypeFlag)
                _sb.Append(intLiteral.SmclType + " ");
            _sb.Append(intLiteral.Value);
        }

        public void Visit(Declaration declaration)
        {
            if (declaration.Assignment != null)
            {
                _sb.Append("\n");
                Indent();
                declaration.Assignment.Accept(this);
            }
            else
            {
                declaration.Typed.Accept(this);
                _sb.Append(" = None\n");
            }
        }

        public void Visit(Group group)
        {
            _sb.Append("('" + group.Name + "', '" + group.Id.Name + "')");
        }

        public void Visit(ExpStmt expStmt)
        {
            expStmt.Exp.Accept(this);
        }

        public void Visit(Take take)
        {
            _sb.Append(_visitingServer ? "take" : "take()");
        }

        public void Visit(Get get)
        {
            _sb.Append(_visitingServer ? "get" : "get()");
        }

        public void Visit(Put put)
        {
            _sb.Append("put(");
            put.Exp.Accept(this);
            _sb.Append(")");
        }

        public void Visit(Return rreturn)
        {
            _sb.Append("return");
            if (rreturn.Exp != null)
            {
                _sb.Append(" ");
                rreturn.Exp.Accept(this);
            }
        }

        public void Visit(ReadInt readInt)
        {
            _sb.Append("int(input(\"readInt: \"))");
        }

        public void Visit(Open open)
        {
            //_sb.Append("open (");
            open.Exp.Accept(this);
            /*
            _sb.Append(" | ");
            foreach (var a in open.Args)
            {
                a.Accept(this);
            }
            _sb.Append(")");
             */
        }

        public void Visit(MethodInvocation classDot)
        {
            _methInvocation = true;
            classDot.Id.Accept(this);
            _sb.Append(".");
            classDot.FunctionCall.Accept(this);
            _methInvocation = false;
        }

        public void Visit(SString sstring)
        {
            _sb.Append(sstring.Value);
        }

        public void Visit(DotClient methodInvocation)
        {
            _sb.Append(methodInvocation.ClientId.Name + ".");
            methodInvocation.TunMethodCall.Accept(this);
        }

        public void Visit(TunMethodCall tunMethodCall)
        {
            if (_visitingServer)
            {
                _sb.Append("remoteTunnelMethod('");
                tunMethodCall.Id.Accept(this);
                _sb.Append("', '");
                tunMethodCall.TunMethod.Accept(this);
                _sb.Append("')");
            }
            else
            {
                _sb.Append("self.tunnels['");
                tunMethodCall.Id.Accept(this);
                _sb.Append("'].");
                tunMethodCall.TunMethod.Accept(this);
                //.Append("()\n");
            }
            
        }
    }
}
