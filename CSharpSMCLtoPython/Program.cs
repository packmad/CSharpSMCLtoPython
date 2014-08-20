using System;
using System.CodeDom;
using System.IO;
using System.Linq;
using CSharpSMCLtoPython.ASTbuilder;
using CSharpSMCLtoPython.Visitors;

namespace CSharpSMCLtoPython {

    internal class Program {

        private static string Usage()
        {
            return
                "Usage:\n" +
                AppDomain.CurrentDomain.FriendlyName + " -i inputFile.smcl -o outputFolder -x xmlConfig.xml\n" +
                "\nIt generates two files (smclClient.py and smclServer.py) in outputFolder";
        }

        private static void Main(string[] args)
        {
            //Console.WriteLine(Directory.GetCurrentDirectory());
            if (args.Length != 6)
            {
                Console.WriteLine(Usage());
                Console.ReadLine();
                return;
            }


            const string pythonSourcePath = "..\\..\\PySources\\";
            string xmlConfigPath = null;
            string outputPath = null;
            FileStream stream = null;


            for (int i=0; i < args.Length; ++i)
            {
                try
                {
                    switch (args[i])
                    {
                        case "-i":
                            stream = new FileStream(args[++i], FileMode.Open);
                            break;
                        case "-o":
                            outputPath = args[++i];
                            break;
                        case "-x":
                            xmlConfigPath = args[++i];
                            break;
                    }
                }
                catch (IndexOutOfRangeException ie)
                {
                    Console.WriteLine(ie + "\n" + Usage());
                    Console.ReadLine();
                    return;
                }
            }

            Console.SetWindowSize(80,62);
            var parser = new Parser(new Scanner(stream));
            if (parser.Parse()) {
                var parsedProgram = parser.Prog;
                try
                {
                    var typecheckVisitor = new TypecheckVisitor();
                    parsedProgram.Accept(typecheckVisitor);
                    
                    var toStringVisitor = new ToStringVisitor();
                    parsedProgram.Accept(toStringVisitor);
                    //Console.WriteLine("TYPE-CHECKED PROG:\n\n{0}\n", toStringVisitor.Result);

                    var pyGen = new ToPythonVisitor(
                        xmlConfigPath,
                        pythonSourcePath + "supportFileForClient.py",
                        pythonSourcePath + "mainFileForClient.py",
                        pythonSourcePath + "supportFileForServer.py",
                        pythonSourcePath + "mainFileForServer.py",
                        pythonSourcePath + "easyTcpSocket.py"
                        );
                    parsedProgram.Accept(pyGen);
                    //Console.WriteLine("PYTHON CODE:\n\n{0}\n", pyGen.Result);

                   
                    try
                    {

                        var splitted = pyGen.Result.Split(new string[] { "#ENDOFCLIENTSDEF" }, StringSplitOptions.RemoveEmptyEntries);
                        var last = splitted.Last();
                        //int i = 0;
                        foreach (var s in splitted)
                        {
                            if (s.Equals(last))
                            {
                                using (var sw = File.CreateText(outputPath + @"smclServer.py"))
                                    sw.Write(s);
                            }
                            else
                            {
                                using (var sw = File.CreateText(outputPath + @"smclClient" + ".py"))
                                    sw.Write(s);
                            }
                         }
                    } catch (Exception e) {
                        Console.WriteLine("Cannot write output file; reason={0}", e.Message);
                    }

                } catch (TypeCheckingException e) {
                    Console.WriteLine("Typechecking error:\n{0}", e.Message);
                }
            }
            Console.WriteLine("Press Any Key to Continue...");
            Console.ReadLine();
        }
    }
}