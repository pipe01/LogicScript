using Antlr4.Runtime.Misc;
using LogicScript.Parsing.Structures;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LogicScript.Parsing
{
    internal class ErrorSink : IReadOnlyList<Error>
    {
        public Error this[int index] => Errors[index];

        public int Count => Errors.Count;

        internal bool HasANTLRError = false;

        private readonly IList<Error> Errors = new List<Error>();

        public void AddError(string msg, SourceLocation location, bool isFatal = false, bool isANTLR = false, Severity severity = Severity.Error)
        {
            if (isANTLR)
            {
                if (!HasANTLRError)
                    HasANTLRError = true;
                else
                    goto exit;
            }

            Errors.Add(new Error(msg, location, severity, isANTLR));

        exit:
            if (isFatal)
                throw new ParseCanceledException();
        }
        public void AddError(string msg, ICodeNode node, bool isFatal = false, bool isANTLR = false, Severity severity = Severity.Error)
        {
            if (isANTLR)
            {
                if (!HasANTLRError)
                    HasANTLRError = true;
                else
                    goto exit;
            }

            if (!Errors.Any(o => o.Node == node))
            {
                Errors.Add(new Error(msg, node, severity, isANTLR));
            }

        exit:
            if (isFatal)
                throw new ParseCanceledException();
        }

        public IEnumerator<Error> GetEnumerator() => Errors.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Errors.GetEnumerator();
    }
}
