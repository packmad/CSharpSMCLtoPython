import socket, sys

class EasyTcpSocket():
	def __init__(self, myAddr, destAddr=None):
		self.myAddr = myAddr
		self.destAddr = destAddr
		try:
			self.mySock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
			self.mySock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
			self.mySock.bind(self.myAddr)
			if destAddr == None: 
				self.isServer = True
				self.fromAddrToSock = {}
				self.mySock.listen(1)
			else:
				self.isServer = False
				self.mySock.connect(self.destAddr)
		except OSError as e:
			print(e)
			sys.exit()
	
	def closeAllSocket(self):
		if self.isServer:
			for s in self.fromAddrToSock.values():
				s.close()
		self.mySock.close()

	def handshakeWithKnownSource(self, nIter, sources):
		if self.isServer:
			while (nIter):
				conn, addr = self.mySock.accept()
				print('\n---\nReceived connection from ', addr)
				if addr in sources:
					self.fromAddrToSock[addr] = conn
					nIter -= 1
					print(addr, "is a known client\n---\n")
				else:
					conn.close()
					
	def tellToAllClients(self, data):
		if self.isServer:
			for s in self.fromAddrToSock.values():
				rec = self.sendThenRecv(data, s)
				
	def tellToOneClient(self, cliAddr, data):
		if self.isServer:
			return self.sendThenRecv(data, self.fromAddrToSock[cliAddr])

	
	def sendThenRecv(self, data, sock=None):
		received = None
		if sock == None and not self.isServer:
			sock = self.mySock
		try:
			sock.sendall(bytes(data, "utf-8"))
			received = str(sock.recv(1024), "utf-8")
		except OSError as e:
			print(e)
			sys.exit()
		
		print("---\nIteration with {}".format(sock.getpeername()))
		print("Sent:	 {}".format(data))
		print("Received: {}\n---\n".format(received))
		return received
	
	def RevcThenSend(self, data, sock=None):
		received = None
		if sock == None and not self.isServer:
			sock = self.mySock
		try:
			received = str(sock.recv(1024).decode("utf-8"))
			sock.sendall(bytes(data, "utf-8"))
		except OSError as e:
			print(e)
			sys.exit()
			
		print("---\nIteration with {}".format(sock.getpeername()))
		print("Received: {}".format(received))
		print("Sent:	 {}\n---\n".format(data))
		return received
	
	
	def recvThenSendIfMatchKeyword(self, clientAddr, keyword, answer):
		if self.isServer:
			s = self.fromAddrToSock[clientAddr]
			try:
				data = str(s.recv(1024).decode("utf-8"))
				if data:
					if data == keyword:
						s.sendall(bytes(answer, "utf-8"))
					else:
						s.sendall(bytes("KO", "utf-8"))
			except OSError as e:
				print(e)
				sys.exit()
				
				