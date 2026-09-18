using System;using System.Drawing;using System.Windows.Forms;
namespace LivlyGardener {
partial class FormApp {
    public void ExportUi(string folder){System.IO.Directory.CreateDirectory(folder);StartPosition=FormStartPosition.Manual;Location=new Point(-20000,-20000);ShowInTaskbar=false;Opacity=0;Show();Application.DoEvents();ExportPage(System.IO.Path.Combine(folder,"ui-home.png"));SwitchPage(true);ExportPage(System.IO.Path.Combine(folder,"ui-settings.png"));SwitchPage(false);ClientSize=new Size(764,591);ExportPage(System.IO.Path.Combine(folder,"ui-compact.png"));Hide();}
    void ExportPage(string path){PerformLayout();LayoutChildren(this);using(var bmp=new Bitmap(Width,Height)){DrawToBitmap(bmp,new Rectangle(0,0,Width,Height));bmp.Save(path,System.Drawing.Imaging.ImageFormat.Png);}}
    static void LayoutChildren(Control c){c.PerformLayout();foreach(Control child in c.Controls)LayoutChildren(child);}
}
class UiPreview {
    [STAThread] static void Main(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);using(var form=new FormApp(System.IO.Path.Combine(args[0],"diagnostics")))form.ExportUi(args[0]);}
}
}
