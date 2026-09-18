using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xml;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace LivlyGardener {
class DisplayGeometry {
    public readonly Size Screenshot;
    public readonly Rectangle Viewport;
    public DisplayGeometry(Size size,Rectangle viewport) {
        if(size.Width<1 || size.Height<1 || viewport.Width<1 || viewport.Height<1 || !new Rectangle(Point.Empty,size).Contains(viewport))throw new ArgumentException("Invalid display bounds");
        Screenshot=size;Viewport=viewport;
    }
    public static DisplayGeometry FromHierarchy(Size size,string xml) {
        if(!string.IsNullOrWhiteSpace(xml)) {
            var doc=new XmlDocument();doc.XmlResolver=null;
            using(var reader=XmlReader.Create(new System.IO.StringReader(xml),new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))doc.Load(reader);
            foreach(XmlElement node in doc.GetElementsByTagName("node")) {
                if(node.GetAttribute("resource-id")!="jp.cocone.livly:id/unitySurfaceView")continue;
                var n=Regex.Matches(node.GetAttribute("bounds"),@"\d+");if(n.Count!=4)continue;
                var rect=Rectangle.FromLTRB(int.Parse(n[0].Value),int.Parse(n[1].Value),int.Parse(n[2].Value),int.Parse(n[3].Value));
                if(new Rectangle(Point.Empty,size).Contains(rect) && rect.Width>0 && rect.Height>rect.Width)return new DisplayGeometry(size,rect);
            }
        }
        if(size.Height<=size.Width)throw new Exception("ゲームの縦向き描画領域が見つかりません。ゲームを開いてください。");
        return new DisplayGeometry(size,new Rectangle(Point.Empty,size));
    }
    public Point Map(Point normalized) {
        if(normalized.X<0||normalized.X>=552||normalized.Y<0||normalized.Y>=984)throw new ArgumentException("Tap outside reference frame");
        return new Point(Viewport.Left+(int)Math.Round(normalized.X*Viewport.Width/552.0),Viewport.Top+(int)Math.Round(normalized.Y*Viewport.Height/984.0));
    }
    public Bitmap Normalize(Image image) {
        if(image.Size!=Screenshot)throw new Exception("描画サイズが変わりました。座標を再取得してください。");
        var frame=new Bitmap(552,984,PixelFormat.Format24bppRgb);using(var g=Graphics.FromImage(frame))g.DrawImage(image,new Rectangle(0,0,552,984),Viewport,GraphicsUnit.Pixel);return frame;
    }
}

static class Vision {
    // Gradient magnitude removes the dependency on stroke polarity (white/black).
    public static double[] Edges(double[] src,int width,int height) {
        var result=new double[src.Length];
        for(int y=1;y<height-1;y++)for(int x=1;x<width-1;x++) {
            int i=y*width+x;double dx=src[i+1]-src[i-1],dy=src[i+width]-src[i-width];result[i]=Math.Sqrt(dx*dx+dy*dy);
        }return result;
    }
    public static Bitmap Prepare(Bitmap source,Rectangle region,int mode) {
        var b=new Bitmap(region.Width*4+48,region.Height*4+48,PixelFormat.Format24bppRgb);
        using(var g=Graphics.FromImage(b)){g.Clear(Color.White);g.DrawImage(source,new Rectangle(24,24,region.Width*4,region.Height*4),region,GraphicsUnit.Pixel);}
        if(mode==0)return b;
        var gray=new double[b.Width*b.Height];
        for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){var c=b.GetPixel(x,y);gray[y*b.Width+x]=.299*c.R+.587*c.G+.114*c.B;}
        for(int y=24;y<b.Height-24;y++)for(int x=24;x<b.Width-24;x++) {
            double v=gray[y*b.Width+x];int value;
            if(mode==1)value=(int)(255-v);
            else if(mode==2){double min=255,max=0;for(int j=-8;j<=8;j+=4)for(int i=-8;i<=8;i+=4){double t=gray[(y+j)*b.Width+x+i];min=Math.Min(min,t);max=Math.Max(max,t);}value=max-min<4?255:(int)(255*(v-min)/(max-min));}
            else value=v>225?0:255;
            value=Math.Max(0,Math.Min(255,value));b.SetPixel(x,y,Color.FromArgb(value,value,value));
        }return b;
    }
    public static string ParseAction(IEnumerable<string> texts) {
        var actions=new HashSet<string>();
        foreach(string text in texts) {
            string s=Regex.Replace(text.ToLowerInvariant(),@"\s+","");
            if(Regex.IsMatch(s,@"^[/|]?harvest[.!_ー-]*$"))actions.Add("harvest");
            if(Regex.IsMatch(s,@"^[/|]?el[i1l]x[i1l]r[.!_ー-]*$"))actions.Add("elixir");
            if(s=="また明日"||s=="また明日!"||s=="また明日！")actions.Add("done");
        }
        return actions.Count==1?actions.First():null;
    }

}

class RecognitionRecovery {
    int attempts;
    public const int RetryLimit=3;
    public void Reset(){attempts=0;}
    // Only an idle, verified visit may be skipped. A pending tap is never retried or skipped blindly.
    public string Next(bool verifiedVisit,bool pendingAction) {
        if(++attempts<=RetryLimit)return "retry";
        attempts=0;return verifiedVisit&&!pendingAction?"skip":"stop";
    }
}
}
