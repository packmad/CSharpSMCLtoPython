using System;

namespace CSharpSMCLtoPython.ASTbuilder
{
    internal enum SmclT
    {
        NoneT,
        IntT,
        SintT,
        BoolT,
        SboolT,
        StringT,
        VoidT,
        ClientT,
        SclientT,
        ServerT,
        GroupT,
        TunnelT
    }


    internal abstract class SmclType
    {
        public abstract SmclT SmclT { get; protected set; }
        public abstract string Name { get; protected set; }

        public abstract Type GetPublic();
        public abstract Type GetSecret();

        public bool IsSecret()
        {
            return GetPublic() != null;
        }

        public override string ToString()
        {
            return Name;
        }
    }


    internal class NoneType : SmclType
    {
        public override sealed SmclT SmclT { get; protected set; }
        public override sealed string Name { get; protected set; }

        public NoneType()
        {
            SmclT = SmclT.NoneT;
            Name = "none";
        }

        public override bool Equals(object obj)
        {
            return obj.GetType() == typeof (NoneType);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ SmclT.GetHashCode();
        }

        public override Type GetPublic()
        {
            return null;
        }

        public override Type GetSecret()
        {
            return null;
        }
    }


    internal class IntType : SmclType
    {
        public override sealed SmclT SmclT { get; protected set; }
        public override sealed string Name { get; protected set; }

        public IntType()
        {
            SmclT = SmclT.IntT;
            Name = "int";
        }

        public override bool Equals(object obj)
        {
            return obj.GetType() == typeof(IntType);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ SmclT.GetHashCode();
        }

        public override Type GetPublic()
        {
            return null;
        }

        public override Type GetSecret()
        {
            return typeof (SintType);
        }
    }


    internal class SintType : IntType
    {
        public SintType()
        {
            SmclT = SmclT.IntT;
            Name = "sint";
        }

        public override bool Equals(object obj)
        {

            return obj.GetType() == typeof (IntType) || obj.GetType() == typeof (SintType);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ SmclT.GetHashCode();
        }

        public override Type GetPublic()
        {
            return typeof (IntType);
        }

        public override Type GetSecret()
        {
            return null;
        }
    }


    internal class BoolType : SmclType
    {
        public override sealed SmclT SmclT { get; protected set; }
        public override sealed string Name { get; protected set; }

        public BoolType()
        {
            SmclT = SmclT.BoolT;
            Name = "bool";
        }

        public override bool Equals(object obj)
        {
            return obj.GetType() == typeof(BoolType);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ SmclT.GetHashCode();
        }

        public override Type GetPublic()
        {
            return null;
        }

        public override Type GetSecret()
        {
            return typeof (SclientType);
        }
    }


    internal class SboolType : BoolType
    {
        public SboolType()
        {
            SmclT = SmclT.SboolT;
            Name = "sbool";
        }

        public override bool Equals(object obj)
        {
            return obj.GetType() == typeof(BoolType) || obj.GetType() == typeof(SboolType);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ SmclT.GetHashCode();
        }

        public override Type GetPublic()
        {
            return typeof(BoolType);
        }

        public override Type GetSecret()
        {
            return null;
        }
    }

    internal class StringType : SmclType
    {
        public override sealed SmclT SmclT { get; protected set; }
        public override sealed string Name { get; protected set; }

        public StringType()
        {
            SmclT = SmclT.StringT;
            Name = "string";
        }

        public override bool Equals(object obj)
        {
            return obj.GetType() == typeof(StringType);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ SmclT.GetHashCode();
        }

        public override Type GetPublic()
        {
            return null;
        }

        public override Type GetSecret()
        {
            return null;
        }
    }

    internal class VoidType : SmclType
    {
        public override sealed SmclT SmclT { get; protected set; }
        public override sealed string Name { get; protected set; }

        public VoidType()
        {
            SmclT = SmclT.VoidT;
            Name = "void";
        }

        public override bool Equals(object obj)
        {
            return obj.GetType() == typeof(VoidType);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ SmclT.GetHashCode();
        }

        public override Type GetPublic()
        {
            return null;
        }

        public override Type GetSecret()
        {
            return null;
        }
    }


    internal class ClientType : SmclType
    {
        public override sealed SmclT SmclT { get; protected set; }
        public override sealed string Name { get; protected set; }

        public ClientType()
        {
            SmclT = SmclT.ClientT;
            Name = "client";
        }

        public override bool Equals(object obj)
        {
            return obj.GetType() == typeof(ClientType);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ SmclT.GetHashCode();
        }

        public override Type GetPublic()
        {
            return null;
        }

        public override Type GetSecret()
        {
            return typeof (SclientType);
        }
    }


    internal class SclientType : ClientType
    {
        public SclientType()
        {
            SmclT = SmclT.SclientT;
            Name = "sclient";
        }

        public override bool Equals(object obj)
        {
            return obj.GetType() == typeof(SclientType) || obj.GetType() == typeof(ClientType);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ SmclT.GetHashCode();
        }

        public override Type GetPublic()
        {
            return typeof(ClientType);
        }

        public override Type GetSecret()
        {
            return null;
        }
    }


    internal class TunnelType : SmclType
    {
        public override sealed SmclT SmclT { get; protected set; }
        public override sealed string Name { get; protected set; }
        public SmclType Tunneled { get; private set; }

        public TunnelType()
        {
            SmclT = SmclT.TunnelT;
            Name = "tunnel";
        }

        public TunnelType(SmclType tunneled) : this()
        {
            Tunneled = tunneled;
        }

        public override bool Equals(object obj)
        {
            return obj.GetType() == typeof(TunnelType);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ SmclT.GetHashCode();
        }

        public override Type GetPublic()
        {
            return null;
        }

        public override Type GetSecret()
        {
            return null;
        }
    }


    internal class ServerType : SmclType
    {
        public override sealed SmclT SmclT { get; protected set; }
        public override sealed string Name { get; protected set; }

        public ServerType()
        {
            SmclT = SmclT.ServerT;
            Name = "server";
        }

        public override bool Equals(object obj)
        {
            return obj.GetType() == typeof(ServerType);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ SmclT.GetHashCode();
        }

        public override Type GetPublic()
        {
            return null;
        }

        public override Type GetSecret()
        {
            return null;
        }
    }


    internal class GroupType : SmclType
    {
        public override sealed SmclT SmclT { get; protected set; }
        public override sealed string Name { get; protected set; }

        public GroupType()
        {
            SmclT = SmclT.GroupT;
            Name = "group";
        }

        public override bool Equals(object obj)
        {
            return obj.GetType() == typeof(GroupType);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ SmclT.GetHashCode();
        }

        public override Type GetPublic()
        {
            return null;
        }

        public override Type GetSecret()
        {
            return null;
        }
    }
}

