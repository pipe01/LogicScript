using LogicScript.ByteCode.DevEx;

namespace LogicScript.ByteCode
{
    public enum OpCode : byte
    {
        [OpCode("nop"), Stack(0)] Nop,
        [OpCode("pop"), Stack(-1)] Pop,

        [OpCode("ldi_8", "value", 1, "length", 1), Stack(+1)] Ldi_8,
        [OpCode("ldi_16", "value", 2, "length", 1), Stack(+1)] Ldi_16,
        [OpCode("ldi_32", "value", 4, "length", 1), Stack(+1)] Ldi_32,
        [OpCode("ldi_64", "value", 8, "length", 1), Stack(+1)] Ldi_64,
        [OpCode("ld_0", "length", 1), Stack(+1)] Ld_0,
        [OpCode("ld_1", "length", 1), Stack(+1)] Ld_1,
        [OpCode("ld_0_1"), Stack(+1)] Ld_0_1,
        [OpCode("ld_1_1"), Stack(+1)] Ld_1_1,
        [OpCode("dup"), Stack(+1)] Dup,
        [OpCode("show"), Stack(-1)] Show,

        [OpCode("jump", "addr", 4), Stack(0)] Jmp,
        [OpCode("brz", "addr", 4), Stack(-1)] Brz,
        [OpCode("brnz", "addr", 4), Stack(-1)] Brnz,
        [OpCode("breq", "addr", 4), Stack(-1)] Breq,
        [OpCode("brneq", "addr", 4), Stack(-1)] Brneq,

        [OpCode("and"), Stack(-2, +1)] And,
        [OpCode("or"), Stack(-2, +1)] Or,
        [OpCode("xor"), Stack(-2, +1)] Xor,
        [OpCode("shl"), Stack(-2, +1)] Shl,
        [OpCode("shr"), Stack(-2, +1)] Shr,

        [OpCode("add"), Stack(-2, +1)] Add,
        [OpCode("sub"), Stack(-2, +1)] Sub,
        [OpCode("mul"), Stack(-2, +1)] Mult,
        [OpCode("div"), Stack(-2, +1)] Div,
        [OpCode("pow"), Stack(-2, +1)] Pow,
        [OpCode("mod"), Stack(-2, +1)] Mod,

        FirstOp = And,
        LastOp = Mod,

        [OpCode("trunc", "size", 1), Stack(-1, +1)] Trunc,

        [OpCode("ldloc", "num", 1), Stack(+1)] Ldloc,
        [OpCode("stloc", "num", 1), Stack(-1)] Stloc,
    }
}