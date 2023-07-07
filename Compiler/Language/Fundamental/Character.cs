namespace Ronin.Language.Fundamental;

internal class Character : Fundamental<char>
{
    private const string sourcecode = """
        datatype character
        {
            intrinsic function += (text => text) => text { return this += text; }

            intrinsic function < (character => character) => maybe { return this < character; }
            intrinsic function <= (character => character) => maybe { return this <= character; }
            intrinsic function > (character => character) => maybe { return this < character; }
            intrinsic function >= (character => character) => maybe { return this >= character; }
                
            intrinsic shared function (me => something) is char => maybe { return me is char; }

            intrinsic function is letter => maybe { return char.CheckLetter(this); }
            intrinsic function is number => maybe { return char.CheckNumber(this); }
            intrinsic function is punctuation => maybe { return char.CheckPuntuation(this); }
            intrinsic function is separator => maybe { return char.CheckSeparator(this); }
            intrinsic function is symbol => maybe { return char.CheckSymbol(this); }
            intrinsic function is ascii => maybe { return char.IsAscii(this); }
            intrinsic function is digit => maybe { return char.IsDigit(this); }
            intrinsic function is letter => maybe { return char.IsLetter(this); }
            intrinsic function is control => maybe { return char.IsControl(this); }
            intrinsic function is upper => maybe { return char.IsUpper(this); }
            intrinsic function is lower => maybe { return char.IsLower(this); }
            intrinsic function is whitespace => maybe { return char.IsWhitespace(this); }
                
            intrinsic function as maybe => maybe { return char.ToBoolean(this); }
            intrinsic function as text => text { return ToString(); }
            intrinsic function as number => number { return char.GetNumericValue(this); }
            intrinsic function as whole number => whole number { return (long)char.getNumericValue(this); }
                
            intrinsic function to lower => character { return char.ToLower(this); }
            intrinsic function to upper => character { return char.ToUpper(this); }

            intrinsic shared function (left => character) <= (me => character) <= (right => character) => maybe
            { 
                return char.IsBetween(me, left, right); 
            }
            
            constant min = '\0';
            constant max = '\uffff';
        }
        """;

    public Character() : base("character") { }
}
