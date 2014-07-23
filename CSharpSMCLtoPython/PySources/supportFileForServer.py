#-----------------------------------------------------------------------------#
#BEGIN SUPPORT CLASSES FOR SERVER
#-----------------------------------------------------------------------------#
import re
import xml.etree.ElementTree as etree
#-----------------------------------------------------------------------------#

class Client():
    def __init__(self, cliAddr, srvInstance):
        self.cliAddr = cliAddr 
        self.srvInstance = srvInstance
        
    def remoteTunnelMethod(self, tunnelName, method):
        self.srvInstance.myTcpSock.tellToOneClient(self.cliAddr, "TUN," + tunnelName + "," + method)
        typedStr = self.srvInstance.myTcpSock.RevcThenSend("ACK", self.srvInstance.myTcpSock.fromAddrToSock[self.cliAddr])
        return self.srvInstance.fromTypedToVar(typedStr)

    def remoteMethodCall(self, csvSignature):
        self.srvInstance.myTcpSock.tellToOneClient(self.cliAddr, "CALL," + csvSignature)
        
#-----------------------------------------------------------------------------#
    
class Server():
    
    def __init__(self, srvAddr, groupPairList, xmlConfigPath, nPlayers):
        self.nPlayers = nPlayers
        self.srvAddr = srvAddr
        tree = etree.parse(xmlConfigPath)
        players = tree.getroot()
        self.groups = {}
        
        for p in players:
            for gpl in groupPairList:
                if p.attrib['name'] == gpl[0]:
                    if (not self.groups.__contains__(gpl[1])):
                        self.groups[gpl[1]] = []
                    self.groups[gpl[1]].append( Client( (p.attrib['host'], int(p.attrib['port'])), self) )
                    
        self.myTcpSock = EasyTcpSocket(self.srvAddr)
    
    
    def getAllClients(self):
        allClients = []
        for v in self.groups.values():
            for c in v:
                allClients.append(c.cliAddr)
        return allClients
    
    def iKnowThisClient(self, addr):
        for v in self.groups.values():
            for c in v:
                if c.cliAddr == addr:
                    return True
        return False
    
    def handshakeWithClients(self):
        self.myTcpSock.handshakeWithKnownSource(self.nPlayers, self.getAllClients())

    
    def addGroup(self, client, groupName):
        self.groups[groupName] = client
    
    def fromTypedToVar(self, typedStr):
        sp = re.split(",", typedStr)
        if sp[0]=="int":
            var = int(sp[1])
        elif sp[0]=="bool":
            var = (lambda st: st in ["True"])(sp[1])
        return var
    
    def remoteMethodCall(self, cliAddr, csvSignature):
        self.myTcpSock.tellToOneClient(cliAddr, "CALL," + csvSignature)


    def endProtocol(self):
        self.myTcpSock.tellToAllClients("__EOC__")
            

#-----------------------------------------------------------------------------#
#END SUPPORT CLASSES FOR SERVER
