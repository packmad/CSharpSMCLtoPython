using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CSharpSMCLtoPython.ASTbuilder;
//using SmclType = CSharpSMCLtoPython.ASTbuilder.SmclType;

namespace CSharpSMCLtoPython.Visitors
{
    internal class ToStringVisitor : ITreeNodeVisitor {
        private readonly StringBuilder _sb = new StringBuilder();
        private int Level { get; set; }

        public string Result 
        {
            get { return _sb.ToString(); }
        }


        private void Indent() 
        {
            for (int i=0; i<Level; i++)
            {
                _sb.Append("|--");
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
            _sb.Append(" && ");
            and.Right.Accept(this);
        }

        public void Visit(Or or)
        {
            or.Left.Accept(this);
            _sb.Append(" || ");
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
            _sb.Append(id.Name);// == null ? id.Typed : id.Var.Typed);
        }

        public void Visit(Display print) 
        {
            /*if (print.Exp.SmclType != null)
                _sb.Append(print.Exp.SmclType.Equals(SmclType.BoolT) ? "print_bool" : "print_int");
            else
                _sb.Append("print");*/
            _sb.Append("display(");
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
            _sb.Append(")");
            ifs.Body.Accept(this);
        }

        public void Visit(While whiles)
        {
            _sb.Append("while (");
            whiles.Guard.Accept(this);
            _sb.Append(")");
            whiles.Body.Accept(this);

        }



        public void Visit(Block block) 
        {
            _sb.Append(" {");
            //Indent();
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
            _sb.Append("}");
                    
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
            _sb.AppendFormat("\n\nfunction {0} {1}(", function.SmclType, function.Name);
            if (function.Params.Any())
            {
                foreach (Typed p in function.Params)
                {
                    p.Accept(this);
                    _sb.Append(", ");
                }
                //_sb.Remove(_sb.Length - 2, 2);
            }
            _sb.Append(")\n{");
            ++Level;
            if (function.Stmts.Any())
            {
                foreach (Stmt s in function.Stmts)
                {
                    _sb.Append("\n");
                    Indent();
                    s.Accept(this);
                }
                //_sb.Remove(_sb.Length - 2, 2);
            }
            _sb.Append("\n}\n");
            --Level;
        }


        public void Visit(Else eelse)
        {
            _sb.Append("else ");
            eelse.Body.Accept(this);
        }

        public void Visit(Typed typed)
        {
            _sb.Append(typed.SmclType);
            _sb.Append(" ");
            typed.Id.Accept(this);
        }

        public void Visit(Assignment assignment)
        {
            assignment.Id.Accept(this);
            _sb.Append(" = ");
            assignment.Exp.Accept(this);
            _sb.Append(";");
        }

        public void Visit(Tunnel tunnel)
        {
            _sb.Append("tunnel of ");
            tunnel.Typed.Accept(this);
        }

        public void Visit(Client client)
        {
            _sb.Append("declare client "+client.Name+" : \n\t");
            foreach (var t in client.Tunnels)
            {
                t.Accept(this);
                _sb.Append(";\n\t");
            }
            foreach (var f in client.Functions)
            {
                f.Accept(this);
            }
        }

        public void Visit(Server server)
        {
            _sb.Append("declare server " + server.Name + " : \n\t");
            foreach (var g in server.Groups)
            {
                g.Accept(this);
                _sb.Append(";\n\t");
            }
            foreach (var f in server.Functions)
            {
                f.Accept(this);
            }
        }

        public void Visit(For ffor)
        {
            _sb.Append("for ( ");
            ffor.Typed.Accept(this);
            _sb.Append(" in ");
            ffor.Id.Accept(this);
            _sb.Append(" )");
            ffor.Body.Accept(this);
        }

        public void Visit(FunctionCall functionCall)
        {
            _sb.Append(functionCall.Name).Append('(');
            if (functionCall.Params.Any())
            {
                foreach (Exp arg in functionCall.Params)
                {
                    arg.Accept(this);
                    _sb.Append(", ");
                }
                //_sb.Remove(_sb.Length - 2, 2);
            }
            if (functionCall.Params.Count == 1)
                _sb.Remove(_sb.Length - 2, 2);
            _sb.Append(')');
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
            _sb.Append(";");
            if (declaration.Assignment != null)
            {
                _sb.Append("\n");
                Indent();
                declaration.Assignment.Accept(this);
            }
        }

        public void Visit(Group group)
        {
            _sb.Append("group of " + group.Name + " ");
            group.Id.Accept(this);
        }

        public void Visit(ExpStmt expStmt)
        {
            expStmt.Exp.Accept(this);
            _sb.Append(";");
        }

        public void Visit(Take take)
        {
            take.Id.Accept(this);
            _sb.Append(".take(");
            take.Exp.Accept(this);
            _sb.Append(")");
        }

        public void Visit(Get get)
        {
            get.Id.Accept(this);
            _sb.Append(".get(");
            get.Exp.Accept(this);
            _sb.Append(")");
        }

        public void Visit(Put put)
        {
            put.Id.Accept(this);
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
            _sb.Append(";");
        }

        public void Visit(ReadInt readInt)
        {
            _sb.Append("readInt()");
        }

        public void Visit(Open open)
        {
            _sb.Append("open (");
            open.Exp.Accept(this);
            _sb.Append(" | ");
            foreach (var a in open.Args)
            {
                a.Accept(this);
            }
            _sb.Append(")");
        }

        public void Visit(MethodInvocation methodInvocation)
        {
            methodInvocation.Id.Accept(this);
            _sb.Append(".");
            methodInvocation.FunctionCall.Accept(this);
        }

        public void Visit(SString sstring)
        {
            _sb.Append(sstring.Value);
        }
    }
}
