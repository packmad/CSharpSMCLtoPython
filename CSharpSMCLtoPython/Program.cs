using System;
using System.IO;
using CSharpSMCLtoPython.ASTbuilder;
using CSharpSMCLtoPython.Visitors;

namespace CSharpSMCLtoPython {

    internal class Program {
        
        private static void Main(string[] args)
        {
            //var stream = args.Length > 0 ? new FileStream(args[0], FileMode.Open) : Console.OpenStandardInput();
            FileStream stream = new FileStream("C:\\Users\\Simone\\test.txt", FileMode.Open);
            

            var parser = new Parser(new Scanner(stream));
            if (parser.Parse()) {
                var parsedProgram = parser.Prog;
                try
                {
                    var typecheckVisitor = new TypecheckVisitor();
                    parsedProgram.Accept(typecheckVisitor);
                    
                    var toStringVisitor = new ToStringVisitor();
                    parsedProgram.Accept(toStringVisitor);

                    Console.WriteLine("TYPE-CHECKED PROG:\n{0}\n", toStringVisitor.Result);
                    /*
                    var pyGen = new GenerateLlvmVisitor();
                    parsedProgram.Accept(codeGen);
                    Console.WriteLine("LLVM CODE:\n{0}\n", codeGen.Result);
                    try {
                        using (var sw = File.CreateText(@"test.ll"))
                            sw.Write(codeGen.Result);
                    } catch (Exception e) {
                        Console.WriteLine("Cannot write output file; reason={0}", e.Message);
                    }
                     * */
                } catch (TypeCheckingException e) {
                    Console.WriteLine("Typechecking error:\n{0}", e.Message);
                }
            }

            Console.ReadLine();
        }
    }
}
