using System;
using System.Collections;
using System.Collections.Generic;

namespace LogicScript.Parsing
{
    public class ErrorSink : IEnumerable<Error>
    {
        private readonly IList<Error> Errors = new List<Error>();

        public int Count => Errors.Count;

        internal ErrorSink()
        {
        }

        public IEnumerator<Error> GetEnumerator() => this.Errors.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.Errors.GetEnumerator();

        internal void AddError(SourceLocation location, string msg)
        {
            Errors.Add(new Error(false, location, msg));
        }

        internal void AddWarning(SourceLocation location, string msg)
        {
            Errors.Add(new Error(true, location, msg));
        }
    }

    public readonly struct Error
    {
        public bool IsWarning { get; }

        public SourceLocation Location { get; }
        public string Message { get; }

        public Error(bool isWarning, SourceLocation location, string message)
        {
            this.IsWarning = isWarning;
            this.Message = message ?? throw new ArgumentNullException(nameof(message));
            this.Location = location;
        }

        public override string ToString() => $"At {Location}: {Message}";
    }
}
