if __name__ == "__main__":
    
    if (len(sys.argv) != 1):
        raise Exception("No command line arguments needed for server!")
    
    tree = etree.parse(xmlConfigPath)
    players = tree.getroot()
    nPlayers = 0
    for p in players:
        pid = int(p.attrib["id"])
        if pid == 0:
            name = p.attrib["name"]
            hp = (p.attrib["host"], int(p.attrib["port"]))
        if pid > nPlayers:
            nPlayers = pid
    srvClass = globals()[name](hp, nPlayers, xmlConfigPath) 
    if srvClass == None:
        raise Exception ("Check your input id within config file!")
    srvClass.handshakeWithClients()
    srvClass.main()
    srvClass.endProtocol()
    print('>>> SERVER ENDS <<<')