using System;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace WpfEventAwaiter;

public static class TimeLineExtensions
{
    public static Task BeginTypeAAsync(this Storyboard self)
    {
        var tcs = new TaskCompletionSource();
        EventHandler? h = null;
        h = (_, _) =>
        {
            self.Completed -= h;
            tcs.SetResult();
        };
        self.Completed += h;

        self.Begin();
        return tcs.Task;
    }

    public static ValueTask BeginTypeBAsync(this Storyboard self)
    {
        var vts = ManualResetValueTaskSource.Create();
        EventHandler? h = null;
        h = (_, _) =>
        {
            self.Completed -= h;
            vts.SetResult();
        };
        self.Completed += h;

        self.Begin();
        return vts.ToValueTask();
    }

    public static ValueTask BeginTypeCAsync(this Storyboard self)
    {
        var r = TimelineCompletedValueTaskSource.Create(self);
        self.Begin();
        return r.ToValueTask();
    }
    
    public static TimelineCompletedAwaitable BeginTypeDAsync(this Storyboard self)
    {
        var r = TimelineCompletedAwaitable.Create(self);
        self.Begin();
        return r;
    }
    
}
