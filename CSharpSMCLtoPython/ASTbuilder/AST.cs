using System;
using System.Collections.Generic;
using CSharpSMCLtoPython.Visitors;

namespace CSharpSMCLtoPython.ASTbuilder
{
    //internal enum SmclT { NoneT, IntT, SintT, BoolT, SboolT, StringT, VoidT, ClientT, SclientT, ServerT, GroupT, TunnelT}
    internal enum TunMethodType { NoneM, PutM, GetM, TakeM }

    internal abstract class TreeNode
    {
        internal SmclType SmclType { get; set; }

        public abstract void Accept(ITreeNodeVisitor visitor);

        protected TreeNode()
        {
            SmclType = new NoneType();
        }

        public override string ToString()
        {
            var pv = new ToStringVisitor();
            Accept(pv);
            return pv.Result;
        }
    }

    internal interface ITreeNodeVisitor
    {
        void Visit(Sum sum);
        void Visit(Subtraction sub);
        void Visit(Product prod);
        void Visit(Division div);
        void Visit(Module rem);
        void Visit(And and);
        void Visit(Or or);
        void Visit(Not not);
        void Visit(Equal eq);
        void Visit(LessThan lt);
        void Visit(Id id);
        void Visit(Display print);
        void Visit(EvalExp eval);
        void Visit(If ifs);
        void Visit(While whiles);
        void Visit(Block block);
        void Visit(Prog prog);
        void Visit(GreaterThan greaterThan);
        void Visit(Function function);
        void Visit(Else eelse);
        void Visit(Typed typed);
        void Visit(Assignment assignment);
        void Visit(Tunnel tunnel);
        void Visit(Client client);
        void Visit(Server server);
        void Visit(For ffor);
        void Visit(FunctionCall functionCall);
        void Visit(BoolLiteral boolLiteral);
        void Visit(IntLiteral intLiteral);
        void Visit(Declaration declaration);
        void Visit(Group group);
        void Visit(ExpStmt expStmt);
        void Visit(Take take);
        void Visit(Get get);
        void Visit(Put put);
        void Visit(Return rreturn);
        void Visit(ReadInt readInt);
        void Visit(Open open);
        void Visit(MethodInvocation methodInvocation);
        void Visit(SString sstring);
    }


    internal abstract class Exp : TreeNode  { }
    
    internal abstract class Stmt : TreeNode { }

    internal abstract class Multipart : TreeNode
    {
        private readonly string _name;
        private readonly IList<Function> _functions;

        public string Name
        {
            get { return _name; }
        }

        public IList<Function> Functions
        {
            get { return _functions; }
        }


        protected Multipart(string name, IList<Function> @functions)
        {
            _name = name;
            _functions = @functions;
        }
    }

    internal abstract class TunMethod : Exp
    {
        private readonly Id _id;
        private readonly Exp _exp;

        public Exp Exp
        {
            get { return _exp; }
        }

        public Id Id
        {
            get { return _id; }
        }

        protected TunMethod(Id id, Exp exp)
        {
            _exp = exp;
            _id = id;
        }
    }


    internal abstract class BinaryOp : Exp
    {
        private readonly Exp _left;
        private readonly Exp _right;

        protected BinaryOp(Exp left, Exp right)
        {
            _left = left;
            _right = right;
        }

        public Exp Left
        {
            get { return _left; }
        }

        public Exp Right
        {
            get { return _right; }
        }
    }


