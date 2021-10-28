grammar LogicScript;

script              : (declaration NEWLINE)* EOF ;

declaration         : decl_const | decl_input | decl_output | decl_register | decl_when ;
decl_const          : 'const' IDENT '=' expression ;
decl_input          : 'input' port_info ;
decl_output         : 'output' port_info ;
decl_register       : 'reg' port_info ;
decl_when           : 'when' (cond=expression | '*') NEWLINE block 'end' ;

port_info           : BIT_SIZE? IDENT ;

block               : (stmt NEWLINE)* ;

stmt                : stmt_if | stmt_assign | stmt_task | stmt_vardecl ;
stmt_assign         : reference EQUALS expression       # assignRegular
                    | reference TRUNC_EQUALS expression # assignTruncate
                    ;

stmt_if             : 'if' if_body ;
stmt_elseif         : 'else if' if_body ;
stmt_else           : 'else' NEWLINE block 'end' ;
if_body             : expression NEWLINE block (stmt_elseif | stmt_else | 'end') ;

stmt_task           : '@' (task_print | task_printbin) ;
task_print          : 'print' (expression | TEXT) ;
task_printbin       : 'print.b' expression ;

stmt_vardecl        : 'local' '$' IDENT BIT_SIZE? ('=' expression)? ;

expression          : '(' expression ')'                            # exprParen
                    | '(' expression ')\'' DEC_NUMBER               # exprTrunc
                    | funcName=IDENT '(' expression ')'             # exprCall
                    | NOT expression                                # exprNegate
                    | expression op=(OR | AND) expression           # exprAndOr
                    | expression XOR expression                     # exprXor
                    | expression POW expression                     # exprPower
                    | expression op=(PLUS | MINUS) expression       # exprPlusMinus
                    | expression op=(MULT | DIVIDE) expression      # exprMultDiv
                    | expression op=(LSHIFT | RSHIFT) expression    # exprShift
                    | expression op=(
                        COMPARE_EQUALS
                      | COMPARE_GREATER
                      | COMPARE_LESSER
                    ) expression                                    # exprCompare
                    | cond=expression '?'
                      ifTrue=expression ':'
                      ifFalse=expression                            # exprTernary
                    | atom                                          # exprAtom
                    | '\\' NEWLINE expression                       # exprLineBreak
                    ;

atom                : reference
                    | number
                    ;

number              : DEC_NUMBER | BIN_NUMBER | HEX_NUMBER ;

reference           : '$' IDENT # refLocal
                    | IDENT     # refPort;

/*
 * Lexer Rules
 */
fragment LOWERCASE  : [a-z] ;
fragment UPPERCASE  : [A-Z] ;
fragment DEC_DIGIT  : [0-9] ;
fragment BIN_DIGIT  : [01] ;
fragment HEX_DIGIT  : [a-fA-F0-9] ;
fragment INPUT      : 'input' ;
fragment OUTPUT     : 'output' ;

DEC_NUMBER          : DEC_DIGIT+ ;
BIN_NUMBER          : BIN_DIGIT+ 'b' ;
HEX_NUMBER          : '0x' HEX_DIGIT+ ;
IDENT               : (LOWERCASE | UPPERCASE | '_') (LOWERCASE | UPPERCASE | DEC_DIGIT | '_')* ;
WHITESPACE          : [ \r]+ -> channel(HIDDEN) ;
NEWLINE             : [ \r\n]+ ;
TEXT                : '"' .*? '"' ;

BIT_SIZE            : '\'' DEC_NUMBER ;

EQUALS              : '=' ;
TRUNC_EQUALS        : '\'=' ;
COMPARE_EQUALS      : '==' ;
COMPARE_GREATER     : '>' ;
COMPARE_LESSER      : '<' ;

AND                 : '&' ;
OR                  : '|' ;
XOR                 : '^' ;
LSHIFT              : '<<' ;
RSHIFT              : '>>' ;
NOT                 : '!' | '~' ;
PLUS                : '+' ;
MINUS               : '-' ;
MULT                : '*' ;
DIVIDE              : '/' ;
POW                 : '**' ;
