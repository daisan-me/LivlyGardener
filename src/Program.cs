using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LivlyGardener {
partial class FormApp:Form {
    string root=AppDomain.CurrentDomain.BaseDirectory;
    string data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LivlyGardenerPrototype");
    TextBox adb=new TextBox(),serial=new TextBox(),log=new TextBox();
    Button inspect=new ModernButton(),start=new ModernButton(),stop=new ModernButton();
    NumericUpDown limit=new NumericUpDown();
    ComboBox actionDelay=new ComboBox();
    PictureBox preview=new PreviewPane();
    Label displayInfo=new Label{AutoSize=true,MaximumSize=new Size(510,70)};
    DisplayGeometry geometry; int skipped;Bitmap nativeFrame;
    Detector detector; CancellationTokenSource cancel; bool running;string completionReason;
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h,int id);
    public FormApp(string storageDirectory=null) {
        if(storageDirectory!=null)data=storageDirectory;
        Directory.CreateDirectory(data);detector=new Detector(Path.Combine(root,"assets"));BuildUi();
        inspect.Click+=async(s,e)=>await Begin(false);start.Click+=async(s,e)=>await Begin(true);stop.Click+=(s,e)=>RequestStop();
        FormClosing+=(s,e)=>{if(running){RequestStop();e.Cancel=true;Log("停止中です。処理終了後に閉じてください。");}};
        Shown+=(s,e)=>{if(storageDirectory==null&&!RegisterHotKey(Handle,1,0,0x77))Log("F8を登録できませんでした。停止ボタンを使用してください。");};
        FormClosed+=(s,e)=>{UnregisterHotKey(Handle,1);if(nativeFrame!=null)nativeFrame.Dispose();};
        Log("準備ができました。BlueStacksでゲームを開いてください。");
    }
    protected override void WndProc(ref Message m){if(m.Msg==0x0312)RequestStop();base.WndProc(ref m);}
    void Log(string s) {
        if(InvokeRequired){BeginInvoke(new Action<string>(Log),s);return;}
        string line=DateTime.Now.ToString("HH:mm:ss")+"  "+s;
        if(log.TextLength>24000)log.Text=log.Text.Substring(log.TextLength-12000);
        log.AppendText(line+Environment.NewLine);
        try {string path=Path.Combine(data,"run.log");if(File.Exists(path)&&new FileInfo(path).Length>262144)File.WriteAllText(path,"");File.AppendAllText(path,line+Environment.NewLine,Encoding.UTF8);}catch(IOException){}catch(UnauthorizedAccessException){}
    }
    void ShowFrame(Bitmap b){var copy=(Bitmap)b.Clone();BeginInvoke(new Action(()=>{var old=preview.Image;preview.Image=copy;if(old!=null)old.Dispose();}));}
    static string Quote(string s){if(s.Contains("\""))throw new Exception("パスに引用符を使用できません。");return "\""+s+"\"";}
    static byte[] Run(string exe,string args,CancellationToken ct) {
        var info=new ProcessStartInfo(exe,args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        using(var p=Process.Start(info))using(var output=new MemoryStream()) {
            var copy=p.StandardOutput.BaseStream.CopyToAsync(output);var error=p.StandardError.ReadToEndAsync();var sw=Stopwatch.StartNew();
            while(!p.WaitForExit(100)){if(ct.IsCancellationRequested || sw.Elapsed.TotalSeconds>25){try{p.Kill();}catch{}ct.ThrowIfCancellationRequested();throw new Exception("外部処理がタイムアウトしました。");}}
            copy.GetAwaiter().GetResult();var err=error.GetAwaiter().GetResult();ct.ThrowIfCancellationRequested();
            if(p.ExitCode!=0)throw new Exception("外部処理の失敗: "+err);return output.ToArray();
        }
    }
    Bitmap CaptureFrame(string exe,string device,CancellationToken ct) {
        var bytes=Run(exe,"-s "+device+" exec-out screencap -p",ct);
        using(var stream=new MemoryStream(bytes))using(var image=Image.FromStream(stream)) {
            if(geometry==null || geometry.Screenshot!=image.Size){
                string hierarchy="";
                try{Run(exe,"-s "+device+" shell uiautomator dump /data/local/tmp/livly-gardener-ui.xml",ct);hierarchy=Encoding.UTF8.GetString(Run(exe,"-s "+device+" shell cat /data/local/tmp/livly-gardener-ui.xml",ct));}
                catch(OperationCanceledException){throw;}catch(Exception ex){hierarchy="";Log("描画領域の取得は代替方式を使用: "+ex.Message);}
                geometry=DisplayGeometry.FromHierarchy(image.Size,hierarchy);
                string text="描画: "+image.Width+" × "+image.Height+" px / ゲーム領域: "+geometry.Viewport.Width+" × "+geometry.Viewport.Height+" px\n位置: "+geometry.Viewport.Left+", "+geometry.Viewport.Top+"（Windowsのウィンドウサイズには非依存）";
                BeginInvoke(new Action(()=>displayInfo.Text=text));Log(text.Replace("\n"," / "));
            }
            if(nativeFrame!=null)nativeFrame.Dispose();nativeFrame=new Bitmap(image);
            return geometry.Normalize(image);
        }
    }
    void Tap(string exe,string device,Point pt,string expected,CancellationToken ct,string target="auto") {
        // Re-acquire both resolution and evidence before input. Window size never enters this mapping.
        for(int attempt=0;attempt<3;attempt++)using(var current=CaptureFrame(exe,device,ct)) {
            var matches=Recognize(current,ct,target!="friend");
            if(target=="friend" ? !IsVisit(Detector.State(matches)) : Detector.State(matches)!=expected){Log("操作直前の画面を再確認しています（タップしません）。");Wait(ct,150);continue;}
            if(target=="friend")pt=matches["friend"].Point;
            else if(expected=="harvest" || expected=="elixir")pt=Detector.ActionPoint(matches,expected);
            else if(expected=="done"||expected=="訪問先・操作なし")pt=matches["friend"].Point;
            else if(expected=="収穫成功")pt=matches["ok"].Point;
            else if(expected=="移動メニュー")pt=matches["menu"].Point;
            Point mapped=geometry.Map(pt);ct.ThrowIfCancellationRequested();Run(exe,"-s "+device+" shell input tap "+mapped.X+" "+mapped.Y,ct);Log("タップ: "+(target=="friend"?"次のフレンド":expected)+" / Android座標 "+mapped.X+", "+mapped.Y);if(target=="friend" || expected=="移動メニュー")WaitForDeparture(exe,device,current,ct);else if(expected=="ホーム" || expected=="収穫成功")WaitForScreenChange(exe,device,expected,ct);return;
        }
        throw new Exception("操作前の画面が変わり、再確認でも一致しませんでした。");
    }
    void WaitForScreenChange(string exe,string device,string previous,CancellationToken ct) {
        var clock=Stopwatch.StartNew();
        while(clock.ElapsedMilliseconds<15000) {
            Wait(ct);
            using(var frame=CaptureFrame(exe,device,ct))if(Detector.State(detector.Read(frame))!=previous)return;
        }
        throw new Exception("操作後の画面切り替えを確認できませんでした。");
    }
    static bool IsVisit(string state){return state=="harvest" || state=="elixir" || state=="done" || state=="訪問先・操作なし";}
    void WaitForDeparture(string exe,string device,Bitmap previous,CancellationToken ct) {
        // The fixed name plate changes on loading/new arrival; do not act on the previous island twice.
        var clock=Stopwatch.StartNew();
        while(clock.ElapsedMilliseconds<20000) {
            Wait(ct,150);
            using(var next=CaptureFrame(exe,device,ct)) {
                double difference=0;int samples=0;
                for(int y=132;y<169;y+=3)for(int x=175;x<378;x+=3){var a=previous.GetPixel(x,y);var b=next.GetPixel(x,y);difference+=Math.Abs(a.R-b.R)+Math.Abs(a.G-b.G)+Math.Abs(a.B-b.B);samples+=3;}
                if(difference/samples>8)return;
            }
        }
        throw new Exception("移動先への画面切り替えを確認できませんでした。");
    }
    string ReadText(string file,string language,CancellationToken ct) {
        string ps=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","powershell.exe");
        string json=Encoding.UTF8.GetString(Run(ps,"-NoProfile -ExecutionPolicy Bypass -File "+Quote(Path.Combine(root,"ocr.ps1"))+" -ImagePath "+Quote(file)+" -Language "+language,ct));
        var parsed=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string,object>>(json);return (string)parsed["text"];
    }
    Bitmap OcrCrop(Bitmap fallback,Rectangle region,int mode) {
        if(nativeFrame==null || geometry==null)return Vision.Prepare(fallback,region,mode);
        var v=geometry.Viewport;
        var rect=Rectangle.FromLTRB(v.Left+region.Left*v.Width/552,v.Top+region.Top*v.Height/984,v.Left+region.Right*v.Width/552,v.Top+region.Bottom*v.Height/984);
        return Vision.Prepare(nativeFrame,rect,mode);
    }
    Dictionary<string,Match> Recognize(Bitmap frame,CancellationToken ct,bool allowOcr=true) {
        var matches=detector.Read(frame);
        if(!allowOcr || Detector.State(matches)!="訪問先・操作なし")return matches;
        var texts=new List<string>();
        for(int mode=0;mode<4;mode++) {
            ct.ThrowIfCancellationRequested();string path=Path.Combine(data,"action-"+mode+".png");
            using(var crop=OcrCrop(frame,new Rectangle(289,255,90,40),mode))crop.Save(path,ImageFormat.Png);
            try{texts.Add(ReadText(path,"en-US",ct));}catch(OperationCanceledException){throw;}catch(Exception ex){Log("文字の補助認識: "+ex.Message);break;}
        }
        string action=Vision.ParseAction(texts);
        if(action!=null){matches[action]=new Match{Name=action,Score=1,Point=new Point(326,276)};Log("文字の補助認識: "+action);}
        return matches;
    }
    async Task Begin(bool automate) {
        if(running)return;string exe=adb.Text,device=serial.Text.Trim();int cap=(int)limit.Value;int actionDelayMs=1000+actionDelay.SelectedIndex*200;
        if(!File.Exists(exe)){MessageBox.Show("ADB実行ファイルが見つかりません。");return;}
        if(!Regex.IsMatch(device,@"^127\.0\.0\.1:\d{1,5}$")){MessageBox.Show("接続先は 127.0.0.1:ポート番号 の形式で指定してください。");return;}
        cancel=new CancellationTokenSource();running=true;inspect.Enabled=start.Enabled=adb.Enabled=serial.Enabled=limit.Enabled=actionDelay.Enabled=false;stop.Enabled=true;RunAppearance(true,automate);
        try{await Task.Run(()=>Work(exe,device,cap,actionDelayMs,automate,cancel.Token));activity.Text=completionReason ?? (automate?"今回の処理を終了しました。実行ログで結果を確認できます。":"接続確認が終了しました。実行ログで認識結果を確認できます。");}
        catch(OperationCanceledException){activity.Text="停止しました。再開する場合は開始を押してください。";Log("停止しました。");}catch(Exception ex){activity.Text="処理を停止しました。実行ログをご確認ください。";Log("停止: "+ex.Message);}
        finally{running=false;inspect.Enabled=start.Enabled=adb.Enabled=serial.Enabled=limit.Enabled=actionDelay.Enabled=true;stop.Enabled=false;cancel.Dispose();cancel=null;RunAppearance(false,automate);}
    }
    void Wait(CancellationToken ct,int ms=150){if(ct.WaitHandle.WaitOne(ms))ct.ThrowIfCancellationRequested();}
    void ReturnHomeAndStop(string exe,string device,CancellationToken ct) {
        Log("Hom Powerが足りません：帰還して終了します。");Ui(()=>activity.Text="HPwr不足のため、ホームへ帰還しています。");
        bool returned=HomeReturn.Execute(()=>{
            using(var frame=CaptureFrame(exe,device,ct)) {
                ShowFrame(frame);var matches=detector.Read(frame);
                return new ReturnObservation{Home=Detector.State(matches)=="ホーム",CanReturn=matches["return"].Score>=.85 && matches["return"].Enabled,Button=geometry.Map(matches["return"].Point)};
            }
        },point=>{ct.ThrowIfCancellationRequested();Run(exe,"-s "+device+" shell input tap "+point.X+" "+point.Y,ct);Log("「かえる」を押しました。帰還を確認しています。");},()=>Wait(ct,500),ct);
        if(!returned)throw new Exception("HPwr不足で停止しましたが、ホームへの帰還を確認できませんでした。");
        completionReason="HPwr不足のため帰還し、自動停止しました。";Log(completionReason);
    }
    void Work(string exe,string device,int cap,int actionDelayMs,bool automate,CancellationToken ct) {
        geometry=null;skipped=0;completionReason=null;
        string connected=Encoding.UTF8.GetString(Run(exe,"connect "+device,ct));Log(connected.Trim());
        Log(Encoding.UTF8.GetString(Run(exe,"-s "+device+" shell wm size",ct)).Trim());
        Log(Encoding.UTF8.GetString(Run(exe,"-s "+device+" shell wm density",ct)).Trim());
        string pending="";int visits=0,completed=0,noProgress=0;DateTime deadline=DateTime.UtcNow.AddMinutes(60);
        bool navigating=false;string lastState="";int stable=0;var recovery=new RecognitionRecovery();var unclear=Stopwatch.StartNew();var resultClock=Stopwatch.StartNew();
        while(true) {
            ct.ThrowIfCancellationRequested();if(DateTime.UtcNow>deadline)throw new Exception("実行時間の上限（60分）に達しました。");
            using(var frame=CaptureFrame(exe,device,ct)) {
                ShowFrame(frame);
                if(automate && detector.HasShortage(frame)){ReturnHomeAndStop(exe,device,ct);return;}
                var matches=Recognize(frame,ct,false);string state=Detector.State(matches);
                if(state=="訪問先・操作なし" && pending=="" && unclear.ElapsedMilliseconds>=2500){matches=Recognize(frame,ct);state=Detector.State(matches);}
                Ui(()=>activity.Text=state=="harvest"?"木の実の収穫を確認しています。":state=="elixir"?"水やりを確認しています。":"現在の画面："+state);
                Log("画面: "+state+" / "+string.Join(" ",matches.Where(x=>new[]{"home","menu","harvest","elixir","fruit-shape","water-shape","done","friend","success"}.Contains(x.Key)).Select(x=>x.Key+"="+x.Value.Score.ToString("F2"))));
                if(!automate)return;
                if(state==lastState)stable++;else{lastState=state;stable=1;}
                if(stable<2 && state!="harvest" && state!="elixir"){Wait(ct,150);continue;}
                if(state=="不明" || (state=="訪問先・操作なし" && pending=="")) {
                    if(unclear.ElapsedMilliseconds<15000){Wait(ct);continue;}
                    string next=recovery.Next(state=="訪問先・操作なし",pending!="");
                    if(next=="retry"){Log("読み取りを再試行します（タップしません）。");Wait(ct);continue;}
                    if(next=="stop")throw new Exception("フレンド画面を確認できません。未知の画面は操作せず停止します。");
                    if(navigating||visits==0){visits++;navigating=false;}skipped++;int skippedCount=skipped;Ui(()=>skippedValue.Text=skippedCount.ToString());Log("読めない島を未処理として記録し、次へ進みます。未処理数: "+skipped);
                    if(visits>=cap || ++noProgress>=10){Log("巡回上限または連続未処理上限です。");return;}
                    Tap(exe,device,matches["friend"].Point,state,ct,"friend");navigating=true;unclear.Restart();Wait(ct);continue;
                }
                recovery.Reset();unclear.Restart();
                if(state=="収穫成功") {
                    if(pending!="harvest")throw new Exception("開始前の収穫ダイアログを手動で閉じてください。");
                    Tap(exe,device,matches["ok"].Point,state,ct);pending="confirmed";Log("収穫成功を確認し、OKを押しました。");Wait(ct);continue;
                }
                if(state=="ホーム") {
                    if(pending!=""||visits>0)throw new Exception("巡回中にホームへ戻りました。");
                    if(stable>=4)throw new Exception("移動メニューが開きませんでした。");
                    Tap(exe,device,new Point(323,921),state,ct);Wait(ct);continue;
                }
                if(state=="移動メニュー"){if(stable>=4)throw new Exception("フレンドへの移動を確認できませんでした。");Tap(exe,device,matches["menu"].Point,state,ct);navigating=true;unclear.Restart();Wait(ct);continue;}
                if(state=="harvest"||state=="elixir"||state=="done"||state=="訪問先・操作なし") {
                    if(pending=="harvest") {
                        if(resultClock.ElapsedMilliseconds<15000){Log("操作結果の反映を待っています。再タップはしません。");Wait(ct);continue;}
                        throw new Exception("操作結果を確認できません。重複操作を避けて停止します。");
                    }
                    bool justCompleted=pending=="confirmed" || pending=="elixir";
                    if(justCompleted) {completed++;int completedCount=completed;Ui(()=>completedValue.Text=completedCount.ToString());noProgress=0;Log("操作完了数: "+completed);pending="";}
                    if(navigating || visits==0){visits++;navigating=false;}
                    if(visits>cap){Log("巡回上限に達しました。");return;}
                    if(justCompleted){if(visits>=cap){Log("巡回上限に達しました。");return;}Tap(exe,device,matches["friend"].Point,state,ct,"friend");navigating=true;unclear.Restart();Wait(ct);continue;}
                    if(state=="harvest"||state=="elixir") {
                        Tap(exe,device,Point.Empty,state,ct);pending=state;resultClock.Restart();
                        string waiting="反映待ち："+(actionDelayMs/1000.0).ToString("F1")+" 秒";
                        Log(state+" を実行 / "+waiting);Ui(()=>activity.Text=waiting+"（この間は次の操作へ進みません）");
                        bool insufficient=ActionWait.Observe(actionDelayMs,ct,()=>{using(var observed=CaptureFrame(exe,device,ct))return detector.HasShortage(observed);});
                        if(insufficient){ReturnHomeAndStop(exe,device,ct);return;}
                        continue;
                    }
                    // A missing action label is not proof that the tree is finished.
                    if(state=="訪問先・操作なし" && !justCompleted)throw new Exception("操作名を認識できません。判定画像を確認してください。");
                    if(visits>=cap){Log("巡回上限に達しました。");return;}
                    if(++noProgress>=10)throw new Exception("10回連続で作業できなかったため停止しました。");
                    Tap(exe,device,matches["friend"].Point,state,ct,"friend");navigating=true;unclear.Restart();Wait(ct);continue;
                }
            }
        }
    }
}
class Program {
    [STAThread] static void Main() {
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new FormApp());
    }
}
}
