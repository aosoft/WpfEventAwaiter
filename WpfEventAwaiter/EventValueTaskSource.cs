using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.Extensions.ObjectPool;

namespace WpfEventAwaiter;

public class EventValueTaskSource<TEventHandler, TEventArgs> : IValueTaskSource<TEventArgs>
    where TEventHandler : Delegate where TEventArgs : EventArgs
{
    private readonly TEventHandler _eventHandler;
    private ManualResetValueTaskSourceCore<TEventArgs> _core;

    private Action<TEventHandler>? _removeHandler;

    private EventValueTaskSource()
    {
        _eventHandler = (TEventHandler)Delegate.CreateDelegate(typeof(TEventHandler), this, nameof(OnEvent));
    }

    public static EventValueTaskSource<TEventHandler, TEventArgs> Create(Action<TEventHandler> addHandler,
        Action<TEventHandler> removeHandler)
    {
        var r = Pool.Get();
        r._core.Reset();
        addHandler(r._eventHandler);
        r._removeHandler = removeHandler;
        return r;
    }

    private void OnEvent(object? sender, TEventArgs e)
    {
        if (_removeHandler != null)
        {
            _removeHandler(_eventHandler);
            _removeHandler = null;
        }

        _core.SetResult(e);
    }


    public ValueTask<TEventArgs> AsValueTask() => new ValueTask<TEventArgs>(this, _core.Version);

    TEventArgs IValueTaskSource<TEventArgs>.GetResult(short token)
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

    ValueTaskSourceStatus IValueTaskSource<TEventArgs>.GetStatus(short token) => _core.GetStatus(token);

    void IValueTaskSource<TEventArgs>.OnCompleted(Action<object?> continuation, object? state, short token,
        ValueTaskSourceOnCompletedFlags flags) =>
        _core.OnCompleted(continuation, state, token, flags);


    private static readonly ObjectPool<EventValueTaskSource<TEventHandler, TEventArgs>> Pool =
        new DefaultObjectPool<EventValueTaskSource<TEventHandler, TEventArgs>>(new PooledObjectPolicy());

    private class PooledObjectPolicy : PooledObjectPolicy<EventValueTaskSource<TEventHandler, TEventArgs>>
    {
        public override EventValueTaskSource<TEventHandler, TEventArgs> Create() => new EventValueTaskSource<TEventHandler, TEventArgs>();
        public override bool Return(EventValueTaskSource<TEventHandler, TEventArgs> obj) => true;
    }
}