    internal class Sum : BinaryOp
    {
        public Sum(Exp left, Exp right) : base(left, right) { }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class Subtraction : BinaryOp
    {
        public Subtraction(Exp left, Exp right) : base(left, right) { }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class Product : BinaryOp
    {
        public Product(Exp left, Exp right) : base(left, right) { }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class Division : BinaryOp
    {
        public Division(Exp left, Exp right) : base(left, right) { }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class Module : BinaryOp
    {
        public Module(Exp left, Exp right) : base(left, right) { }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class And : BinaryOp
    {
        public And(Exp left, Exp right) : base(left, right) {
            // SmclType = SmclType.BOOL_T;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class Or : BinaryOp
    {
        public Or(Exp left, Exp right) : base(left, right) { }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class Equal : BinaryOp
    {
        public Equal(Exp left, Exp right) : base(left, right) { }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class ExpStmt : Stmt
    {
        private readonly Exp _exp ;

        public Exp Exp
        {
            get { return _exp; }
        }

        public ExpStmt(Exp exp)
        {
            _exp = exp;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class FunctionCall : Exp
    {
        private readonly String _name;
        private readonly IList<Exp> _args;

        public string Name
        {
            get { return _name; }
        }

        public IList<Exp> Params
        {
            get { return _args; }
        }

        public FunctionCall(String name, IList<Exp> @args)
        {
            _name = name;
            _args = @args;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class MethodInvocation : Exp
    {
        private readonly Id _id;
        private readonly FunctionCall _functionCall;

        public Id Id { get { return _id; } }
        public FunctionCall FunctionCall { get { return _functionCall; } }

        public MethodInvocation(Id id, FunctionCall functionCall)
        {
            _id = id;
            _functionCall = functionCall;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    internal class LessThan : BinaryOp
    {
        public LessThan(Exp left, Exp right) : base(left, right) { }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class GreaterThan : BinaryOp
    {
        public GreaterThan(Exp left, Exp right) : base(left, right) { }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal abstract class UnaryOperator : Exp
    {
        private readonly Exp _operand;

        protected UnaryOperator(Exp operand)
        {
            _operand = operand;
        }

        public Exp Operand
        {
            get { return _operand; }
        }
    }


    internal class Not : UnaryOperator
    {
        public Not(Exp operand) : base(operand) { }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    internal class Id : Exp
    {
        private readonly string _name;
        public string Name { get { return _name; } }

        public Id(string name)
        {
            _name = name.TrimEnd('.');
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    internal class Typed : Stmt
    {
        private readonly Id _id;

        public Id Id
        {
            get { return _id; }
        }

        public Typed(SmclType smclType, Id id)
        {
            SmclType = smclType;
            _id = id;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return SmclType + " " + _id.Name;
        }
    }


    internal class Display : Stmt
    {
        private readonly Exp _exp;

        public Display(Exp exp)
        {
            _exp = exp;
        }

        public Exp Exp
        {
            get { return _exp; }
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class EvalExp : Stmt
    {
        private readonly Exp _exp;

        public EvalExp(Exp exp)
        {
            _exp = exp;
        }

        public Exp Exp
        {
            get { return _exp; }
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    internal class Tunnel : TreeNode
    {
        private readonly Typed _typed;

        public Typed Typed
        {
            get { return _typed; }
        }


        public Tunnel(Typed typed)
        {
            _typed = typed;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    internal class Client : Multipart
    {
        private readonly IList<Tunnel> _tunnels;

        public IList<Tunnel> Tunnels
        {
            get { return _tunnels; }
        }

        public Client(string name, IList<Tunnel> @tunnels, IList<Function> @functions) : base (name, @functions)
        {
            _tunnels = @tunnels;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    internal class Server: Multipart
    {
        private readonly IList<Group> _groups;

        public IList<Group> Groups
        {
            get { return _groups; }
        }
        
        public Server(string name, IList<Group> @groups, IList<Function> @functions) : base (name, @functions)
        {
            _groups = @groups;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class Group : TreeNode
    {
        private readonly string _name;
        private readonly Id _id;

        public string Name
        {
            get { return _name; }
        }

        public Id Id
        {
            get { return _id; }
        }

        public Group(string name, Id id)
        {
            _name = name;
            _id = id;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    internal class Prog : TreeNode
    {
        private readonly IList<Client> _clients;
        private readonly Server _server;

        public IList<Client> Clients
        {
            get { return _clients; }
        }

        public Server Server
        {
            get { return _server; }
        }

        public Prog(IList<Client> @clients, Server server)
        {
            _clients = @clients;
            _server = server;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    
    internal class Function : TreeNode
    {
        private readonly string _name;
        private readonly IList<Typed> _params;
        private readonly IList<Stmt> _stmts;

        public string Name { get { return _name; } }
        public IList<Typed> Params { get { return _params; } }
        public IList<Stmt> Stmts { get { return _stmts; } }

        public Function(SmclType smclType, string name, IList<Typed> @params, IList<Stmt> @stmts)
        {
            SmclType = smclType;
            _name = name;
            _params = @params;
            _stmts = @stmts;
        }


        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    internal class Block : Stmt
    {
        private readonly IList<Stmt> _statements;
        public IEnumerable<Stmt> Statements { get { return _statements; } }

        public Block(IList<Stmt> statements)
        {
            _statements = statements;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }


    }

    internal class Assignment : Stmt
    {
        private readonly Id _id;
        private readonly Exp _exp;

        public Id Id
        {
            get { return _id; }
        }

        public Exp Exp
        {
            get { return _exp; }
        }

        public Assignment(Id left, Exp right)
        {
            _id = left;
            _exp = right;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class Declaration : Stmt
    {
        private readonly Typed _typed;
        private readonly Assignment _assignment;

        public Typed Typed
        {
            get { return _typed; }
        }

        public Assignment Assignment
        {
            get { return _assignment; }
        }

        public Declaration(Typed typed)
        {
            _typed = typed;
        }

        public Declaration(Typed typed, Assignment assignment)
        {
            _typed = typed;
            _assignment = assignment;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    internal class BoolLiteral : Exp
    {
        private readonly bool _b;

        public bool Value { get { return _b; } }

        public BoolLiteral(bool b)
        {
            _b = b;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    internal class IntLiteral : Exp
    {
        private readonly int _value;

        public int Value { get { return _value; } }

        public IntLiteral(int v)
        {
            _value = v;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class If : Stmt
    {
        private readonly Exp _guard;
        private readonly Stmt _body;

        public If(Exp guard, Stmt body)
        {
            _guard = guard;
            _body = body;
        }

        public Exp Guard
        {
            get { return _guard; }
        }

        public Stmt Body
        {
            get { return _body; }
        }


        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

   
    internal class Else : Stmt
    {
        private readonly Stmt _body;
        public Stmt Body { get { return _body; } }

        public Else (Stmt body)
        {
            _body = body;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class While : Stmt
    {
        private readonly Exp _guard;
        private readonly Stmt _body;

        public While(Exp guard, Stmt body)
        {
            _guard = guard;
            _body = body;
        }

        public Exp Guard
        {
            get { return _guard; }
        }

        public Stmt Body
        {
            get { return _body; }
        }


        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class For : Stmt
    {

        private readonly Typed _typed;
        private readonly Id _id;
        private readonly Stmt _body;

        public Typed Typed { get { return _typed; } }
        public Id Id { get { return _id; } }
        public Stmt Body { get { return _body; } }

        public For(Typed typed, Id id, Stmt body)
        {
            _typed = typed;
            _id = id;
            _body = body;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class Args
    {
        private readonly List<String> _argsList;

        public List<string> ArgsList
        {
            get { return _argsList; }
        }

        public Args()
        {
            _argsList = new List<String>();
        }

    }


    internal class Put : TunMethod
    {
        public Put(Id id, Exp exp) : base(id, exp) { }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    internal class Get : TunMethod
    {
        public Get(Id id, Exp exp) : base(id, exp) { }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class Take : TunMethod
    {
        public Take(Id id, Exp exp) : base(id, exp) { }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    internal class Return : Stmt
    {
        private readonly Exp _exp;
        public Exp Exp { get { return _exp; } }

        public Return(Exp exp)
        {
            _exp = exp;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class ReadInt : Exp
    {

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class Open : Exp
    {
        private readonly Exp _exp;
        private readonly IList<Id> _args;

        public Exp Exp { get { return _exp; } }
        public IList<Id> Args { get { return _args; } }

        public Open(Exp exp, IList<Id> args)
        {
            _exp = exp;
            _args = args;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    internal class SString : Exp
    {
        private readonly string _value;
        public string Value { get { return _value; } }

        public SString(string value)
        {
            _value = value;
        }

        public override void Accept(ITreeNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

}
