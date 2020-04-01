namespace LogicScript.Parsing.Structures
{
    internal class NumberLiteralExpression : Expression
    {
        public int Value { get; }
        public int? Length { get; }

        public NumberLiteralExpression(int value, int? length = null)
        {
            this.Value = value;
            this.Length = length;
        }
    }
}
