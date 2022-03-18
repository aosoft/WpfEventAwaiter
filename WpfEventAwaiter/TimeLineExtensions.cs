using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace WpfEventAwaiter;

public static class TimeLineExtensions
{
    public static Task BeginTypeAAsync(this Storyboard self, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource();
        EventHandler? h = null;
        h = (_, _) =>
        {
            self.Completed -= h;
            tcs.SetResult();
        };
        self.Completed += h;
        ct.Register(() =>
        {
            self.Completed -= h;
            tcs.SetCanceled();
        });

        try
        {
            self.Begin();
            return tcs.Task;
        }
        catch
        {
            self.Completed -= h;
            throw;
        }
    }

    public static ValueTask BeginTypeBAsync(this Storyboard self, CancellationToken ct = default)
    {
        var vts = ManualResetValueTaskSource.Create();
        EventHandler? h = null;
        h = (_, _) =>
        {
            self.Completed -= h;
            vts.SetResult();
        };
        self.Completed += h;
        ct.Register(() =>
        {
            self.Completed -= h;
            vts.SetCanceled();
        });

        try
        {
            self.Begin();
            return vts.AsValueTask();
        }
        catch
        {
            self.Completed -= h;
            throw;
        }
    }

    public static ValueTask<EventArgs> BeginTypeCAsync(this Storyboard self)
    {
        var r = EventValueTaskSource<Timeline, EventHandler, EventArgs>.Create(self,
            static (t, h) => t.Completed += h,
            static (t, h) => t.Completed -= h);
        try
        {
            self.Begin();
            return r.AsValueTask();
        }
        catch
        {
            r.Release();
            throw;
        }
    }

    public static TimelineCompletedAwaitable BeginTypeDAsync(this Storyboard self)
    {
        var r = TimelineCompletedAwaitable.Create(self);
        self.Begin();
        return r;
    }
}