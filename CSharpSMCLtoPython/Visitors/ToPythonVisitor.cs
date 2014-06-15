using System.Diagnostics;
using System.Linq;
using System.Text;
using CSharpSMCLtoPython.ASTbuilder;


namespace CSharpSMCLtoPython.Visitors
{
    internal class ToPythonVisitor : ITreeNodeVisitor {
        private readonly StringBuilder _sb = new StringBuilder();
        private int Level { get; set; }
        private bool _typeFlag;

        public string Result 
        {
            get
            {
                const string runner =
                    "parser = OptionParser()\n"+
                    "Toft05Runtime.add_options(parser)\n"+
                    "options, args = parser.parse_args()\n"+
                    "if len(args) == 0:\n"+
                    "\tparser.error(\"you must specify a config file\")\n"+
                    "else:\n"+
                    "\tid, players = load_config(args[0])\n"+
                    "pre_runtime = create_runtime(id, players, 1, options, Toft05Runtime)\n"+
                    "pre_runtime.addCallback(Protocol)\n"+
                    "reactor.run()\n";
                _sb.Append(runner);
                return _sb.ToString();
            }
        }

        public ToPythonVisitor()
        {
            const string imports =
                "from optparse import OptionParser\n" +
                "import viff.reactor\n" +
                "viff.reactor.install()\n" +
                "from twisted.internet import reactor\n" +
                "from viff.field import GF\n" +
                "from viff.runtime import create_runtime, gather_shares\n" +
                "from viff.comparison import Toft05Runtime\n" +
                "from viff.config import load_config\n" +
                "from viff.util import rand, find_prime\n\n";
            _sb.Append(imports);
        }


        private void Indent() 
        {
            for (int i=0; i<Level; i++)
            {
                _sb.Append("\t");
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
            _sb.Append("~");
            not.Operand.Accept(this);
        }

        public void Visit(Equal eq) 
        {
            eq.Left.Accept(this);
            _sb.Append("==");
            //if (eq.SmclType != null) {
                Debug.Assert(eq.Left.SmclType.Equals(eq.Right.SmclType));
                _sb.Append(eq.Left.SmclType).Append(' ');
            //}
            eq.Right.Accept(this);
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


        public void Visit(Prog prog) 
        {
            foreach (var c in prog.Clients)
            {
                c.Accept(this);
                _sb.Append("\n\n");
            }
            prog.Server.Accept(this);
        }


        public void Visit(Function function)
        {
            _sb.AppendFormat("\n");
            Indent();
            _sb.AppendFormat("def " + function.Name + "(");
            if (function.Params.Any())
            {
                _typeFlag = true;
                foreach (Typed p in function.Params)
                {
                    p.Accept(this);
                    _sb.Append(", ");
                }
                _sb.Remove(_sb.Length-2, 2);
                _typeFlag = false;
            }
            _sb.Append("):\n");
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
            _sb.Append("\n\n");
            --Level;
        }

        public void Visit(Else eelse)
        {
            _sb.Append("else: ");
            eelse.Body.Accept(this);
        }

        public void Visit(Typed typed)
        {
            /*
            _sb.Append(typed.SmclType);
            _sb.Append(" ");
             */
            if (_typeFlag)
                typed.Id.Accept(this);
        }

        public void Visit(Assignment assignment)
        {
            assignment.Id.Accept(this);
            _sb.Append(" = ");
            assignment.Exp.Accept(this);
        }

        public void Visit(Tunnel tunnel)
        {
            /* TODO
            _sb.Append("tunnel of ");
            tunnel.Typed.Accept(this);
             */
        }

        public void Visit(Client client)
        {
            _sb.Append("#MULTIPART\nclass "+client.Name+" : \n\t");

            foreach (var t in client.Tunnels)
            {
                t.Accept(this);
                _sb.Append("\n\t");
            }
            Level++;
            foreach (var f in client.Functions)
            {
                f.Accept(this);
            }
            Level--;
        }

        public void Visit(Server server)
        {
            _sb.Append("#MULTIPART\nclass " + server.Name + " : \n\t");
            foreach (var g in server.Groups)
            {
                g.Accept(this);
                _sb.Append("\n\t");
            }
            Level++;
            foreach (var f in server.Functions)
            {
                f.Accept(this);
            }
            Level--;
        }

        public void Visit(For ffor)
        {
            _sb.Append("for ");
            _typeFlag = true;
            ffor.Typed.Accept(this);
            _typeFlag = false;
            _sb.Append(" in ");
            ffor.Id.Accept(this);
            _sb.Append(":");
            ffor.Body.Accept(this);
        }

        public void Visit(FunctionCall functionCall)
        {
            Level++;
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
            Level--;
        }

        public void Visit(BoolLiteral boolLiteral)
        {
            _sb.Append(boolLiteral.Value);
        }

        public void Visit(IntLiteral intLiteral)
        {
            _sb.Append(intLiteral.Value);
        }

        public void Visit(Declaration declaration)
        {
            declaration.Typed.Accept(this);
            if (declaration.Assignment != null)
            {
                _sb.Append("\n");
                Indent();
                declaration.Assignment.Accept(this);
            }
        }

        public void Visit(Group group)
        {
            /* TODO
            _sb.Append("group of " + group.Name + " ");
            group.Id.Accept(this);
             */
        }

        public void Visit(ExpStmt expStmt)
        {
            expStmt.Exp.Accept(this);
        }

        public void Visit(Take take)
        {
            _sb.Append(".take()");
        }

        public void Visit(Get get)
        {
            _sb.Append(".get()");
        }

        public void Visit(Put put)
        {
            _sb.Append(".put(");
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
            _sb.Append("int(raw_input(\"readInt: \"))");
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
            classDot.Id.Accept(this);
            _sb.Append(".");
            classDot.FunctionCall.Accept(this);
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
            tunMethodCall.Id.Accept(this);
            tunMethodCall.TunMethod.Accept(this);
        }
    }
}
