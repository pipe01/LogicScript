grammar LogicScript;

script              : (declaration NEWLINE)* EOF ;

declaration         : input_decl | output_decl | register_decl | when_decl ;
input_decl          : 'input' BIT_SIZE?  IDENT ;
output_decl         : 'output' BIT_SIZE?  IDENT ;
register_decl       : 'reg' BIT_SIZE? IDENT ;
when_decl           : 'when' (expression | '*') NEWLINE block ;

block               : (statement NEWLINE)* 'end' ;

statement           : assign_statement ;
assign_statement    : reference EQUALS expression ;

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

DEC_NUMBER              : DEC_DIGIT+ ;
IDENT               : (LOWERCASE | UPPERCASE)+ ;
TEXT                : '"' .*? '"' ;
WHITESPACE          : [ \r]+ -> skip ;
NEWLINE             : [\r\n]+ ;

BIT_SIZE            : '\'' DEC_NUMBER ;

EQUALS      : '=' ;
COMPARE_EQUALS      : '==' ;
COMPARE_GREATER     : '>' ;
COMPARE_LESSER     : '<' ;

AND                 : '&' ;
OR                  : '|' ;
XOR                 : '^' ;
NOT                 : '!' | '~' ;
