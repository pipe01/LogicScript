using System;

namespace LogicScript.Parsing.Structures
{
    internal class Directive : TopLevelNode
    {
        public string Name { get; }
        public string Value { get; }

        public Directive(string name, string value, SourceLocation location) : base(location)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
