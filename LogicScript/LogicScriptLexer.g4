lexer grammar LogicScriptLexer;

fragment LOWERCASE  : [a-z] ;
fragment UPPERCASE  : [A-Z] ;
fragment DEC_DIGIT  : [0-9] ;
fragment BIN_DIGIT  : [01] ;
fragment HEX_DIGIT  : [a-fA-F0-9] ;

INPUT               : 'input' ;
OUTPUT              : 'output' ;
CONST               : 'const' ;
REG                 : 'reg' ;
WHEN                : 'when' ;
STARTUP             : 'startup' ;
ASSIGN              : 'assign' ;
BREAK               : 'break' ;
ELSE_IF             : 'else if' ;
ELSE                : 'else' ;
IF                  : 'if' ;
WHILE               : 'while' ;
FOR                 : 'for' ;
FROM                : 'from' ;
TO                  : 'to' ;
END                 : 'end' ;
LOCAL               : 'local' ;
LEN                 : 'len' ;

AT_TEST             : '@test' ;
AT_PRINT            : '@print' ;
AT_QUEUEUPDATE      : '@queueUpdate' ;

LINE_COMMENT        : '//' ~('\n')* -> channel(HIDDEN) ;
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

ARROW               : '=>' ;

LPAREN              : '(' ;
RPAREN              : ')' ;
LBRACE              : '{' ;
RBRACE              : '}' ;
LBRACKET            : '[' ;
RBRACKET            : ']' ;
COMMA               : ',' ;
QMARK               : '?' ;
COLON               : ':' ;

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
SQUOTE              : '\'' ;
