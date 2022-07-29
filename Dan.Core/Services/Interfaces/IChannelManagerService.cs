namespace Dan.Core.Services.Interfaces;

public interface IChannelManagerService
{
    void Add<T>(string endpoint);
    T Get<T>() where T : class;
    Task<object> With<T>(Func<T, Task<object>> func) where T : class;
}