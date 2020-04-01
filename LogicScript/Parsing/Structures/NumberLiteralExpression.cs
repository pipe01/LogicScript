namespace LogicScript.Parsing.Structures
{
    internal class NumberLiteralExpression : Expression
    {
        public static readonly NumberLiteralExpression Zero = new NumberLiteralExpression(0, 1);

        public int Value { get; }
        public int? Length { get; }

        public NumberLiteralExpression(int value, int? length = null)
        {
            this.Value = value;
            this.Length = length;
        }
    }
}
