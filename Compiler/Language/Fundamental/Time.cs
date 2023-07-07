namespace Ronin.Language.Fundamental;

internal class Time : Fundamental<TimeOnly>
{
    private const string sourcecode = """
        datatype time 
        {
            intrinsic function + (duration => Duration) => time { return this.Add(duration); }
            intrinsic function - (duration => Duration) => time { return this - duration; }
            intrinsic function < (other => time) => maybe { return me < other; }
            intrinsic function <= (other => time) => maybe { return me <= other; }
            intrinsic function > (other => time) => maybe { return me > other; }
            intrinsic function >= (other => time) => maybe { return me >= other; }

            intrinsic shared function (left => time) <= (me => time) <= (right => time) => maybe { return me.IsBetween(left, right); }

            intrinsic function as text => text { return this.ToString(); }
        }
        """;

    public Time() : base("time") { }
}
