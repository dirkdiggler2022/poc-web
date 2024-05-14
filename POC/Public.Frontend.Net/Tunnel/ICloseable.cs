public interface ICloseable
{
    bool IsClosed { get; }
    void Abort();
}
