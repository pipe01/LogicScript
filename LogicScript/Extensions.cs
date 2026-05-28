using System.Threading;
using System.Threading.Tasks;
using Antlr4.Runtime;
using LogicScript.Parsing;

namespace LogicScript
{
    internal static class Extensions
    {
        public static SourceSpan Span(this ParserRuleContext context)
            => new(context);
        public static SourceSpan Span(this IToken token)
            => new(token);

        public static async Task WaitOrCancel(this Task task, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await Task.WhenAny(task, token.WhenCanceled());
            token.ThrowIfCancellationRequested();
        }

        public static async Task<T> WaitOrCancel<T>(this Task<T> task, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await Task.WhenAny(task, token.WhenCanceled());
            token.ThrowIfCancellationRequested();

            return await task;
        }

        public static Task WhenCanceled(this CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(() => tcs.SetResult(true));
            return tcs.Task;
        }
    }
}
