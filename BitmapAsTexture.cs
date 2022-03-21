using System;
using System.Collections.Generic;
using System.Drawing;

namespace MyBitmapedTextures
{
    class Pixel
    {
        public int Position {get; set;}
        public double Intensity {get; set;}

        public Pixel(double intensity, int position)
        {
            Position = position;
            Intensity = intensity;
        }
    }

    class BitmapAsTexture
    {
        private FastBitmap map;
        private Algorithm algo;

        public BitmapAsTexture(Bitmap bmp)
        {
            map = new FastBitmap(bmp);
            map.LockBitmap();

            algo = new Algorithm();
        }

        public Bitmap Get1DTexture(Point start, Point end)
        {
            int width = algo.GetDistance(start, end);

            IList<Point> points = algo.GenerateLinePoints(start, end);

            FastBitmap oneD = new FastBitmap(width, 1);
            oneD.LockBitmap();

            int max = points.Count < width ? points.Count : width;
            int lastPixel = 0;

            for (int i = 0; i < max; i++)
            {
                Point pt = points[i];
                if (max == width)
                {
                    oneD.SetPixel(i, 0, map.GetPixel(pt.X, pt.Y));
                }
                else
                {
                    int x1D = Convert.ToInt32((float)i / points.Count * width);
                    PixelData pixel = map.GetPixel(pt.X, pt.Y);
                    oneD.SetPixel(x1D, 0, pixel);

                    for (int j = x1D - 1; j > lastPixel; j--)
                    {
                        oneD.SetPixel(j, 0, pixel);
                    }

                    lastPixel = x1D;
                }
            }

            oneD.UnlockBitmap();
            return oneD.Bitmap;
        }

        public Bitmap ShowEdgePoints(Bitmap texture1D, int maxEdges, ColorSubtracter subtype, int tolerance, Color color)
        {
            ICustomCollection collection = null;

            if (subtype is LuminoSubtractor)
            {
                collection = new IntensityStack(maxEdges);
            }
            else
            {
                collection = new BoundedSortedCollection(maxEdges);
            }

            return SetEdgePoints(texture1D, collection, subtype, tolerance, color); ;
        }

        private Bitmap SetEdgePoints(Bitmap tex, ICustomCollection collection, ColorSubtracter subtype, int tolerance, Color color)
        {
            Bitmap blankTex1D = new Bitmap(tex.Width, tex.Height);

            Graphics g = Graphics.FromImage(blankTex1D);
            g.Clear(Color.FromArgb(255 - color.R, 255 - color.G, 255 - color.B));
            g.Dispose();

            FastBitmap basetex = new FastBitmap(tex);
            FastBitmap edgetex = new FastBitmap(blankTex1D);

            basetex.LockBitmap();
            edgetex.LockBitmap();

            double lastDiff = 0.0;

            for (int i = 0; i < tex.Width - 1; i++)
            {
                Color c1 = basetex.GetPixel(i, 0).ToColor();
                Color c2 = basetex.GetPixel(i + 1, 0).ToColor();

                double diff = subtype.GetColorDiff(c1, c2);
                double absDiff = Math.Abs(diff);
                double stepDiff = Math.Abs(absDiff - lastDiff);

                if (absDiff > tolerance && stepDiff > tolerance)
                {
                    collection.Add(new Pixel(diff, i));
                }

                lastDiff = absDiff;
            }

            collection.Block();

            for (int i = 0; i < collection.Count; i++)
            {
                int pos = collection.GetItem(i).Position;
                edgetex.SetPixel(pos, 0, new PixelData(color));
            }

            edgetex.UnlockBitmap();
            basetex.UnlockBitmap();

            return edgetex.Bitmap;
        }

        public void Dispose()
        {
            map.UnlockBitmap();
        }
    }
}
