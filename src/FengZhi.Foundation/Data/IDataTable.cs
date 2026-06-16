namespace FengZhi.Foundation.Data;

/// <summary>
/// 只读数据表接口。启动时一次性加载，运行时 O(1) 查询。
/// </summary>
public interface IDataTable<T> where T : class
{
    /// <summary>按 ID 获取单条记录。不存在时返回 null。</summary>
    T? Get(string id);

    /// <summary>获取全部已注册记录。</summary>
    IReadOnlyList<T> GetAll();

    /// <summary>已注册记录数量。</summary>
    int Count { get; }
}
