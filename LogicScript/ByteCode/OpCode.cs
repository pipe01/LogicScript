namespace LogicScript.ByteCode
{
    public enum OpCode : ushort
    {
        Ldi_16,     // ldi_16 <value (1)> <length (1)>
        Ldi_32,     // ldi_32 <value (2)> <length (1)>
        Ldi_64,     // ldi_16 <value (4)> <length (1)>
        Ld_0,       // ld_0 <length (1)>
        Ld_1,       // ld_1 <length (1)>
        Ld_0_1,     // ld_0_1
        Ld_1_1,     // ld_1_1

        Dup,        // dup
        Show,       // show
    }
}