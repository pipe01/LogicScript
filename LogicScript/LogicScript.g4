grammar LogicScript;

script              : (declaration NL+)* EOF ;
test_bench          : (test_case NL+)* EOF ;

test_case           : '@test' WS+ name=TEXT WS+ LPAREN wsnl (test_step NL+)* RPAREN wsnl ;
test_step           : (step_action | step_repeat) COMMA ;

step_action         : inputs=step_ports '=>' WS* outputs=step_ports ;
step_repeat         : '@' DEC_NUMBER ;
step_ports          : (step_portvalue WS+)+ ;
step_portvalue      : port=IDENT LPAREN value=number RPAREN ;

declaration         : decl_const | decl_input | decl_output | decl_register | decl_when | decl_startup | decl_assign ;
decl_const          : 'const' WS+ IDENT WS+ '=' WS+ expression ;
decl_input          : 'input' port_info ;
decl_output         : 'output' port_info ;
decl_register       : 'reg' port_info ;
decl_when           : 'when' WS+ (cond=expression | any='*') NL+ block 'end' ;
decl_startup        : 'startup' WS* NL+ block 'end' ;
decl_assign         : 'assign' WS+ stmt_assign ;

port_info           : ('\'' size=expression)? WS+ IDENT ;

block               : (wsnl stmt WS* NL wsnl)* wsnl ;

stmt                : stmt_if | stmt_for | stmt_assign | stmt_task | stmt_vardecl | stmt_while | stmt_break ;
stmt_assign         : reference wsnl EQUALS wsnl expression       # assignRegular
                    | reference wsnl TRUNC_EQUALS wsnl expression # assignTruncate
                    ;
stmt_break          : 'break' ;

stmt_if             : 'if' WS+ if_body ;
stmt_elseif         : 'else if' WS+ if_body ;
stmt_else           : 'else' WS* NL+ WS* block 'end' ;
if_body             : expression WS* NL+ block (stmt_elseif | stmt_else | 'end') ;

stmt_for            :
                    'for' WS+ VARIABLE wsnl_req
                    ('from' WS+ from=expression wsnl_req)?
                    'to' WS+ to=expression WS* NL+
                    block
                    'end'
                    ;

stmt_while          : 'while' WS+ expression WS* NL+ block 'end' ;

stmt_task           : '@' (task_print | task_update) ;
task_print          : 'print' wsnl_req (expression | TEXT) ;
task_update         : 'queueUpdate' ;

stmt_vardecl        : 'local' WS+ VARIABLE ('\'' size=atom)? (wsnl '=' wsnl expression)? ;

expression          : LPAREN wsnl expression wsnl RPAREN                        # exprParen
                    | LPAREN wsnl expression wsnl RPAREN '\'' DEC_NUMBER        # exprTrunc
                    | funcName=IDENT LPAREN wsnl expression wsnl RPAREN         # exprCall
                    | expression indexer                                        # exprSlice
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
                    | cond=expression wsnl '?' wsnl
                      ifTrue=expression wsnl ':' wsnl
                      ifFalse=expression                                        # exprTernary
                    | atom                                                      # exprAtom
                    ;

atom                : reference
                    | number
                    ;

number              : DEC_NUMBER | BIN_NUMBER | HEX_NUMBER ;

reference           : VARIABLE                      # refLocal
                    | IDENT                         # refPort
                    // | reference indexer             # refSlice
                    ;

wsnl                : (WS | NL)* ;
wsnl_req            : (WS | NL)+ ;

indexer             : '[' lr=('>' | '<')? WS* offset=expression wsnl (COMMA WS* len=expression)? ']' ;

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

LINE_COMMENT        : '//' .*? '\n' -> channel(HIDDEN) ;
COMMENT             : '/*' .*? '*/' -> channel(HIDDEN) ;

DEC_NUMBER          : DEC_DIGIT+ ;
BIN_NUMBER          : BIN_DIGIT+ 'b' ;
HEX_NUMBER          : '0x' HEX_DIGIT+ ;
IDENT               : (LOWERCASE | UPPERCASE | '_') (LOWERCASE | UPPERCASE | DEC_DIGIT | '_')* ;
WS                  : ' ' ;
NL                  : [\n;] ;
TEXT                : '"' .*? '"' ;

VARIABLE            : '$' IDENT ;

EQUALS              : '=' ;
COMPARE_NOTEQUALS   : '!=' ;
TRUNC_EQUALS        : '\'=' ;
COMPARE_EQUALS      : '==' ;
COMPARE_GREATER     : '>' ;
COMPARE_LESSER      : '<' ;

LPAREN              : '(' ;
RPAREN              : ')' ;
COMMA               : ',' ;

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
MOD                 : '%' ;
