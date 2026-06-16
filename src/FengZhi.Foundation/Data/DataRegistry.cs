namespace FengZhi.Foundation.Data;

/// <summary>
/// 数据注册中心。启动时注册所有 IDataTable，运行时提供只读查询。
/// </summary>
public sealed class DataRegistry
{
    private readonly Dictionary<Type, object> _tables = new();

    /// <summary>注册一个数据表。同类型重复注册将覆盖。</summary>
    public void RegisterTable<T>(IDataTable<T> table) where T : class
    {
        _tables[typeof(T)] = table;
    }

    /// <summary>获取指定类型的数据表。未注册时返回 null。</summary>
    public IDataTable<T>? GetTable<T>() where T : class
    {
        return _tables.TryGetValue(typeof(T), out var table)
            ? (IDataTable<T>)table
            : null;
    }
}
