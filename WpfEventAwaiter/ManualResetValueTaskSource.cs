using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.Extensions.ObjectPool;

namespace WpfEventAwaiter;

public sealed class ManualResetValueTaskSource : IValueTaskSource
{
    private ManualResetValueTaskSourceCore<int> _core;

    private ManualResetValueTaskSource()
    {
        
    }

    public static ManualResetValueTaskSource Create()
    {
        var r = Pool.Get();
        r._core.Reset();
        return r;
    }
    
    public void SetResult() => _core.SetResult(0);
    public void SetException(Exception error) => _core.SetException(error);

    public ValueTask ToValueTask() => new ValueTask(this, _core.Version);
    
    public void GetResult(short token)
    {
        try
        {
            _core.GetResult(token);
        }
        finally
        {
            Pool.Return(this);
        }
    }

    public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

    public void OnCompleted(Action<object?> continuation, object? state, short token,
        ValueTaskSourceOnCompletedFlags flags) =>
        _core.OnCompleted(continuation, state, token, flags);

    
    private static readonly ObjectPool<ManualResetValueTaskSource> Pool =
        new DefaultObjectPool<ManualResetValueTaskSource>(new PooledObjectPolicy());

    private class PooledObjectPolicy : PooledObjectPolicy<ManualResetValueTaskSource>
    {
        public override ManualResetValueTaskSource Create() => new ManualResetValueTaskSource();
        public override bool Return(ManualResetValueTaskSource obj) => true;
    }
}

public sealed class ManualResetValueTaskSource<T> : IValueTaskSource<T>
{
    private ManualResetValueTaskSourceCore<T> _core;

    private ManualResetValueTaskSource()
    {
        
    }

    public static ManualResetValueTaskSource<T> Create()
    {
        var r = Pool.Get();
        r._core.Reset();
        return r;
    }
    
    public void SetResult(T result) => _core.SetResult(result);
    public void SetException(Exception error) => _core.SetException(error);
    
    public ValueTask<T> ToValueTask() => new ValueTask<T>(this, _core.Version);
    
    public T GetResult(short token)
    {
        try
        {
            return _core.GetResult(token);
        }
        finally
        {
            Pool.Return(this);
        }
    }

    public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

    public void OnCompleted(Action<object?> continuation, object? state, short token,
        ValueTaskSourceOnCompletedFlags flags) =>
        _core.OnCompleted(continuation, state, token, flags);

    
    private static readonly ObjectPool<ManualResetValueTaskSource<T>> Pool =
        new DefaultObjectPool<ManualResetValueTaskSource<T>>(new PooledObjectPolicy());

    private class PooledObjectPolicy : PooledObjectPolicy<ManualResetValueTaskSource<T>>
    {
        public override ManualResetValueTaskSource<T> Create() => new ManualResetValueTaskSource<T>();
        public override bool Return(ManualResetValueTaskSource<T> obj) => true;
    }
}
