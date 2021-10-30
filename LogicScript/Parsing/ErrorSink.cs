using System.Collections;
using System.Collections.Generic;

namespace LogicScript.Parsing
{
    internal class ErrorSink : IReadOnlyList<Error>
    {
        public Error this[int index] => Errors[index];

        public int Count => Errors.Count;

        private readonly IList<Error> Errors = new List<Error>();

        public void AddError(string msg, SourceLocation location, Severity severity = Severity.Error)
        {
            Errors.Add(new Error(msg, location, severity));
        }

        public IEnumerator<Error> GetEnumerator() => Errors.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Errors.GetEnumerator();
    }
}
