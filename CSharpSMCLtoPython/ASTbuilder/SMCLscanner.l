%namespace CSharpSMCLtoPython.ASTbuilder
%%
[0-9]+						{ yylval.intValue = int.Parse(yytext); return (int)Tokens.NUM; }
[ \t]|\n|\r\n?				/* empty */
if							{ return (int)Tokens.IF; }
else						{ return (int)Tokens.ELSE; }
while						{ return (int)Tokens.WHILE; }
for							{ return (int)Tokens.FOR; }
in							{ return (int)Tokens.IN; }
display						{ return (int)Tokens.DISPLAY; }
True						{ return (int)Tokens.TRUE; }
False						{ return (int)Tokens.FALSE; }
function					{ return (int)Tokens.FUNCTION; }
open						{ return (int)Tokens.OPEN; }
int							{ return (int)Tokens.INT; }
sint						{ return (int)Tokens.SINT; }
bool						{ return (int)Tokens.BOOL; }
sbool						{ return (int)Tokens.SBOOL; }
void						{ return (int)Tokens.VOID; }
declare						{ return (int)Tokens.DECLARE; }
groupof						{ return (int)Tokens.GROUPOF; }
tunnelof					{ return (int)Tokens.TUNNELOF; }
client						{ return (int)Tokens.CLIENT; }
sclient						{ return (int)Tokens.SCLIENT; }
server						{ return (int)Tokens.SERVER; }
return						{ return (int)Tokens.RETURN; }
put							{ return (int)Tokens.PUT; }
get							{ return (int)Tokens.GET; }
take						{ return (int)Tokens.TAKE; }
readInt\(\)					{ return (int)Tokens.READINT; }
[a-zA-Z_][a-zA-Z_0-9]*\.	{ yylval.identifier=yytext; return (int)Tokens.IDDOT; }
[a-zA-Z_][a-zA-Z_0-9]*		{ yylval.identifier=yytext; return (int)Tokens.ID; }
\"[a-zA-Z_0-9 ]*\"	        { yylval.identifier=yytext; return (int)Tokens.SSTRING; }
\&\&						{ return (int)Tokens.AND; }
\|\|						{ return (int)Tokens.OR; }
==							{ return (int)Tokens.EQUAL; }
\<=							{ return (int)Tokens.LEQ; }
\<							{ return (int)Tokens.LESS_THAN; }
\>=							{ return (int)Tokens.GEQ; }
\>							{ return (int)Tokens.GREATER_THAN; }
!							{ return (int)Tokens.NOT; }
\+|\-|\*|\/|n|\(|\)|,|\{|\}|\=|\;|\:|\|	{ return yytext[0]; }
.							{ Console.WriteLine("Invalid char: {0}", yytext[0]); }
%%
 public override void yyerror(string msg, params object[] args) {
   Console.WriteLine(msg, args);
 }
