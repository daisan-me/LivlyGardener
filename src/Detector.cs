using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
namespace LivlyGardener {
class Match { public string Name; public double Score; public Point Point; public bool Enabled=true; }
class Detector {
    class Template { public string Name; public double[] Values,Edge; public int W,H; public double Norm,EdgeNorm; public Rectangle Area; public List<Point> Gold=new List<Point>(); public double GoldMean; }
    List<Template> templates = new List<Template>();
    public Detector(string path) {
        Add(path,"shortage",new Rectangle(236,83,216,50));
        Add(path,"shortage-small",new Rectangle(236,83,216,50));
        Add(path,"shortage-rendered",new Rectangle(236,83,216,50));
        Add(path,"return",new Rectangle(28,906,82,68));
        Add(path,"return-icon",new Rectangle(28,880,83,52));
        Add(path,"harvest",new Rectangle(278,245,110,65));
        Add(path,"elixir",new Rectangle(278,245,110,65));
        Add(path,"done",new Rectangle(278,245,110,65));
        Add(path,"done-shape",new Rectangle(280,250,100,49));
        Add(path,"fruit-shape",new Rectangle(248,290,62,60));
        Add(path,"water-shape",new Rectangle(248,290,62,60));
        Add(path,"fruit-icon",new Rectangle(245,280,70,75));
        Add(path,"home",new Rectangle(275,920,100,64));
        Add(path,"home-clean",new Rectangle(278,873,94,109));
        Add(path,"menu",new Rectangle(275,725,145,65));
        Add(path,"friend",new Rectangle(230,915,100,60));
        Add(path,"friend2",new Rectangle(230,915,100,60));
        Add(path,"friend3",new Rectangle(230,915,100,60));
        Add(path,"friend-icon",new Rectangle(240,880,77,51));
        Add(path,"friend4",new Rectangle(230,915,100,60));
        Add(path,"success",new Rectangle(190,130,175,60));
        Add(path,"ok",new Rectangle(220,750,120,65));
    }
    static double[] Pixels(Bitmap b) {
        var a=new double[b.Width*b.Height];
        var r=b.LockBits(new Rectangle(0,0,b.Width,b.Height),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
        try { byte[] data=new byte[Math.Abs(r.Stride)*b.Height]; Marshal.Copy(r.Scan0,data,0,data.Length);
            for(int y=0;y<b.Height;y++) for(int x=0;x<b.Width;x++) {
                int i=y*r.Stride+x*3; a[y*b.Width+x]=.114*data[i]+.587*data[i+1]+.299*data[i+2];
            }
        } finally { b.UnlockBits(r); } return a;
    }
    static double[] WhiteStrokes(Bitmap b) {
        var values=new double[b.Width*b.Height];
        for(int y=245;y<350;y++)for(int x=248;x<388;x++){var c=b.GetPixel(x,y);values[y*b.Width+x]=255*Math.Max(0,Math.Min(1,(Math.Min(c.R,Math.Min(c.G,c.B))-215)/30.0));}
        return values;
    }
    void Add(string path,string name,Rectangle area) {
        using(var b=new Bitmap(Path.Combine(path,name+".png"))) {
            var v=Pixels(b);var edge=Vision.Edges(v,b.Width,b.Height);double em=edge.Average();for(int i=0;i<edge.Length;i++)edge[i]-=em;
            var t=new Template{Name=name,Values=v,W=b.Width,H=b.Height,Area=area,Edge=edge,EdgeNorm=Math.Sqrt(edge.Sum(x=>x*x))};
            if(name.StartsWith("friend") || name.StartsWith("return"))for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){var c=b.GetPixel(x,y);if(c.R>140&&c.R-c.B>45&&c.G-c.B>20){t.Gold.Add(new Point(x,y));t.GoldMean+=v[y*b.Width+x];}}
            if(t.Gold.Count>0)t.GoldMean/=t.Gold.Count;
            double mean=v.Average(); for(int i=0;i<v.Length;i++)v[i]-=mean;t.Norm=Math.Sqrt(v.Sum(x=>x*x));templates.Add(t);
        }
    }
    public static Bitmap Normalize(Image b) {
        return DisplayGeometry.FromHierarchy(b.Size,null).Normalize(b);
    }
    public Dictionary<string,Match> Read(Bitmap frame,string only=null) {
        var p=Pixels(frame);var edges=only==null?Vision.Edges(p,frame.Width,frame.Height):null; var result=new Dictionary<string,Match>();var white=only==null?WhiteStrokes(frame):null;
        foreach(var t in templates) {
            if(only!=null && t.Name!=only && !(only=="shortage" && t.Name.StartsWith("shortage-")))continue;
            var pixels=t.Name.EndsWith("-shape")?white:p;
            bool useEdges=t.Name=="harvest"||t.Name=="elixir"||t.Name=="done"||t.Name=="fruit-icon";
            var best=new Match{Name=t.Name,Score=-1}; int count=t.W*t.H;
            for(int y=t.Area.Top;y<=t.Area.Bottom-t.H;y++) for(int x=t.Area.Left;x<=t.Area.Right-t.W;x++) {
                double sum=0,sq=0,dot=0,esum=0,esq=0,edot=0; int k=0;
                for(int j=0;j<t.H;j++) for(int i=0;i<t.W;i++) {
                    double v=pixels[(y+j)*552+x+i]; sum+=v; sq+=v*v; dot+=v*t.Values[k];
                    if(useEdges){double e=(j==0||i==0||j==t.H-1||i==t.W-1)?0:edges[(y+j)*552+x+i];esum+=e;esq+=e*e;edot+=e*t.Edge[k];}k++;
                }
                double norm=Math.Sqrt(Math.Max(0,sq-sum*sum/count))*t.Norm;
                double score=norm>0?dot/norm:0;
                if(useEdges) {
                    double en=Math.Sqrt(Math.Max(0,esq-esum*esum/count))*t.EdgeNorm;
                    if(en>0)score=Math.Max(score,.97*edot/en);
                }
                if(score>best.Score) {best.Score=score;best.Point=new Point(x+t.W/2,y+t.H/2);}
            }
            if(t.Gold.Count>0){double actual=t.Gold.Average(q=>p[(best.Point.Y-t.H/2+q.Y)*552+best.Point.X-t.W/2+q.X]);best.Enabled=actual>=t.GoldMean*.86;}
            result[t.Name]=best;
        }
        if(result.ContainsKey("shortage-small") && result["shortage-small"].Score>result["shortage"].Score)result["shortage"]=result["shortage-small"];
        if(result.ContainsKey("shortage-rendered") && result["shortage-rendered"].Score>result["shortage"].Score)result["shortage"]=result["shortage-rendered"];
        if(only!=null)return result;
        if(result["return-icon"].Enabled && (!result["return"].Enabled || result["return-icon"].Score>result["return"].Score))result["return"]=result["return-icon"];
        if(result["home-clean"].Score>result["home"].Score)result["home"]=result["home-clean"];
        result["friend"]=new[]{result["friend"],result["friend2"],result["friend3"],result["friend4"],result["friend-icon"]}.OrderByDescending(m=>m.Enabled?m.Score:0).First();
        return result;
    }
    public static string ShapeAction(Dictionary<string,Match> m) {
        double fruit=m["fruit-shape"].Score,water=m["water-shape"].Score;
        if(fruit>=.68 && fruit-water>=.15)return "harvest";
        if(water>=.68 && water-fruit>=.15)return "elixir";
        return null;
    }
    public static Point ActionPoint(Dictionary<string,Match> m,string action) {
        if(ShapeAction(m)==action)return m[action=="harvest"?"fruit-shape":"water-shape"].Point;
        if(action=="harvest" && m["harvest"].Score<.88)return m["fruit-icon"].Point;
        return new Point(276,316);
    }
    public bool HasShortage(Bitmap frame){return Read(frame,"shortage")["shortage"].Score>=.90;}
    public static string State(Dictionary<string,Match> m) {
        if(m.ContainsKey("shortage") && m["shortage"].Score>=.90)return "HPwr不足";
        if(m["success"].Score>=.86 && m["ok"].Score>=.86)return "収穫成功";
        if(m["menu"].Score>=.88)return "移動メニュー";
        if(m["friend"].Score>=.85 && m["friend"].Enabled) {
            var action=new[]{m["harvest"],m["elixir"],m["done"]}.OrderByDescending(x=>x.Score).ToArray();
            if(action[0].Score>=.88 && action[0].Score-action[1].Score>=.06)return action[0].Name;
            if(m["done-shape"].Score>=.65)return "done";
            string icon=ShapeAction(m);
            if(icon!=null)return icon;
            if(m["fruit-icon"].Score>=.93 && m["done"].Score<.8)return "harvest";
            return "訪問先・操作なし";
        }
        if(m["home"].Score>=.88)return "ホーム";
        return "不明";
    }
}

}
