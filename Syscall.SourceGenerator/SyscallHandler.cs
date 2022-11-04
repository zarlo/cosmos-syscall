using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Syscall.Common;

namespace Syscall.SourceGenerator;

[Generator]
public class SyscallHandler : ISourceGenerator
{
    public class syscallData {
        public uint[] syscall;

        public bool passIRQContext = true; 
        public MethodDeclarationSyntax component;
    }
    public void Initialize(GeneratorInitializationContext context)
    {

    }

    public void Execute(GeneratorExecutionContext context)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(@"
using System;
using static Cosmos.Core.INTs;
namespace libcs.core {
    public static class SyscallHandler
    {
        static byte interrupt;
        public static void Init(byte interrupt) {
            SyscallHandler.interrupt = interrupt;
            SetIntHandler(interrupt, InterruptHandler);
        }

        public unsafe static void InterruptHandler(ref IRQContext aContext)
        {
            if (aContext.Interrupt != interrupt) return;
            switch(aContext.EAX)
            {
            ");

                foreach (syscallData item in GetSyscalls(context.Compilation))
                {
                    foreach (var syscall in item.syscall)
                    {
                        sb.Append($"case {syscall}:");
                        
                    }
                    sb.Append(@$"{item.component.Identifier.Text}(ref aContext);");
                    sb.Append("break;");
                }

            sb.Append(@"
            default:
            break;
            }
        }

    }
}");

        
        context.AddSource(
            "libcs.core.generator.syscallhandler",
            SourceText.From(sb.ToString(), Encoding.UTF8)
        );
    }

    private static ImmutableArray<syscallData> GetSyscalls(Compilation compilation)
    {
        // Get all classes
        IEnumerable<SyntaxNode> allNodes = compilation.SyntaxTrees.SelectMany(s => s.GetRoot().DescendantNodes());
        IEnumerable<MethodDeclarationSyntax> allClasses = allNodes
            .Where(d => d.IsKind(SyntaxKind.MethodDeclaration))
            .OfType<MethodDeclarationSyntax>();

        ImmutableArray<syscallData> output = allClasses
            .Select(component => TryGetSyscall(compilation, component))
            .Where(syscall => syscall is not null)
            .ToImmutableArray();

        return output;
    
    }


    private static syscallData TryGetSyscall(Compilation compilation, MethodDeclarationSyntax component)
    {
        var attributes = component.AttributeLists
            .SelectMany(x => x.Attributes)
            .Where(attr => attr.GetType() == typeof(SyscallAttribute))
            .Cast<SyscallAttribute>()
            .Select(x => {
                return new syscallData() {
                    syscall = x.SysCall,
                    component = component,
                };

            })
            .ToArray();
        if(attributes.Length == 0)
            return null;

        return attributes[0];

    }

}