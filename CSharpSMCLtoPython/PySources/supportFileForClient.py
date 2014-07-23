#-----------------------------------------------------------------------------#
#BEGIN SUPPORT CLASSES FOR CLIENT
#-----------------------------------------------------------------------------#
import re
import xml.etree.ElementTree as etree
#-----------------------------------------------------------------------------#

class Tunnel():
    def __init__(self, tunnelName, value, isSecure=False):
        self.name = tunnelName
        self.value = value
        self.isSecure = isSecure

    # non-blocking and if the tunnel is empty returns the special value Null.
    def get(self):
        return self.value
    
    # blocking and if the tunnel is empty waits until a value becomes available
    def take(self):
        return self.value
    
    # Values may be placed in the tunnel 
    def put(self, value):
        self.value = value

#-----------------------------------------------------------------------------#

class Client():
    def __init__(self, cliAddr, srvAddr):
        self.myTcpSock = EasyTcpSocket(cliAddr, srvAddr)
        self.tunnels = {}
        self.callRegex = re.compile(r"^CALL,[a-zA-Z0-9]+,((int|sint|bool|sbool) [a-zA-Z0-9]+,)*$")
        self.tunRegex = re.compile(r"^TUN,[a-zA-Z0-9]+,(get|take)$")


    def addTunnel(self, tunnel):
        self.tunnels[tunnel.name] = tunnel

    def typeToString(self, var):
        types = ["int", "bool"]
        varTypeStr = str(type(var))
        for t in types:
            if t in varTypeStr:
                return t

    def methTunnel(self, tunnelName, op):
        if (op=="get"):
            v = self.tunnels[tunnelName].get()
        elif (op=="take"):
            v = self.tunnels[tunnelName].take()
        tv = self.typeToString(v)
        ret = tv + ',' + str(v)
        print("---\nTUN {}.{}()\nRETURN: {}\n---\n".format(tunnelName,op,ret))
        return ret
    
    def callMethod(self, methodName, args):
        typedArgs = []
        for a in args:
            s = re.split(" ", a)
            if s[0] == "int" or s[0] == "sint":
                typedArgs.append(int(s[1]))
            elif  s[0] == "bool" or s[0] == "sbool":
                typedArgs.append( (lambda st: st in ["True"])(s[1]) )
        print("---\nCALL {}({})".format(methodName, typedArgs))
        getattr(self, methodName)(*typedArgs)
        print("\n---\n")

    def parseCmd(self, cmd):
        if cmd == "__EOC__":
            print(">>> End Of Computation <<<")
            return
        if (self.callRegex.match(cmd)):
            f = re.split(",", cmd)
            self.callMethod(f[1], f[2:])
        elif (self.tunRegex.match(cmd)):
            f = re.split(",", cmd)
            ret = self.methTunnel(f[1], f[2])
            self.myTcpSock.sendThenRecv(ret)
        else:
            raise Exception("Wrong command from server: '" + cmd +  "'")
        
    def getAndParseCmds(self):
        cmd = ""
        while (cmd != "__EOC__"):
            cmd = self.myTcpSock.RevcThenSend("ACK")
            self.parseCmd(cmd)
        self.myTcpSock.closeAllSocket()
        
#-----------------------------------------------------------------------------#
#END SUPPORT CLASSES FOR CLIENT
