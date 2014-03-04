using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Ink;
using System.Reflection;

namespace xGraffiti
{
    public class customStroke : Stroke
    {
        private string color;
        private int size;
        public customStroke(StylusPointCollection pts,PaintingCanvas ink)
            : base(pts)
        {
            this.StylusPoints = pts;
            this.color = ink.colorName;
            this.size = ink.penSize;
        }

        protected override void DrawCore(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            if (drawingContext == null)
            {
                throw new ArgumentNullException("drawingContext");
            }
            if (null == drawingAttributes)
            {
                throw new ArgumentNullException("drawingAttributes");
            }

            DrawingAttributes originalDa = drawingAttributes.Clone();
            originalDa.Width = this.size;
            originalDa.Height = this.size;
            //ImageBrush brush = new ImageBrush(new BitmapImage(new Uri(@"test3.png", UriKind.Relative)));

            string path = "pack://application:,,,/Resources/ColorBrush/BColor_" + color + ".png";
            ImageBrush brush = new ImageBrush(new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute)));
            brush.Freeze();
            drawingContext.DrawGeometry(brush, null, this.GetGeometry(originalDa));

        }
    }
}
