#CSharpSMCLtoPython

###Brief description
This is a translator from [Secure Multiparty Computation Language (SMCL)] (http://www.brics.dk/SMCL/papers/smcl-plas07.pdf) to Python.

Clarification: I'm using a subset of the language described in the paper, see the language details.

It was developed for educational purposes as the final project for the Implementation of Programming Languages 2013/2014 University of Genoa course.

###Tools
The project was developed using:
- [GPLEX] (https://gplex.codeplex.com/) to generate the scanner using the LEX specification language
- [GPPG] (https://gppg.codeplex.com/) to generate the parser using YACC-like syntax
- [Visual Studio 2013] (http://www.visualstudio.com/) as IDE for the translator
- [ReSharper] (http://www.jetbrains.com/resharper/) as holy hand!
- [Eclipse] (https://www.eclipse.org/) Kepler with [PyDev] (http://pydev.org/) plugin for debug the Python generated code

###The XML configuration file
In the xml file you need to specify:
- the numeric id
- the name (that must match with its definition in the smcl source)
- the hostname and port (because processes communicate through the network)

For each entry you implicitly defines the number of participants.

For example:
		<?xml version='1.0' encoding='utf-8'?>
		<smcxml>
			<player id='0' name='Max' host='127.0.0.1' port='1337'/>
			<player id='1' name='Millionaires' host='127.0.0.1' port='1234'/>
			<player id='2' name='Employees' host='127.0.0.1' port='1235'/>
		</smcxml>

###How it works
It need 3 command line arguments:

./CSharpSMCLtoPython.exe **-i** inputFile.smcl **-o** outputFolder **-x** xmlConfig.xml

and it generates two files: *smclClient.py* and *smclServer.py* in *outputFolder*.

Run the server first without arguments.

Then run the clients specifying the numeric id (relative to the xml configuration).
		
python smclClient.py *id*


###Language details
I think that LEX and YACC syntax it's intuitive and self-explicative to understand, so the best (and updated) documentation is the source:
- [SMCLscanner.l] (https://github.com/simoneaonzo/CSharpSMCLtoPython/blob/master/CSharpSMCLtoPython/ASTbuilder/SMCLscanner.l)
- [SMCLparser.y] (https://github.com/simoneaonzo/CSharpSMCLtoPython/blob/master/CSharpSMCLtoPython/ASTbuilder/SMCLparser.y)


###Criticism
Even though this is my first experience in the world of compilers I'm not satisfied with the feature of my language and my compiler's complexity. 
I lost much time to set up [viff] (http://viff.dk/) for the cryptographic primitives 
that should have been used by the python generated code, 
in the toy examples seemed the right choice, but due to the asynchronous nature
(viff uses [twisted](https://twistedmatrix.com/)) it was a waste of time. 
So, this project was simply an exercise to improve my programming skills 
and understand the magic that's behind ompilers!
