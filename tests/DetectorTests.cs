using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using LivlyGardener;
class DetectorTests {
    static Bitmap Scene(string assets,string action,int mode) {
        var b=new Bitmap(552,984,PixelFormat.Format24bppRgb);
        using(var g=Graphics.FromImage(b)){
            g.Clear(Color.White);using(var friend=new Bitmap(Path.Combine(assets,"friend.png")))g.DrawImageUnscaled(friend,248,934);
            if(action!=null)using(var template=new Bitmap(Path.Combine(assets,action+".png"))) {
                for(int y=0;y<template.Height;y++)for(int x=0;x<template.Width;x++){
                    var c=template.GetPixel(x,y);int v=(int)(.299*c.R+.587*c.G+.114*c.B);
                    if(mode==1)v=255-v;else if(mode==2)v=225+(int)(v*.115);else if(mode==3)v=255;
                    if(mode!=0)template.SetPixel(x,y,Color.FromArgb(v,v,v));
                }g.DrawImageUnscaled(template,action=="fruit-icon"?260:296,action=="fruit-icon"?299:267);
            }
        }return b;
    }
    static int Main(string[] args) {
        string assets=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets");var d=new Detector(assets);int failures=0;
        string[][] cases={new[]{"不明"},new[]{"ホーム","home"},new[]{"移動メニュー","menu"},new[]{"harvest","friend","harvest"},new[]{"elixir","friend","elixir"},new[]{"done","friend","done"},new[]{"訪問先・操作なし","friend"},new[]{"収穫成功","success","ok"},new[]{"不明","ok"},new[]{"不明","success"},new[]{"不明","harvest"}};
        foreach(var test in cases)using(var b=new Bitmap(552,984,PixelFormat.Format24bppRgb)) {
            using(var g=Graphics.FromImage(b)){g.Clear(Color.FromArgb(80,80,80));foreach(string name in test.SkipOne())using(var t=new Bitmap(Path.Combine(assets,name+".png"))){Point p=name=="home"?new Point(297,944):name=="menu"?new Point(291,746):name=="friend"?new Point(248,934):name=="success"?new Point(208,145):name=="ok"?new Point(244,771):new Point(296,267);g.DrawImageUnscaled(t,p);}}
            string state=Detector.State(d.Read(b));Console.WriteLine(test[0]+" -> "+state);if(state!=test[0])failures++;
        }
        foreach(string action in new[]{"harvest","elixir"})foreach(int mode in new[]{1,2,3})using(var b=Scene(assets,action,mode)) {
            string expected=mode==3?"訪問先・操作なし":action;string actual=Detector.State(d.Read(b));Console.WriteLine("White/inverse "+action+" mode="+mode+" -> "+actual);if(actual!=expected)failures++;
        }
        using(var b=Scene(assets,"fruit-icon",0)){string state=Detector.State(d.Read(b));Console.WriteLine("Icon only -> "+state);if(state!="harvest")failures++;}
        using(var b=Scene(assets,"elixir",0)){using(var g=Graphics.FromImage(b))using(var brush=new SolidBrush(Color.FromArgb(120,0,0,0)))g.FillRectangle(brush,0,0,b.Width,b.Height);string state=Detector.State(d.Read(b));Console.WriteLine("Dimmed modal background -> "+state);if(state!="不明")failures++;}
        foreach(var size in new[]{new Size(360,640),new Size(720,1280),new Size(900,1600),new Size(1080,1920)})using(var b=Scene(assets,"elixir",0))using(var scaled=new Bitmap(b,size))using(var n=Detector.Normalize(scaled)){
            string state=Detector.State(d.Read(n));Console.WriteLine("Resolution "+size+" -> "+state);if(state!="elixir")failures++;
        }
        if(args.Length>0)foreach(string name in new[]{"live-v02.png","live-next.png"})if(File.Exists(Path.Combine(args[0],name)))using(var raw=new Bitmap(Path.Combine(args[0],name)))using(var b=Detector.Normalize(raw)) {
            var m=d.Read(b);Console.WriteLine("Water shape regression "+name+" -> "+Detector.State(m));
            if(Detector.State(m)!="elixir" || Detector.ActionPoint(m,"elixir").X<260 || Detector.ActionPoint(m,"elixir").X>292)failures++;
        }
        if(args.Length>0 && File.Exists(Path.Combine(args[0],"v06-after-water.png")))using(var raw=new Bitmap(Path.Combine(args[0],"v06-after-water.png")))using(var b=Detector.Normalize(raw)){var m=d.Read(b);Console.WriteLine("Done over droplet regression -> "+Detector.State(m));if(Detector.State(m)!="done")failures++;}
        using(var blank=Scene(assets,null,0)){var m=d.Read(blank);if(Detector.ShapeAction(m)!=null)failures++;}
        var geo=DisplayGeometry.FromHierarchy(new Size(1100,1700),"<hierarchy><node resource-id='jp.cocone.livly:id/unitySurfaceView' bounds='[100,40][1000,1640]' /></hierarchy>");
        if(geo.Map(new Point(276,492))!=new Point(550,840))failures++;
        try{geo.Map(new Point(553,984));failures++;}catch(ArgumentException){}
        var r=new RecognitionRecovery();for(int i=0;i<3;i++)if(r.Next(true,false)!="retry")failures++;if(r.Next(true,false)!="skip")failures++;
        for(int i=0;i<3;i++)r.Next(true,true);if(r.Next(true,true)!="stop")failures++;
        for(int i=0;i<3;i++)r.Next(false,false);if(r.Next(false,false)!="stop")failures++;
        if(Vision.ParseAction(new[]{"/harvest"," / h a r v e s t "})!="harvest")failures++;
        if(Vision.ParseAction(new[]{"/harvest","/elixir"})!=null)failures++;
        if(Vision.ParseAction(new[]{"elixir shop"})!=null)failures++;
        if(Vision.ParseAction(new[]{"/el1xir"})!="elixir")failures++;
        if(args.Length>0 && File.Exists(Path.Combine(args[0],"home-bug.png")))using(var raw=new Bitmap(Path.Combine(args[0],"home-bug.png")))using(var b=Detector.Normalize(raw)){string state=Detector.State(d.Read(b));Console.WriteLine("Clean home regression -> "+state);if(state!="ホーム")failures++;}
        if(args.Length>0){string[] expected={"ホーム","移動メニュー","harvest","収穫成功","訪問先・操作なし","elixir","done"};for(int i=0;i<7;i++)using(var b=new Bitmap(Path.Combine(args[0],"fixture"+(i+1)+".png"))){string state=Detector.State(d.Read(b));Console.WriteLine("Fixture "+(i+1)+" -> "+state);if(state!=expected[i])failures++;}}
        foreach(var size in new[]{new Size(360,640),new Size(552,984),new Size(900,1600)})using(var b=Scene(assets,"elixir",0)) {
            using(var g=Graphics.FromImage(b))using(var notice=new Bitmap(Path.Combine(assets,"shortage.png")))g.DrawImageUnscaled(notice,249,98);
            using(var scaled=new Bitmap(b,size))using(var n=Detector.Normalize(scaled)){var detected=d.Read(n);Console.WriteLine("Notice "+size+" -> "+detected["shortage"].Score.ToString("F3"));if(!d.HasShortage(n)||Detector.State(detected)!="HPwr不足")failures++;}
        }
        using(var white=new Bitmap(552,984)) {using(var g=Graphics.FromImage(white)){g.Clear(Color.White);g.DrawString("Hom Power 403/403",new Font("Arial",13),Brushes.Black,249,98);}if(d.HasShortage(white))failures++;}
        if(args.Length>0 && File.Exists(Path.Combine(args[0],"shortage.png")))using(var b=new Bitmap(Path.Combine(args[0],"shortage.png"))){var m=d.Read(b);Console.WriteLine("Shortage fixture: "+Detector.State(m)+" / return="+m["return"].Score.ToString("F3")+" enabled="+m["return"].Enabled);if(!d.HasShortage(b)||m["return"].Score<.85||!m["return"].Enabled)failures++;}
        // Actual BlueStacks notification differs from the original reference font rasterization.
        if(args.Length>0 && File.Exists(Path.Combine(args[0],"shortage-live.png")))using(var raw=new Bitmap(Path.Combine(args[0],"shortage-live.png"))) {
            foreach(var size in new[]{new Size(360,640),raw.Size,new Size(1080,1920)})using(var scaled=new Bitmap(raw,size))using(var b=Detector.Normalize(scaled)) {
                var m=d.Read(b);var back=m["return"];
                Console.WriteLine("Live shortage "+size+" -> "+Detector.State(m)+" return="+back.Score.ToString("F3")+" at "+back.Point);
                if(!d.HasShortage(b)||Detector.State(m)!="HPwr不足"||back.Score<.85||!back.Enabled||back.Point.X>=110||back.Point.Y<880)failures++;
            }
            using(var b=Detector.Normalize(raw)) {
                var m=d.Read(b);int steps=0,backTaps=0;
                bool returned=HomeReturn.Execute(()=>++steps==1?new ReturnObservation{CanReturn=m["return"].Enabled&&m["return"].Score>=.85,Button=m["return"].Point}:new ReturnObservation{Home=true},p=>{backTaps++;if(p.X>=110)failures++;},()=>{},CancellationToken.None);
                if(!returned||backTaps!=1)failures++;
            }
        }
        int polls=0;var clock=Stopwatch.StartNew();bool latched=ActionWait.Observe(1000,CancellationToken.None,()=>++polls==2);
        if(!latched || polls!=2 || clock.ElapsedMilliseconds<1000)failures++;
        using(var cancellation=new CancellationTokenSource()){cancellation.CancelAfter(100);clock.Restart();try{ActionWait.Observe(5000,cancellation.Token,()=>false);failures++;}catch(OperationCanceledException){}if(clock.ElapsedMilliseconds>1000)failures++;}
        Console.WriteLine("Delay: transient notification retained; configured wait and cancellation checked.");
        int observations=0,taps=0;
        bool home=HomeReturn.Execute(()=>new ReturnObservation{Home=++observations==3,CanReturn=true,Button=new Point(70,930)},p=>{if(p!=new Point(70,930))failures++;taps++;},()=>{},CancellationToken.None);
        if(!home||taps!=1||observations!=3)failures++;
        taps=0;home=HomeReturn.Execute(()=>new ReturnObservation{Home=true,CanReturn=true},p=>taps++,()=>{},CancellationToken.None);if(!home||taps!=0)failures++;
        using(var stopped=new CancellationTokenSource()){taps=0;try{HomeReturn.Execute(()=>{stopped.Cancel();return new ReturnObservation{CanReturn=true};},p=>taps++,()=>{},stopped.Token);failures++;}catch(OperationCanceledException){}if(taps!=0)failures++;}
        Console.WriteLine("Return: one tap only, stop at home, cancellation before input checked.");
        Console.WriteLine("Failures: "+failures);return failures==0?0:1;
    }
}
static class TestExtensions {public static System.Collections.Generic.IEnumerable<string> SkipOne(this string[] values){for(int i=1;i<values.Length;i++)yield return values[i];}}
