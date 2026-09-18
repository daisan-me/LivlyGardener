using System;
using System.Diagnostics;
using System.Threading;

namespace LivlyGardener {
class ReturnObservation {
    public bool Home,CanReturn;
    public System.Drawing.Point Button;
}
static class HomeReturn {
    public static bool Execute(Func<ReturnObservation> observe,Action<System.Drawing.Point> tap,Action wait,CancellationToken token) {
        bool sent=false;var clock=Stopwatch.StartNew();
        while(clock.Elapsed.TotalSeconds<20) {
            token.ThrowIfCancellationRequested();var frame=observe();token.ThrowIfCancellationRequested();
            if(frame.Home)return true;
            if(clock.Elapsed.TotalSeconds>=20)return false;
            if(!sent && frame.CanReturn){tap(frame.Button);sent=true;}
            wait();
        }return false;
    }
}
static class ActionWait {
    public static bool Observe(int milliseconds,CancellationToken token,Func<bool> detectShortage) {
        if(milliseconds<1000 || milliseconds>5000 || milliseconds%200!=0)throw new ArgumentOutOfRangeException("milliseconds");
        var clock=Stopwatch.StartNew();bool shortage=false;
        do {
            token.ThrowIfCancellationRequested();
            if(!shortage)shortage=detectShortage();
            int remaining=milliseconds-(int)clock.ElapsedMilliseconds;
            if(remaining<=0)break;
            if(token.WaitHandle.WaitOne(Math.Min(250,remaining)))token.ThrowIfCancellationRequested();
        }while(true);
        token.ThrowIfCancellationRequested();return shortage;
    }
}
}
