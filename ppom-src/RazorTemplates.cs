using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ppom
{
    public class RazorEngine
    {
        public RazorEngine(String templateDir) {
            this.cache = new Dictionary<String, Assembly>();

            // points to the local path
            fs = RazorProjectFileSystem.Create(templateDir);

            // customize the default engine a little bit
            engine = RazorProjectEngine.Create(RazorConfiguration.Default, fs, (builder) =>
            {
                InheritsDirective.Register(builder);
                builder.SetNamespace("MyRazorNamespace"); 
            });
        }

        private RazorProjectFileSystem fs;
        private RazorProjectEngine engine;
        private Dictionary<String, Assembly> cache;

        public void LoadTemplate(String filename)
        {
            var item = fs.GetItem(filename);
            var codeDocument = engine.Process(item);
            var cs = codeDocument.GetCSharpDocument();

            // now, use roslyn, parse the C# code
            var tree = CSharpSyntaxTree.ParseText(cs.GeneratedCode);

            string assemblyName = "razor_" + filename;
            var compilation = CSharpCompilation.Create(assemblyName, new[] { tree },
                new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location), // include corlib
                    MetadataReference.CreateFromFile(typeof(RazorCompiledItemAttribute).Assembly.Location), // include Microsoft.AspNetCore.Razor.Runtime
                    MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location), // this file (that contains the MyTemplate base class)

                    // for some reason on .NET core, I need to add this... this is not needed with .NET framework
                    MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Runtime.dll")),

                    // as found out by @Isantipov, for some other reason on .NET Core for Mac and Linux, we need to add this... this is not needed with .NET framework
                    MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "netstandard.dll"))
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                ); 

            // compile the dll
            string path = Path.Combine(Path.GetFullPath("templates/dlls"), filename + ".dll");
            var result = compilation.Emit(path);
            if (!result.Success) {
                Console.WriteLine(string.Join(Environment.NewLine, result.Diagnostics));
                throw new Exception("compile failed");
            }

            // load the built dll
            Console.WriteLine(path);
            var assembly = Assembly.LoadFile(path);
            this.cache[filename] = assembly;
        }

        public MyTemplate CreateTemplate(String filename) 
        {
            var assembly = this.cache[filename];
            // the generated type is defined in our custom namespace, as we
            // asked. "Template" is the type name that razor uses by default.
            var template = (MyTemplate)Activator.CreateInstance(assembly.GetType("MyRazorNamespace.Template"));
            return template;
        }
    }


    // the sample base template class. It's not mandatory but I think it's much easier.
    public abstract class MyTemplate
    {
        public String Name = "Suzy Orman";

        public void run() {
            this.ExecuteAsync().Wait();
        }

        public void WriteLiteral(string literal)
        {
            // replace that by a text writer for example
            Console.Write($"-- {literal} --");
        }

        public void Write(object obj)
        {
            // replace that by a text writer for example
            Console.Write($"-- {obj} --");
        }

        public async virtual Task ExecuteAsync()
        {
            await Task.Yield(); // whatever, we just need something that compiles...
        }
    }
}