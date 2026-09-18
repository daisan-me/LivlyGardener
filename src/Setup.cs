using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
class Setup {
    [STAThread] static void Main() {
        Application.EnableVisualStyles();
        string target=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","LivlyGardener");
        if(MessageBox.Show("Livly Gardener 0.8をインストールします。\n\n保存先: "+target+"\n\nBlueStacksは別途必要です。続行しますか？","Livly Gardener Setup",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;
        try {
            Directory.CreateDirectory(target);
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("package.zip"))using(var zip=new ZipArchive(stream,ZipArchiveMode.Read)) {
                foreach(var entry in zip.Entries) {
                    string path=Path.GetFullPath(Path.Combine(target,entry.FullName));
                    if(!path.StartsWith(target+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Invalid archive path");
                    if(string.IsNullOrEmpty(entry.Name)){Directory.CreateDirectory(path);continue;}
                    Directory.CreateDirectory(Path.GetDirectoryName(path));entry.ExtractToFile(path,true);
                }
            }
            Type shell=Type.GetTypeFromProgID("WScript.Shell");dynamic obj=Activator.CreateInstance(shell);
            string shortcut=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"Livly Gardener.lnk");
            dynamic link=obj.CreateShortcut(shortcut);link.TargetPath=Path.Combine(target,"LivlyGardener.exe");link.WorkingDirectory=target;link.IconLocation=Path.Combine(target,"LivlyGardener.exe")+",0";link.Save();
            string desktop=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"Livly Gardener.lnk");
            dynamic desktopLink=obj.CreateShortcut(desktop);desktopLink.TargetPath=link.TargetPath;desktopLink.WorkingDirectory=target;desktopLink.IconLocation=link.IconLocation;desktopLink.Save();
            MessageBox.Show("インストールが完了しました。Livly Gardenerを起動します。\n次回からはデスクトップまたはスタートメニューから起動できます。","完了");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.Combine(target,"LivlyGardener.exe")){UseShellExecute=true,WorkingDirectory=target});
        } catch(Exception ex){MessageBox.Show("インストールできませんでした。起動中のアプリを閉じて再試行してください。\n"+ex.Message);}
    }
}
