grammar LogicScript;

script              : (declaration NEWLINE)* EOF ;

declaration         : const_decl | input_decl | output_decl | register_decl | when_decl ;
const_decl          : 'const' IDENT '=' expression ;
input_decl          : 'input' BIT_SIZE?  IDENT ;
output_decl         : 'output' BIT_SIZE?  IDENT ;
register_decl       : 'reg' BIT_SIZE? IDENT ;
when_decl           : 'when' (expression | '*') NEWLINE block 'end' ;

block               : (statement NEWLINE)* ;

statement           : if_statement | assign_statement ;
assign_statement    : reference EQUALS expression ;
if_statement        : 'if' if_body ;
elseif_statement    : 'else if' if_body ;
else_statement      : 'else' NEWLINE block 'end' ;

if_body             : expression NEWLINE block (elseif_statement | else_statement | 'end') ;

expression          : '(' expression ')'                  # exprParen
                    | NOT expression                      # exprNegate
                    | expression op=(OR | AND) expression # exprAndOr
                    | expression XOR expression           # exprXor
                    | expression op=(
                        COMPARE_EQUALS
                        | COMPARE_GREATER
                        | COMPARE_LESSER
                    ) expression                          # exprCompare
                    | atom                                # exprAtom
                    ;

atom                : reference
                    | DEC_NUMBER
                    ;

reference           : IDENT ;

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
TEXT                : '"' .*? '"' ;
WHITESPACE          : [ \r]+ -> skip ;
NEWLINE             : [\r\n]+ ;

BIT_SIZE            : '\'' DEC_NUMBER ;

EQUALS              : '=' ;
COMPARE_EQUALS      : '==' ;
COMPARE_GREATER     : '>' ;
COMPARE_LESSER      : '<' ;

AND                 : '&' ;
OR                  : '|' ;
XOR                 : '^' ;
NOT                 : '!' | '~' ;
