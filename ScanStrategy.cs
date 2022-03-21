using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MyBitmapedTextures
{
    struct PointPair
    {
        public Point Start;
        public Point End;

        public PointPair(Point start, Point end)
        {
            Start = start;
            End = end;
        }
    }

    public interface ScanStrategy
    {
        double Scan(int width, int height, int maxedges, int tolerance);
        ColorSubtracter Strategy { get; set; }
    }

    class HScan : ScanStrategy
    {
        private BitmapAsTexture bmptex;
        private Graphics graphics;
        private Color edgecolor;
        private object syncObj = new object();

        public ColorSubtracter Strategy { get; set; }

        public HScan(BitmapAsTexture b, Graphics g, Color e, ColorSubtracter c)
        {
            lock (syncObj)
            {
                bmptex = b;
                graphics = g;
                edgecolor = e;
                Strategy = c;
            }
        }

        public double Scan(int width, int height, int maxedges, int tolerance)
        {
            lock (syncObj)
            {
                DateTime start = DateTime.Now;

                foreach (PointPair pointpair in GetPoints(width, height))
                {
                    Bitmap tex1d = null, onlyedges = null;

                    try
                    {
                        tex1d = bmptex.Get1DTexture(pointpair.Start, pointpair.End);
                        onlyedges = bmptex.ShowEdgePoints(tex1d, maxedges, Strategy, tolerance, edgecolor);
                        graphics.DrawImage(onlyedges, pointpair.Start);
                    }
                    finally
                    {
                        if (tex1d != null)
                        {
                            tex1d.Dispose();
                        }
                        if (onlyedges != null)
                        {
                            onlyedges.Dispose();
                        }
                    }
                }

                return (DateTime.Now - start).TotalMilliseconds;
            }
        }

        private IList<PointPair> GetPoints(int width, int height)
        {
            IList<PointPair> result = new List<PointPair>();

            for (int i = 0; i < height; i++)
            {
                result.Add(new PointPair(new Point(0, i), new Point(width, i)));
            }

            return result;
        }
    }

    class VScan : ScanStrategy
    {
        private BitmapAsTexture bmptex;
        private Graphics graphics;
        private Color edgecolor;
        private object syncObj = new object();

        public ColorSubtracter Strategy { get; set; }

        public VScan(BitmapAsTexture b, Graphics g, Color e, ColorSubtracter c)
        {
            lock (syncObj)
            {
                bmptex = b;
                graphics = g;
                edgecolor = e;
                Strategy = c;
            }
        }

        public double Scan(int width, int height, int maxedges, int tolerance)
        {
            lock (syncObj)
            {
                DateTime start = DateTime.Now;

                foreach (PointPair pointpair in GetPoints(width, height))
                {
                    Bitmap tex1d = null, onlyedges = null;

                    try
                    {
                        tex1d = bmptex.Get1DTexture(pointpair.Start, pointpair.End);
                        onlyedges = bmptex.ShowEdgePoints(tex1d, maxedges, Strategy, tolerance, edgecolor);
                        onlyedges.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        graphics.DrawImage(onlyedges, pointpair.Start);
                    }
                    finally
                    {
                        if (tex1d != null)
                        {
                            tex1d.Dispose();
                        }
                        if (onlyedges != null)
                        {
                            onlyedges.Dispose();
                        }
                    }
                }

                return (DateTime.Now - start).TotalMilliseconds;
            }
        }

        private IList<PointPair> GetPoints(int width, int height)
        {
            IList<PointPair> result = new List<PointPair>();

            for (int i = 0; i < width; i++)
            {
                result.Add(new PointPair(new Point(i, 0), new Point(i, height)));
            }

            return result;
        }
    }

    class InclinedScan : ScanStrategy
    {
        private BitmapAsTexture bmptex;
        private Graphics graphics;
        private Color edgecolor;
        private int angle;
        private object syncObj = new object();
        private const double DEGREE_TO_RADIAN = 0.017453292519943295769236907684883;

        public ColorSubtracter Strategy { get; set; }

        public InclinedScan(int a, BitmapAsTexture b, Graphics g, Color e, ColorSubtracter c)
        {
            lock (syncObj)
            {
                angle = a;
                bmptex = b;
                graphics = g;
                edgecolor = e;
                Strategy = c;
            }
        }

        public double Scan(int width, int height, int maxedges, int tolerance)
        {
            lock (syncObj)
            {
                //graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                DateTime start = DateTime.Now;

                foreach (PointPair pointpair in GetPoints(width, height))
                {
                    Bitmap tex1d = null, onlyedges = null;

                    try
                    {
                        tex1d = bmptex.Get1DTexture(pointpair.Start, pointpair.End);
                        onlyedges = bmptex.ShowEdgePoints(tex1d, maxedges, Strategy, tolerance, edgecolor);

                        ObjectSpaceRotateTransform(pointpair, angle); // rotate modelmatrix (obj-trans-mat * world-trans-mat)
                        graphics.DrawImage(onlyedges, pointpair.Start);
                        ObjectSpaceRotateTransform(pointpair, -angle); // -rotate viewmatrix (reset world transform matrix)
                    }
                    finally
                    {
                        if (tex1d != null)
                        {
                            tex1d.Dispose();
                        }
                        if (onlyedges != null)
                        {
                            onlyedges.Dispose();
                        }
                    }
                }

                return (DateTime.Now - start).TotalMilliseconds;
            }
        }

        private void ObjectSpaceRotateTransform(PointPair axisPoint, int angle)
        {
            graphics.TranslateTransform(axisPoint.Start.X, axisPoint.Start.Y);
            graphics.RotateTransform(angle);
            graphics.TranslateTransform(-axisPoint.Start.X, -axisPoint.Start.Y);
        }

        private IList<PointPair> GetPoints(int width, int height)
        {
            IList<PointPair> result = new List<PointPair>();

            double angle1 = Math.Tan((90 - angle) * DEGREE_TO_RADIAN);
            double angle2 = Math.Tan(angle * DEGREE_TO_RADIAN);

            int start = (int)(angle1 * height);
            int end = width - start;

            for (int i = 0; i < end; i++)
            {
                result.Add(new PointPair(new Point(i, 0), new Point(i + start, height)));
            }

            for (int i = 1; (i < height); i++)
            {
                result.Add(new PointPair(new Point(0, i), new Point((int)(angle1 * (height - i)), height)));
            }

            for (int i = end; i < width; i++)
            {
                result.Add(new PointPair(new Point(i, 0), new Point(width, (int)(angle2 * (width - i)))));
            }

            return result;
        }
    }
}
