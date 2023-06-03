using Ronin.Grammar;

namespace Ronin.Language;

internal class Result : Semantic
{
    public static implicit operator Result(Anonymous anonymous) => new() { value = anonymous };
    public static implicit operator Result(Datatype datatype) => new() { value = datatype };
    public static implicit operator Result(Function function) => new() { value = function };
    public static implicit operator Result(Datum datum) => new() { value = datum };

    public static implicit operator Anonymous(Result result) => result.value as Anonymous;
    public static implicit operator Datatype(Result result) => result.value as Datatype;
    public static implicit operator Function(Result result) => result.value as Function;
    public static implicit operator Datum(Result result) => result.value as Datum;

    private object value;
}
