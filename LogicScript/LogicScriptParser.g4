parser grammar LogicScriptParser;

options { tokenVocab = LogicScriptLexer; }

script              : (wsnl declaration WS* NL+ wsnl)* (wsnl test_case NL+)* wsnl EOF ;
test_bench          : (test_case NL+)* EOF ;

test_case           : AT_TEST WS+ (name=TEXT WS+)? LPAREN wsnl (test_step NL+)* wsnl RPAREN wsnl ;
test_step           : wsnl (step_action | step_repeat) wsnl COMMA ;

step_action         : inputs=step_ports WS* ARROW WS* outputs=step_ports ;
step_repeat         : PLUS DEC_NUMBER ;
step_ports          : (step_portvalue (WS+ step_portvalue)*)? ;
step_portvalue      : port=IDENT LPAREN expression (wsnl COMMA wsnl expression)* RPAREN ;

declaration         : decl_const | decl_input | decl_output | decl_register | decl_when | decl_startup | decl_assign ;
decl_const          : CONST WS+ IDENT WS+ EQUALS WS+ expression ;
decl_input          : INPUT port_info ;
decl_output         : OUTPUT port_info ;
decl_register       : REG port_info ;
decl_when           : WHEN space=WS+ (cond=expression | any='*') NL+ block END ;
decl_startup        : STARTUP WS* NL+ block END ;
decl_assign         : ASSIGN WS+ stmt_assign ;

port_info           : (SQUOTE size=expression)? WS+ IDENT simple_indexer? ;

block               : (wsnl stmt WS* NL wsnl)* wsnl ;

stmt                : stmt_if | stmt_for | stmt_assign | stmt_task | stmt_vardecl | stmt_while | stmt_break ;
stmt_assign         : reference wsnl EQUALS wsnl expression       # assignRegular
                    | reference wsnl TRUNC_EQUALS wsnl expression # assignTruncate
                    ;
stmt_break          : BREAK ;

stmt_if             : IF WS+ if_body ;
stmt_elseif         : ELSE_IF WS+ if_body ;
stmt_else           : ELSE WS* NL+ WS* block END ;
if_body             : expression WS* NL+ block (stmt_elseif | stmt_else | END) ;

stmt_for            :
                    FOR WS+ VARIABLE wsnl_req
                    (FROM WS+ from=expression wsnl_req)?
                    TO WS+ to=expression WS* NL+
                    block
                    END
                    ;

stmt_while          : WHILE WS+ expression WS* NL+ block END ;

stmt_task           : (task_print | task_update) ;
task_print          : AT_PRINT wsnl_req (expression | TEXT) ;
task_update         : AT_QUEUEUPDATE ;

stmt_vardecl        : LOCAL WS+ VARIABLE (SQUOTE size=atom)? (wsnl EQUALS wsnl expression)? ;

expression          : LPAREN wsnl expression wsnl RPAREN                        # exprParen
                    | LPAREN wsnl expression wsnl RPAREN SQUOTE size=expression # exprTrunc
                    | funcName=IDENT LPAREN wsnl expression wsnl RPAREN         # exprCall
                    | LEN LPAREN wsnl (reference | expression) wsnl RPAREN    # exprLength
                    | expression slice_indexer                                  # exprSlice
                    | NOT expression                                            # exprNegate
                    | expression wsnl op=(OR | AND) wsnl expression             # exprAndOr
                    | expression wsnl XOR wsnl expression                       # exprXor
                    | expression wsnl POW wsnl expression                       # exprPower
                    | expression wsnl op=(PLUS | MINUS) wsnl expression         # exprPlusMinus
                    | expression wsnl op=(MULT | DIVIDE) wsnl expression        # exprMultDiv
                    | expression wsnl MOD wsnl expression                       # exprModulus
                    | expression wsnl op=(LSHIFT | RSHIFT) wsnl expression      # exprShift
                    | expression wsnl op=(
                        COMPARE_EQUALS
                      | COMPARE_NOTEQUALS
                      | COMPARE_GREATER
                      | COMPARE_LESSER
                    ) wsnl expression                                           # exprCompare
                    | <assoc=right> cond=expression wsnl QMARK wsnl
                      ifTrue=expression wsnl COLON wsnl
                      ifFalse=expression                                        # exprTernary
                    | atom                                                      # exprAtom
                    ;

atom                : reference
                    | number
                    ;

number              : DEC_NUMBER | BIN_NUMBER | HEX_NUMBER ;

reference           : VARIABLE                      # refLocal
                    | IDENT simple_indexer          # refIndex
                    | IDENT                         # refPort
                    ;

wsnl                : (WS | NL)* ;
wsnl_req            : (WS | NL)+ ;

slice_indexer       : LBRACE lr=(COMPARE_GREATER | COMPARE_LESSER)? WS* offset=expression wsnl (COMMA WS* len=expression)? RBRACE ;
simple_indexer      : LBRACKET index=expression RBRACKET ;
