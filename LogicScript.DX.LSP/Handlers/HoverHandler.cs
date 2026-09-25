using LogicScript.Data;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using LogicScript.Parsing.Visitors;
using LogicScript.Testing;
using LogicScript.Utils;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LogicScript.DX.LSP.Handlers
{
    class HoverHandler(Workspace workspace) : HoverHandlerBase
    {
        private readonly Workspace Workspace = workspace;

        protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector,
            };
        }

        public override Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            var location = request.Position.ToLocation(request.TextDocument.Uri);

            var node = Workspace.GetNodeAt(request.TextDocument.Uri, location, [
                typeof(PrintStringFormat.PartInterpolate),
                typeof(MachinePortInfo),
                typeof(Reference),
                typeof(Expression),
                typeof(DeclareLocalStatement),
                typeof(PortValue),
                typeof(FunctionBlock),
            ]);
            var lines = new List<string>();
            int size = 0;
            SourceSpan span;
            BitsValue? constValue = null;

            switch (node)
            {
                case MachinePortInfo port:
                    lines.Add(GetPortDescription(port));
                    span = port.Span;
                    break;

                case Reference @ref:
                    if (@ref is PortReference portRef)
                    {
                        lines.Add(GetPortDescription(portRef.PortInfo));
                    }
                    else if (@ref is LocalReference localRef)
                    {
                        lines.Add(SyntaxHighlight($"local {localRef.Name}'{localRef.BitSize}"));
                    }
                    else
                    {
                        if (@ref is ConstantReference cnst)
                            constValue = cnst.Constant.Value;

                        size = @ref.BitSize;
                    }
                    span = @ref.Span;
                    break;

                case PrintStringFormat.PartInterpolate interp:
                    size = interp.LocalInfo.BitSize;
                    span = interp.Span;
                    break;

                case FunctionCallExpression functionCall when functionCall.NameSpan.Contains(location):
                    lines.Add(SyntaxHighlight(functionCall.Function.ToString()));
                    span = functionCall.Span;
                    break;

                case Expression expr:
                    size = expr.BitSize;
                    span = expr.Span;

                    if (expr.IsConstant)
                    {
                        try
                        {
                            constValue = expr.GetConstantValue();
                        }
                        catch { }
                    }

                    break;

                case DeclareLocalStatement local:
                    size = local.Local.BitSize;
                    span = local.Local.Span;
                    break;

                case PortValue portValue:
                    size = portValue.Value.Length;
                    span = portValue.Span;
                    constValue = portValue.Value;
                    break;

                case FunctionBlock function when function.NameSpan.Contains(location):
                    span = function.NameSpan;
                    lines.Add(SyntaxHighlight(function.ToString()));
                    break;

                default:
                    return Task.FromResult<Hover?>(null);
            }

            if (constValue != null)
            {
                lines.Add(SyntaxHighlight(
                    $"dec: {constValue}\n" +
                    $"hex: 0x{constValue.Value.ToStringHex()}\n" +
                    $"bin: {constValue.Value.ToStringBinary()}b"
                ));
            }

            if (size != 0)
                lines.Add($"`{size}` bit{(size != 1 ? "s" : "")} long");

            return Task.FromResult<Hover?>(new Hover
            {
                Contents = new MarkedStringsOrMarkupContent(new MarkupContent
                {
                    Kind = MarkupKind.Markdown,
                    Value = string.Join("\n\n", lines)
                }),
                Range = span.ToRange()
            });
        }

        private static string GetPortDescription(MachinePortInfo port)
        {
            var keyword = port.Target switch
            {
                MachinePorts.Input => "input",
                MachinePorts.Output => "output",
                MachinePorts.Register => "reg",
                _ => "",
            };
            var size = port.BitSize == 1 ? "" : $"'{port.BitSize}";
            var vector = port.VectorLength == 1 ? "" : $"[{port.VectorLength}]";

            return SyntaxHighlight($"{keyword}{size} {port.Name}{vector}");
        }

        private static string SyntaxHighlight(string str) => $"```logicscript\n{str}\n```";
    }
}
