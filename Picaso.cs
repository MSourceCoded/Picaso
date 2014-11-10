using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Net;
using System.Xml.Linq;

namespace sourcecoded.Picaso {
    class Picaso {

        public static int WIDTH = 3840;
        public static int HEIGHT = 2160;

        public static StreamWriter fileImgurLog;
        public static ArrayList imgurLinks = new ArrayList();

        public static bool imgur = false;
        public static bool doTitle = true;

        public static int fontSize = 160;           //160 default

        public static string output = "narrow";

        static void Main(string[] args) {
            Random rnd = new Random();

            fileImgurLog = new StreamWriter("imgur.log");

            if (!Directory.Exists(output))
                Directory.CreateDirectory(output);

            for (int it = 0; it < 100; it++) {
                Bitmap image = new Bitmap(WIDTH, HEIGHT, PixelFormat.Format32bppArgb);

                string[] dict = File.ReadAllLines(@"dict.txt");

                string title = it.ToString("000") + " -- " + dict[rnd.Next(dict.Length)] + " " + dict[rnd.Next(dict.Length)];

                using (Graphics graphics = Graphics.FromImage(image)) {
                    byte r = (byte)rnd.Next(255);
                    byte g = (byte)rnd.Next(255);
                    byte b = (byte)rnd.Next(255);

                    Rectangle bg = new Rectangle(0, 0, WIDTH, HEIGHT);

                    int d = 80;

                    Brush brush = new LinearGradientBrush(bg, Color.FromArgb(r, g, b), Color.FromArgb((byte)clampRandomColourOffset(rnd, r, d), (byte)clampRandomColourOffset(rnd, g, d), (byte)clampRandomColourOffset(rnd, b, d)), 90, true);
                    graphics.FillRectangle(brush, bg);

                    do {
                        GraphicsPath path = new GraphicsPath();

                        Point[] points = new Point[rnd.Next(3) + 3];
                        for (int i = 0; i < points.Length; i++)
                            points[i] = new Point(rnd.Next(WIDTH), rnd.Next(HEIGHT));

                        path.AddLines(points);

                        r = (byte)rnd.Next(255);
                        g = (byte)rnd.Next(255);
                        b = (byte)rnd.Next(255);
                        byte a = (byte)rnd.Next(200);

                        Color color = Color.FromArgb(a, Color.FromArgb(r, g, b));

                        r = (byte)rnd.Next(255);
                        g = (byte)rnd.Next(255);
                        b = (byte)rnd.Next(255);
                        a = (byte)rnd.Next(200);

                        Color color2 = Color.FromArgb(a, Color.FromArgb(r, g, b));

                        LinearGradientBrush brush2 = new LinearGradientBrush(bg, color, color2, rnd.Next(360), true);

                        graphics.FillPath(brush2, path);
                        path.Dispose();
                        brush2.Dispose();
                    } while (rnd.Next(5) != 0);

                    brush.Dispose();
                    if (doTitle)
                        renderTitle(graphics, title);
                    graphics.Dispose();
                }

                Console.WriteLine(title);

                try {
                    image.Save(String.Format(output + "/{0}.png", title));
                    if (imgur)
                        uploadToImgur(image, title);
                } catch (Exception e) {
                    it--;
                    Console.WriteLine(e);
                    continue;
                }
            }

            if (imgur)
                imgurAlbum();

            Console.ReadLine();
        }

        static int clampRandomColourOffset(Random rnd, int original, int d) {
            return Math.Min(Math.Max(original + (rnd.Next(d * 2) - d), 0), 255);
        }

        static void renderTitle(Graphics g, string title) {
            Font font = new Font("Ostrich Sans", fontSize, FontStyle.Bold);

            Point point = new Point((int)(WIDTH - (fontSize + 30)), (int)(HEIGHT - (fontSize + 40)));

            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Far;

            Color color = Color.White;
            Color newColor = Color.FromArgb(100, color);

            Pen p = new Pen(Color.FromArgb(160, ColorTranslator.FromHtml("#000000")), 2);
            p.LineJoin = LineJoin.Round;

            GraphicsPath gp = new GraphicsPath();
            gp.AddString(title, font.FontFamily, (int)font.Style, font.Size, point, format);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            g.DrawPath(p, gp);
            g.FillPath(new SolidBrush(newColor), gp);

            p.Dispose();
            gp.Dispose();
        }

        static void uploadToImgur(Image image, string title) {
            string apiKey = "Client-ID bcbfecd69a6eca7";

            MemoryStream stream = new MemoryStream();
            image.Save(stream, ImageFormat.Png);

            string data = Convert.ToBase64String(stream.ToArray());

            using (var w = new WebClient()) {
                var values = new NameValueCollection {
                    {"image", data},
                    {"title", title}
                };

                w.Headers.Add("Authorization", apiKey);
                byte[] response = w.UploadValues("https://api.imgur.com/3/upload.xml", values);

                XDocument doc = XDocument.Load(new MemoryStream(response));
                XElement element = doc.Element(XName.Get("data"));
                XElement id = element.Element(XName.Get("id"));
                imgurLinks.Add(id.Value);
            }
        }

        static void imgurAlbum() {
            string apiKey = "Client-ID bcbfecd69a6eca7";

            string ids = "";

            foreach (string s in imgurLinks) {
                if (ids.Length > 0)
                    ids += ",";
                ids += s;
            }

            using (var w = new WebClient()) {
                var values = new NameValueCollection {
                    {"ids", ids}
                };

                w.Headers.Add("Authorization", apiKey);
                byte[] response = w.UploadValues("https://api.imgur.com/3/album.xml", values);

                XDocument doc = XDocument.Load(new MemoryStream(response));
                XElement element = doc.Element(XName.Get("data"));
                XElement id = element.Element(XName.Get("id"));

                Console.WriteLine("Album ID: " + id);
                Console.WriteLine("Album URL: " + "http://imgur.com/a/" + id);
            }
        }
    }
}