namespace Yapplr.Api.Common;

/// <summary>
/// Wrapper class for caching value types
/// </summary>
public class ValueWrapper<T> where T : struct
{
    public T Value { get; set; }

    public ValueWrapper() { }

    public ValueWrapper(T value)
    {
        Value = value;
    }
}