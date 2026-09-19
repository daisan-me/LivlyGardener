using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LivlyGardener {
static class Palette {
    public static Color Ink=Color.FromArgb(35,53,48),Muted=Color.FromArgb(113,128,120),Green=Color.FromArgb(38,117,89),Canvas=Color.FromArgb(245,247,243),Line=Color.FromArgb(225,232,224);
    public static GraphicsPath Round(Rectangle r,int radius){var p=new GraphicsPath();int d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
}
class Card:Panel {
    public Color Fill=Color.White;
    public Card(){DoubleBuffered=true;BackColor=Palette.Canvas;Padding=new Padding(22);}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.Clear(Palette.Canvas);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Palette.Round(new Rectangle(0,0,Width-1,Height-1),16))using(var b=new SolidBrush(Fill))using(var pen=new Pen(Palette.Line)){e.Graphics.FillPath(b,path);e.Graphics.DrawPath(pen,path);}}
}
class ModernButton:Button {
    public bool Primary;public bool Destructive;public bool Selected;bool hover;
    public ModernButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;Font=new Font("Yu Gothic UI",10,FontStyle.Bold);Height=44;UseVisualStyleBackColor=false;SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e){
        Control ancestor=Parent;while(ancestor!=null && ancestor.BackColor.A<255)ancestor=ancestor.Parent;e.Graphics.Clear(ancestor==null?Palette.Canvas:ancestor.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        Color fill=!Enabled?Color.FromArgb(239,242,238):Primary?(hover?Color.FromArgb(28,95,71):Palette.Green):Selected?Color.FromArgb(223,237,225):hover?Color.FromArgb(235,241,234):Color.White;
        Color ink=!Enabled?Color.FromArgb(153,165,156):Primary?Color.White:Destructive?Color.FromArgb(170,73,66):Palette.Ink;
        using(var path=Palette.Round(new Rectangle(0,0,Width-1,Height-1),10))using(var b=new SolidBrush(fill))using(var pen=new Pen(Primary?fill:Palette.Line)){e.Graphics.FillPath(b,path);e.Graphics.DrawPath(pen,path);}
        TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(5,5,Width-10,Height-10),ink,fill);
    }
}
class LeafMark:Control {
    protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var b=new SolidBrush(Palette.Green))e.Graphics.FillEllipse(b,0,0,40,40);using(var b=new SolidBrush(Color.FromArgb(212,233,183))){e.Graphics.FillEllipse(b,12,10,16,10);e.Graphics.FillEllipse(b,8,19,13,8);}using(var pen=new Pen(Color.White,1.6f)){e.Graphics.DrawLine(pen,16,31,24,12);}}
}
class PreviewPane:PictureBox {
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Image==null)TextRenderer.DrawText(e.Graphics,"画面の取得を待っています\n\nBlueStacksでゲームを開いて\n「接続を確認」を押してください。",Font,ClientRectangle,Palette.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);}
}
partial class FormApp {
    Label badge,completedValue,skippedValue,activity,elapsedLabel;
    Panel normalPage,settingsPage;ModernButton homeNav,settingsNav;
    System.Windows.Forms.Timer uiTimer;DateTime startedAt;bool timing;
    static Label TextLabel(string text,float size,Color color,bool bold=false){return new Label{Text=text,AutoSize=false,Font=new Font("Yu Gothic UI",size,bold?FontStyle.Bold:FontStyle.Regular),ForeColor=color,BackColor=Color.Transparent,TextAlign=ContentAlignment.MiddleLeft,Dock=DockStyle.Fill};}
    static TableLayoutPanel Grid(int rows){var p=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=rows,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};return p;}
    void BuildUi() {
        Text="Livly Gardener 0.8.1";ClientSize=new Size(820,620);MinimumSize=new Size(780,630);
        Icon=new Icon(System.IO.Path.Combine(root,"assets","app.ico"));FormClosed+=(s,e)=>Icon.Dispose();StartPosition=FormStartPosition.CenterScreen;
        Font=new Font("Yu Gothic UI",10);ForeColor=Palette.Ink;BackColor=Palette.Canvas;AutoScaleMode=AutoScaleMode.Dpi;DoubleBuffered=true;
        var shell=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,138));shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));Controls.Add(shell);
        var rail=new Panel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(233,239,230),Padding=new Padding(10,16,10,12),Margin=Padding.Empty};shell.Controls.Add(rail,0,0);
        var mark=new LeafMark{Location=new Point(16,18),Size=new Size(42,42)};rail.Controls.Add(mark);
        var brand=TextLabel("Livly\nGardener",14,Palette.Ink,true);brand.Dock=DockStyle.None;brand.SetBounds(16,68,118,58);rail.Controls.Add(brand);
        var section=TextLabel("AUTOMATION",8,Palette.Muted,true);section.Dock=DockStyle.None;section.SetBounds(16,150,118,24);rail.Controls.Add(section);
        homeNav=new ModernButton{Text="全自動HPwr消費",Selected=true,Location=new Point(8,180),Size=new Size(122,44)};settingsNav=new ModernButton{Text="接続・設定",Location=new Point(8,232),Size=new Size(122,40)};rail.Controls.Add(homeNav);rail.Controls.Add(settingsNav);
        var railFoot=TextLabel("DESKTOP EDITION\nv0.8.1",8,Palette.Muted);railFoot.Dock=DockStyle.Bottom;railFoot.Height=48;rail.Controls.Add(railFoot);
        var content=Grid(4);content.Padding=new Padding(14,10,14,8);content.RowStyles.Add(new RowStyle(SizeType.Absolute,54));content.RowStyles.Add(new RowStyle(SizeType.Absolute,72));content.RowStyles.Add(new RowStyle(SizeType.Percent,100));content.RowStyles.Add(new RowStyle(SizeType.Absolute,24));shell.Controls.Add(content,1,0);
        var header=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Margin=Padding.Empty};header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,108));
        header.RowCount=1;header.RowStyles.Add(new RowStyle(SizeType.Percent,100));header.Controls.Add(TextLabel("島のお世話を、手軽に。",17,Palette.Ink,true),0,0);
        badge=TextLabel("●  待機中",10,Palette.Green,true);badge.TextAlign=ContentAlignment.MiddleRight;header.Controls.Add(badge,1,0);content.Controls.Add(header,0,0);
        var metrics=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=new Padding(0,4,0,8)};metrics.RowStyles.Add(new RowStyle(SizeType.Percent,100));for(int i=0;i<2;i++)metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        completedValue=Metric(metrics,0,"操作完了","0");skippedValue=Metric(metrics,1,"未処理の島","0");content.Controls.Add(metrics,0,1);
        var pages=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};content.Controls.Add(pages,0,2);normalPage=new Panel{Dock=DockStyle.Fill};settingsPage=new Panel{Dock=DockStyle.Fill,Visible=false};pages.Controls.Add(normalPage);pages.Controls.Add(settingsPage);
        var columns=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,65));columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,35));normalPage.Controls.Add(columns);
        var left=Grid(2);left.Margin=new Padding(0,0,10,0);left.RowStyles.Add(new RowStyle(SizeType.Absolute,278));left.RowStyles.Add(new RowStyle(SizeType.Percent,100));columns.Controls.Add(left,0,0);
        var task=new Card{Dock=DockStyle.Fill,Margin=new Padding(0,0,0,10),BackColor=Color.White,Padding=new Padding(12)};left.Controls.Add(task,0,0);var taskGrid=Grid(7);task.Controls.Add(taskGrid);
        foreach(int h in new[]{16,32,36,36,38,52,28})taskGrid.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
        taskGrid.Controls.Add(TextLabel("AUTO CARE",8,Palette.Green,true),0,0);taskGrid.Controls.Add(TextLabel("全自動HPwr消費",18,Palette.Ink,true),0,1);
        taskGrid.Controls.Add(TextLabel("フレンドの島で水やり・木の実回収を行います。",10,Palette.Muted),0,2);
        var delayRow=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Margin=Padding.Empty,Padding=new Padding(0,5,0,0)};
        var delayLabel=TextLabel("操作後の待機",10,Palette.Ink);delayLabel.Dock=DockStyle.None;delayLabel.Size=new Size(112,28);delayLabel.Margin=Padding.Empty;delayRow.Controls.Add(delayLabel);
        actionDelay.DropDownStyle=ComboBoxStyle.DropDownList;actionDelay.Width=106;actionDelay.AccessibleName="収穫・水やり後の待機時間";
        for(int i=0;i<=20;i++)actionDelay.Items.Add((1m+i*.2m).ToString("F1")+" 秒");actionDelay.SelectedIndex=15;
        string delayFile=System.IO.Path.Combine(data,"action-delay.txt");int savedDelay;
        if(System.IO.File.Exists(delayFile)&&int.TryParse(System.IO.File.ReadAllText(delayFile),out savedDelay)&&savedDelay>=1000&&savedDelay<=5000&&(savedDelay-1000)%200==0)actionDelay.SelectedIndex=(savedDelay-1000)/200;
        actionDelay.SelectedIndexChanged+=(s,e)=>{try{System.IO.File.WriteAllText(delayFile,(1000+actionDelay.SelectedIndex*200).ToString());}catch(Exception ex){Log("待機時間を保存できませんでした: "+ex.Message);}};
        delayRow.Controls.Add(actionDelay);taskGrid.Controls.Add(delayRow,0,3);
        activity=TextLabel("開始すると、接続と画面を自動で確認します。",9,Palette.Ink);taskGrid.Controls.Add(activity,0,4);
        var actions=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Margin=new Padding(0,5,0,7)};actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,62));actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,38));
        start.Text="▶  開始";((ModernButton)start).Primary=true;start.Dock=DockStyle.Fill;start.Margin=new Padding(0,0,10,0);start.Font=new Font("Yu Gothic UI",12,FontStyle.Bold);start.AccessibleName="全自動HPwr消費を開始";
        stop.Text="■  停止";((ModernButton)stop).Destructive=true;stop.Dock=DockStyle.Fill;stop.Margin=Padding.Empty;stop.Enabled=false;stop.AccessibleName="全自動HPwr消費を停止";actions.Controls.Add(start,0,0);actions.Controls.Add(stop,1,0);taskGrid.Controls.Add(actions,0,5);
        taskGrid.Controls.Add(TextLabel("ゲームへの反映のため、4.0秒以上を推奨。",8,Palette.Muted),0,6);
        var history=new Card{Dock=DockStyle.Fill,Margin=Padding.Empty,BackColor=Color.White,Padding=new Padding(12)};left.Controls.Add(history,0,1);var historyGrid=Grid(2);historyGrid.RowStyles.Add(new RowStyle(SizeType.Absolute,24));historyGrid.RowStyles.Add(new RowStyle(SizeType.Percent,100));history.Controls.Add(historyGrid);historyGrid.Controls.Add(TextLabel("実行ログ",11,Palette.Ink,true),0,0);
        log.Multiline=true;log.ReadOnly=true;log.BorderStyle=BorderStyle.None;log.ScrollBars=ScrollBars.Vertical;log.Dock=DockStyle.Fill;log.BackColor=Color.White;log.ForeColor=Palette.Muted;log.Font=new Font("Yu Gothic UI",9);historyGrid.Controls.Add(log,0,1);
        var screenCard=new Card{Dock=DockStyle.Fill,Margin=Padding.Empty,BackColor=Color.White,Padding=new Padding(10)};columns.Controls.Add(screenCard,1,0);var screenGrid=Grid(3);screenGrid.RowStyles.Add(new RowStyle(SizeType.Absolute,32));screenGrid.RowStyles.Add(new RowStyle(SizeType.Percent,100));screenGrid.RowStyles.Add(new RowStyle(SizeType.Absolute,40));screenCard.Controls.Add(screenGrid);screenGrid.Controls.Add(TextLabel("ゲーム画面",11,Palette.Ink,true),0,0);
        var viewport=new Panel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(242,245,239),Margin=new Padding(0,8,0,8)};screenGrid.Controls.Add(viewport,0,1);preview.Dock=DockStyle.Fill;preview.SizeMode=PictureBoxSizeMode.Zoom;preview.BackColor=viewport.BackColor;viewport.Controls.Add(preview);
        inspect.Text="接続を確認";inspect.Dock=DockStyle.Fill;inspect.Margin=Padding.Empty;screenGrid.Controls.Add(inspect,0,2);
        BuildSettings();
        var footer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,70));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,30));footer.Controls.Add(TextLabel("ローカルで動作   ·   停止ショートカット  F8",8,Palette.Muted),0,0);elapsedLabel=TextLabel("経過  00:00",8,Palette.Muted);elapsedLabel.TextAlign=ContentAlignment.MiddleRight;footer.Controls.Add(elapsedLabel,1,0);content.Controls.Add(footer,0,3);
        homeNav.Click+=(s,e)=>SwitchPage(false);settingsNav.Click+=(s,e)=>SwitchPage(true);
        uiTimer=new System.Windows.Forms.Timer{Interval=1000};uiTimer.Tick+=(s,e)=>{if(timing){var t=DateTime.Now-startedAt;elapsedLabel.Text="経過  "+((int)t.TotalMinutes).ToString("00")+":"+t.Seconds.ToString("00");}};uiTimer.Start();FormClosed+=(s,e)=>uiTimer.Dispose();
    }
    Label Metric(TableLayoutPanel grid,int column,string title,string value){var card=new Card{Dock=DockStyle.Fill,Margin=new Padding(column==0?0:7,0,column==1?0:7,0),Padding=new Padding(12,5,12,4),BackColor=Color.White};var inner=Grid(2);inner.RowStyles.Add(new RowStyle(SizeType.Absolute,23));inner.RowStyles.Add(new RowStyle(SizeType.Percent,100));inner.Controls.Add(TextLabel(title,9,Palette.Muted),0,0);var label=TextLabel(value,18,Palette.Ink,true);inner.Controls.Add(label,0,1);card.Controls.Add(inner);grid.Controls.Add(card,column,0);return label;}
    void BuildSettings(){
        var card=new Card{Dock=DockStyle.Fill,BackColor=Color.White,Padding=new Padding(12)};settingsPage.Controls.Add(card);var fields=Grid(10);card.Controls.Add(fields);
        foreach(int h in new[]{34,38,24,34,24,34,24,34,26})fields.RowStyles.Add(new RowStyle(SizeType.Absolute,h));fields.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        fields.Controls.Add(TextLabel("接続・設定",18,Palette.Ink,true),0,0);fields.Controls.Add(TextLabel("通常は初期設定のまま使えます。BlueStacksのADBを有効にしてください。",10,Palette.Muted),0,1);
        fields.Controls.Add(TextLabel("ADB実行ファイル",9,Palette.Muted,true),0,2);adb.Text=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"BlueStacks_nxt","HD-Adb.exe");adb.Dock=DockStyle.Top;fields.Controls.Add(adb,0,3);
        fields.Controls.Add(TextLabel("BlueStacksの接続先",9,Palette.Muted,true),0,4);serial.Text="127.0.0.1:5555";serial.Width=280;fields.Controls.Add(serial,0,5);
        fields.Controls.Add(TextLabel("1回の巡回上限（島）",9,Palette.Muted,true),0,6);limit.Minimum=1;limit.Maximum=500;limit.Value=100;limit.Width=130;fields.Controls.Add(limit,0,7);
        fields.Controls.Add(TextLabel("取得した画面情報",9,Palette.Muted,true),0,8);displayInfo.AutoSize=false;displayInfo.MaximumSize=Size.Empty;displayInfo.Dock=DockStyle.Fill;displayInfo.ForeColor=Palette.Muted;displayInfo.Text="まだ接続していません。ホームの「接続を確認」で取得します。";fields.Controls.Add(displayInfo,0,9);
    }
    void SwitchPage(bool settings){settingsPage.Visible=settings;normalPage.Visible=!settings;homeNav.Selected=!settings;settingsNav.Selected=settings;homeNav.Invalidate();settingsNav.Invalidate();}
    void Ui(Action action){if(IsDisposed)return;if(InvokeRequired){BeginInvoke(action);return;}action();}
    void RunAppearance(bool active,bool automate){timing=active&&automate;settingsNav.Enabled=!active;if(active){startedAt=DateTime.Now;badge.Text=automate?"●  実行中":"●  接続確認中";badge.ForeColor=Palette.Green;activity.Text=automate?"BlueStacksへの接続を確認しています。":"画面を取得しています。";if(automate){completedValue.Text=skippedValue.Text="0";}SwitchPage(false);}else{badge.Text="●  待機中";stop.Text="■  停止";}}
    void RequestStop(){if(cancel!=null){cancel.Cancel();badge.Text="●  停止中";stop.Text="停止中…";stop.Enabled=false;activity.Text="現在の処理を停止しています。";}}

}
}
