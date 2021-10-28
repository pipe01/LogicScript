grammar LogicScript;

script              : (declaration NEWLINE)* EOF ;

declaration         : const_decl | input_decl | output_decl | register_decl | when_decl ;
const_decl          : 'const' IDENT '=' expression ;
input_decl          : 'input' port_info ;
output_decl         : 'output' port_info ;
register_decl       : 'reg' port_info ;
when_decl           : 'when' (cond=expression | '*') NEWLINE block 'end' ;

port_info           : BIT_SIZE? IDENT ;

block               : (statement NEWLINE)* ;

statement           : if_statement | assign_statement | task_statement | vardecl_statement ;
assign_statement    : reference EQUALS expression ;

if_statement        : 'if' if_body ;
elseif_statement    : 'else if' if_body ;
else_statement      : 'else' NEWLINE block 'end' ;
if_body             : expression NEWLINE block (elseif_statement | else_statement | 'end') ;

task_statement      : '@' (print_task | printbin_task) ;
print_task          : 'print' (expression | TEXT) ;
printbin_task       : 'print.b' expression ;

vardecl_statement   : 'local' '$' IDENT BIT_SIZE? ('=' expression)? ;

expression          : '(' expression ')'                        # exprParen
                    | funcName=IDENT '(' expression ')'         # exprCall
                    | NOT expression                            # exprNegate
                    | expression op=(OR | AND) expression       # exprAndOr
                    | expression op=(PLUS | MINUS) expression   # exprPlusMinus
                    | expression op=(MULT | DIVIDE) expression  # exprMultDiv
                    | expression XOR expression                 # exprXor
                    | expression op=(
                        COMPARE_EQUALS
                        | COMPARE_GREATER
                        | COMPARE_LESSER
                    ) expression                                # exprCompare
                    | cond=expression '?'
                      ifTrue=expression ':'
                      ifFalse=expression                        # exprTernary
                    | atom                                      # exprAtom
                    | '\\' NEWLINE expression                   # exprLineBreak
                    ;

atom                : reference
                    | DEC_NUMBER
                    ;

reference           : '$' IDENT # refLocal
                    | IDENT     # refPort;

/*
 * Lexer Rules
 */
fragment LOWERCASE  : [a-z] ;
fragment UPPERCASE  : [A-Z] ;
fragment DEC_DIGIT  : [0-9] ;
fragment INPUT      : 'input' ;
fragment OUTPUT     : 'output' ;

DEC_NUMBER          : DEC_DIGIT+ ;
IDENT               : (LOWERCASE | UPPERCASE | '_') (LOWERCASE | UPPERCASE | DEC_DIGIT | '_')* ;
WHITESPACE          : [ \r]+ -> channel(HIDDEN) ;
NEWLINE             : [ \r\n]+ ;
TEXT                : '"' .*? '"' ;

BIT_SIZE            : '\'' DEC_NUMBER ;

EQUALS              : '=' ;
COMPARE_EQUALS      : '==' ;
COMPARE_GREATER     : '>' ;
COMPARE_LESSER      : '<' ;

AND                 : '&' ;
OR                  : '|' ;
XOR                 : '^' ;
NOT                 : '!' | '~' ;
PLUS                : '+' ;
MINUS               : '-' ;
MULT                : '*' ;
DIVIDE              : '/' ;
