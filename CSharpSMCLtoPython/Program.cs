using System;
using System.CodeDom;
using System.IO;
using System.Linq;
using CSharpSMCLtoPython.ASTbuilder;
using CSharpSMCLtoPython.Visitors;

namespace CSharpSMCLtoPython {

    internal class Program {
        
        private static void Main(string[] args)
        {
            //var stream = args.Length > 0 ? new FileStream(args[0], FileMode.Open) : Console.OpenStandardInput();
            string pythonSourcePath = "C:\\Users\\Simone\\workspace\\SMCLpy\\smcl\\";
            FileStream stream = new FileStream("C:\\Users\\Simone\\Documents\\GitHub\\CSharpSMCLtoPython\\CSharpSMCLtoPython\\Test\\testFile.txt", FileMode.Open);


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
                    Console.WriteLine("TYPE-CHECKED PROG:\n\n{0}\n", toStringVisitor.Result);

                    var pyGen = new ToPythonVisitor(
                        pythonSourcePath + "smcxmlconfig.xml",
                        pythonSourcePath + "supportFileForClient.py",
                        pythonSourcePath + "mainFileForClient.py",
                        pythonSourcePath + "supportFileForServer.py",
                        pythonSourcePath + "mainFileForServer.py"
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
                                using (var sw = File.CreateText(pythonSourcePath + @"smclServer.py"))
                                    sw.Write(s);
                            }
                            else
                            {
                                using (var sw = File.CreateText(pythonSourcePath + @"smclClient" + ".py"))
                                    sw.Write(s);
                            }
                         }
                         
                        /*
                        using ( var sw = File.CreateText(@"output.py"))
                            sw.Write(pyGen.Result);
                        */
                    } catch (Exception e) {
                        Console.WriteLine("Cannot write output file; reason={0}", e.Message);
                    }

                } catch (TypeCheckingException e) {
                    Console.WriteLine("Typechecking error:\n{0}", e.Message);
                }
            }

            Console.ReadLine();
        }
    }
}

//EOF