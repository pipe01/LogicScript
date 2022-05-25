namespace LogicScript.ByteCode
{
    public enum OpCode : byte
    {
        Nop,        // nop [0]
        Pop,        // pop [-1]

        Ldi_8,      // ldi_8 <value (1)> <length (1)> [+1]
        Ldi_16,     // ldi_16 <value (2)> <length (1)> [+1]
        Ldi_32,     // ldi_32 <value (4)> <length (1)> [+1]
        Ldi_64,     // ldi_16 <value (8)> <length (1)> [+1]
        Ld_0,       // ld_0 <length (1)> [+1]
        Ld_1,       // ld_1 <length (1)> [+1]
        Ld_0_1,     // ld_0_1 [+1]
        Ld_1_1,     // ld_1_1 [+1]

        Dup,        // dup [+1]
        Show,       // show [-1]

        Jmp,        // jump <addr (4)> [0]
        Brz,        // brz <addr (4)> [-1]
        Brnz,       // brnz <addr (4)> [-1]
        Breq,       // breq <addr (4)> [-2]
        Brneq,      // brneq <addr (4)> [-2]

        Add,        // add [-2+1]
        Sub,        // sub [-2+1]

        Trunc,      // trunc <size (1)> [-1+1]

        Ldloc,      // ldloc <num (1)> [+1]
        Stloc,      // stloc <num (1)> [-1]
    }
}