using System;
using System.IO;
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
using System.Windows.Resources;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace xGraffiti
{
    public class PaintingCanvas
    {
        /// <summary>
        /// 画布
        /// </summary>
        private InkCanvas _myCanvas;
        public InkCanvas myCanvas
        {
            get { return this._myCanvas; }
            set { this._myCanvas = value; }

        }
        /// <summary>
        /// 画布背景色
        /// </summary>
        private Brush backgroundColor;
        public Brush _backgroundColor
        {
            get { return this.backgroundColor; }
            set
            {
                this.backgroundColor = value;
                this._myCanvas.Background = this.backgroundColor;
            }
        }
        /// <summary>
        /// 画笔颜色
        /// </summary>
        private string _colorName;
        public string colorName
        {
            get { return this._colorName; }
            set
            {
                this._colorName = value;
                
            }
        }
        /// <summary>
        /// 笔触大小
        /// </summary>
        private int _penSize;
        public int penSize
        {
            get { return this._penSize; }
            set
            {
                this._penSize = value;

            }
        }
        public int indexForStrokeArray;
        public int[] strokeArray;
        public List<int> strokeList;


        private DrawingAttributes canvasAttributes;
        private int currentBackgroundNum = 1;
        private int TotalNumberOfBackground = 3;

        private MainWindow _windowUI;
        public PaintingCanvas()
        {
            //默认构造函数;
        }


        public PaintingCanvas(MainWindow window, InkCanvas _canvas)
        {
            _windowUI = window;


            this._myCanvas = _canvas;
            //背景色
            //this.backgroundColor = Brushes.Gray;
            //this._myCanvas.Background = this.backgroundColor;


            //改变笔触以及颜色(重载
            canvasAttributes = new DrawingAttributes();
            canvasAttributes.Width = 10;
            canvasAttributes.Height = 10;
            canvasAttributes.StylusTip = StylusTip.Ellipse;
            canvasAttributes.FitToCurve = true;
            canvasAttributes.Color = Color.FromArgb(255, 255, 255, 255);
            this._myCanvas.DefaultDrawingAttributes = canvasAttributes;


            this._myCanvas.UseCustomCursor = true;
            //Cursor对象不直接支持URI资源语法，为应用程序添加光标文件作为资源，然后将该资源作为可以使用于Cursor对象的数据流返回，通过使用Application.GetResourceStream()方式；
            StreamResourceInfo sri = Application.GetResourceStream(new Uri(@"pack://application:,,,/Resources/PointerImage/unselected_pointer.cur", UriKind.RelativeOrAbsolute));
            Cursor customCursor = new Cursor(sri.Stream);
            this._myCanvas.Cursor = customCursor;
            //颜色初值
            this._colorName = "black";
            //笔触初值
            this._penSize = 100;
            //笔触记录初值 
            this.strokeArray=new int[6];//笔触记录最多六次
            this.indexForStrokeArray = 0;

            this.strokeList = new List<int>();

            //Stroke st = null;
            //double imageWidth = 0.1;
            //double distance = 0.0;
            //double x = imageWidth;
            //double y = imageWidth ;
            //distance -= imageWidth;
            ////previousPoint = new Point(x, y);

            //StylusPointCollection pts = new StylusPointCollection();
            //pts.Add(new StylusPoint(x, y));
            //st = new customStroke(pts);
            //this.myCanvas.Strokes.Add(st);
        }
        /// <summary>
        /// 保存文件序列号
        /// </summary>
        private int indexBmp = 0;
        public void save()
        {
            //保存
            string imageFile = "save"+indexBmp+".bmp";
            indexBmp++;
            double width = this._myCanvas.ActualWidth;
            double height = this._myCanvas.ActualHeight;
            RenderTargetBitmap bmpCopied = new RenderTargetBitmap((int)Math.Round(width), (int)Math.Round(height), 96, 96, PixelFormats.Default);
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(this._myCanvas);
                dc.DrawRectangle(vb, null, new Rect(new System.Windows.Point(), new System.Windows.Size(width, height)));
            }
            bmpCopied.Render(dv);
            using (FileStream file = new FileStream(imageFile, FileMode.Create, FileAccess.Write))
            {
                BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmpCopied));
                encoder.Save(file);
            }

            //保存为.isf格式
            string imageFileISF = "save" + indexBmp + ".isf";
            using (FileStream file = new FileStream(imageFileISF, FileMode.Create, FileAccess.Write))
            {
                this._myCanvas.Strokes.Save(file);
                file.Close();
            }
        }
        /// <summary>
        /// 非必要功能
        /// </summary>
        public void load()
        {
            string imageFile = "test" + indexBmp + ".isf";
            // FileStream file = new FileStream(imageFile, FileMode.Create, FileAccess.Read);
            // this._myCanvas.Strokes = new StrokeCollection(file);

            using (FileStream file = new FileStream(imageFile, FileMode.Open, FileAccess.Read))
            {
                this._myCanvas.Strokes = new StrokeCollection(file);
                file.Close();
            }
        }
        public void reset()
        {
            //重置画布
            this._myCanvas.Strokes.Clear();
            strokeList.Clear();
        }
        public void undo()
        {
            //撤销
            if (this._myCanvas.Strokes.Count > 0 && this.strokeList.Count>0)
            {

                int index = this.strokeList.Count;
                int last = this.strokeList[index - 1] ;
                int first=0;
                if (index > 1)
                {
                     first = this.strokeList[index - 2];
                }
                else if (index == 1)
                {
                    first = 0;
                }
                if (first == last)
                {
                    this._myCanvas.Strokes.RemoveAt(last);
                }
                else
                {
                    for (int i = last - 1; i >= first; i--)
                    {
                        if (this._myCanvas.Strokes.Count > 0)
                        {
                            this._myCanvas.Strokes.RemoveAt(i);
                        }
                    }
                }
                this.strokeList.RemoveAt(index - 1);
            }

        }

        public void ChangeToNextBackground(bool isNext)
        {
            if(isNext)
            {
                currentBackgroundNum=(currentBackgroundNum+1)%TotalNumberOfBackground;
            }
            else
            {
                currentBackgroundNum=(currentBackgroundNum-1+TotalNumberOfBackground)%TotalNumberOfBackground;
            }
            Uri uri = new Uri("pack://application:,,,/Resources/BGImage/" + currentBackgroundNum + ".png",UriKind.RelativeOrAbsolute );
            _windowUI.background.Source=new BitmapImage(uri);
        }

        //public void eraser()
        //{
        //    //橡皮
        //    //this._myCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
        //    //this._myCanvas.EraserShape= StylusTip.Ellipse;
        //    //this._myCanvas.EraserShape.Width = 100;
        //}
        //public void paint()
        //{
        //    this._myCanvas.EditingMode = InkCanvasEditingMode.None;
        //}


    }
}

