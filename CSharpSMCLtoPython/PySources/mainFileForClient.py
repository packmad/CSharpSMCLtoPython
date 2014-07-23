if __name__ == "__main__":
    if (len(sys.argv) != 2):
        raise Exception("USAGE: ./client.py id")
    pid = int(sys.argv[1])
    if (pid < 1):
        raise Exception("id must be greater than 0")
    tree = etree.parse(xmlConfigPath)
    players = tree.getroot()
    pclass = None
    for p in players:
        if int(p.attrib["id"]) == 0:
            srvAddr = (p.attrib["host"], int(p.attrib["port"])) 
        if int(p.attrib["id"]) == pid:
            cliAddr = (p.attrib["host"], int(p.attrib["port"])) 
            pclass = globals()[p.attrib["name"]](cliAddr, srvAddr)
    if pclass == None:
        raise Exception ("Check your input id within config file!")
    pclass.main()
    pclass.getAndParseCmds()