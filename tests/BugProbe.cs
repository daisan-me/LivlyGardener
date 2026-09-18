using System;using System.IO;using System.Drawing;using System.Linq;using System.Threading;using System.Threading.Tasks;using System.Windows.Forms;
namespace LivlyGardener {
partial class FormApp {
    public void RunOnce(string report){Opacity=0;ShowInTaskbar=false;Shown+=async(s,e)=>{string exe=adb.Text,device=serial.Text;using(var timeout=new CancellationTokenSource(180000)){try{await Task.Run(()=>Work(exe,device,3,4000,true,timeout.Token));}catch(Exception ex){Log("PROBE: "+ex.Message);}finally{File.WriteAllText(report,log.Text);Close();}}};Application.Run(this);}
    public void Probe(string image,string report){using(var raw=new Bitmap(image))using(var frame=Detector.Normalize(raw)){nativeFrame=new Bitmap(raw);geometry=DisplayGeometry.FromHierarchy(raw.Size,null);var m=Recognize(frame,CancellationToken.None);string text=Detector.State(m)+"\n"+string.Join("\n",m.Select(x=>x.Key+"="+x.Value.Score));File.WriteAllText(report,text);}}
}
class BugProbe {
    [STAThread] static void Main(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);using(var form=new FormApp(Path.Combine(Path.GetDirectoryName(args[1]),"probe-data"))){if(args[0]=="--run-one")form.RunOnce(args[1]);else form.Probe(args[0],args[1]);}}
}
}
