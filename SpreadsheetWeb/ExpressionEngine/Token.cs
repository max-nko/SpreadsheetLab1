namespace SpreadsheetLab1.ExpressionEngine
{
    public enum TokenType
    {
        Number,   
        CellRef,  
        Plus,  
        Minus, 
        Multiply, 
        Divide,  
        Equal, 
        Less,  
        Greater, 
        Not,   
        Inc, 
        Dec,   
        Max,  
        Min,    
        LParen,    
        RParen,    
        Comma,      
        EOF         
    }

    public record Token(TokenType Type, string Lexeme, int Position);
}
