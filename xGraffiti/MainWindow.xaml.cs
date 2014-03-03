using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Windows.Resources;
using System.Media;
//kinect
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;
using Microsoft.Kinect.Interop;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.Controls;
using Microsoft.Kinect.Toolkit.Interaction;


namespace xGraffiti
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 概要:
        ///     该页面为只要页面,共分两大部分:Kinect控制响应部分,以及UI显示部分\
        ///     kinect控制响应部分:
        ///     分四大模块:骨骼流,深度流,手势流,音频流,分别对应骨骼映射控制鼠标,深度显示人物,手势响应控制,音频响应语音控制
        ///     UI显示部分:
        ///     创建PaintingCanvas的实例,并赋值UI层的InkCanvas统一控制画布以及其余属性
        ///     重载绘画效果,以及各种操作
        ///     
        ///     两部分通过kinect的骨骼流 手势流 音频流进行交互
        /// </summary>
        private const float ClickThreshold = 0.2f;
        private const float SkeletonMaxX = 0.50f;
        private const float SkeletonMaxY = 0.30f;
        Point oldpoint;
        Point newpoint;
        //kinect
        private KinectSensor kinectDevice;
        /// <summary>
        /// 骨骼流
        /// </summary>
        private Skeleton[] frameSkeletons;
        /// <summary>
        /// 手势流
        /// </summary>
        private InteractionStream interactionStream;
        private UserInfo[] userInfos = null;
        private KinectAdapter client;
        bool isgrip = false;//是否握手
        /// <summary>
        /// 音频流
        /// </summary>
        private SpeechRecognitionEngine _sre;
        private KinectAudioSource _source;
        /// <summary>
        /// 深度流
        /// </summary>
        private WriteableBitmap depthImageBitMap;
        private Int32Rect depthImageBitmapRect;
        private int depthImageStride;
        /// <summary>
        /// 定义画布类
        /// </summary>
        PaintingCanvas DrawCanvas;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //赋值
            DrawCanvas = new PaintingCanvas(Ink);
        }
        public MainWindow()
        {
            InitializeComponent();
            
            //kinect
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            this.KinectDevice = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);


            
        }

        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Initializing:
                case KinectStatus.Connected:
                case KinectStatus.NotPowered:
                case KinectStatus.NotReady:
                case KinectStatus.DeviceNotGenuine:
                    this.KinectDevice = e.Sensor;
                    break;
                case KinectStatus.Disconnected:
                    //TODO: Give the user feedback to plug-in a Kinect device.                    
                    this.KinectDevice = null;
                    break;
                default:
                    //TODO: Show an error state
                    break;
            }
        }

        ////将三维坐标转化到平面
        private static Point GetJointPoint(KinectSensor kinectDevice, Joint joint, Size containerSize, Point offset)
        {
            DepthImagePoint point = kinectDevice.MapSkeletonPointToDepth(joint.Position, kinectDevice.DepthStream.Format);



            //////////////////////????
            double windowWidth = 770;
            double windowHeight = 1020;

            point.X = (int)((point.X * windowWidth / kinectDevice.DepthStream.FrameWidth) - offset.X);
            point.Y = (int)((point.Y * windowHeight / kinectDevice.DepthStream.FrameHeight) - offset.Y);

            return new Point(point.X, point.Y);
        }

        /// <summary>
        /// 鼠标映射
        /// </summary>
        /// <param name="skeleton">骨骼数据</param>
        private void controlMouse(Skeleton skeleton)
        {
            int cursonx, cursony;
            // bool leftclick = false;
            Joint jointleft = skeleton.Joints[JointType.HandLeft];
            Joint jointright = skeleton.Joints[JointType.HandRight];
            Joint scaledRight = jointright.ScaleTo((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, SkeletonMaxX, SkeletonMaxY);
            Joint scaleLeft = jointleft.ScaleTo((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, SkeletonMaxX, SkeletonMaxY);
            cursonx = (int)scaledRight.Position.X;
            cursony = (int)scaledRight.Position.Y;
            newpoint = new Point(cursonx, cursony);

            if (skeleton.Joints[JointType.HandRight].TrackingState != JointTrackingState.NotTracked)
            {
                //鼠标映射传输
                SetCurson.SendMouseInput(cursonx, cursony, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, isgrip);
            }
        }
        /// <summary>
        /// welcome界面动画 渐变
        /// </summary>
        private void displayWelcome(bool isStart)
        {
            if (isStart)
            {
                if (welcome.Opacity > 0)
                {
                    welcome.Opacity -= 0.02;
                    tips.Opacity -= 0.02;

                    //
                    Thickness temp = new Thickness(tips.Margin.Left, tips.Margin.Top + 1, 0, 0);
                    tips.Margin = temp;
                   
                }
                else if (welcome.Opacity <= 0)
                {
                    welcome.Visibility = Visibility.Hidden;
                    tips.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                if (welcome.Visibility == Visibility.Hidden&&welcome.Opacity <= 0 && welcome.Opacity <= 1)
                {
                    welcome.Visibility = Visibility.Visible;
                    welcome.Opacity += 0.01;
                    tips.Opacity += 0.01;
                    tips.Visibility = Visibility.Visible;
                }
            }
        }
        /// <summary>
        /// 获取骨架 并 调用映射函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame != null)
                {
                    frame.CopySkeletonDataTo(this.frameSkeletons);
                    Skeleton skeleton = GetPrimarySkeleton(this.frameSkeletons);
                    if (skeleton != null)
                    {
                        displayWelcome(true);

                        //control mouse
                        if (skeleton.Joints[JointType.HandRight].TrackingState==JointTrackingState.Tracked)
                        {
                            controlMouse(skeleton);
                            var accelerometerReading = this.kinectDevice.AccelerometerGetCurrentReading();
                            // Hand data to Interaction framework to be processed
                            this.interactionStream.ProcessSkeleton(this.frameSkeletons, accelerometerReading, frame.Timestamp);
                        }
                    }
                    else
                    {
                        //人离开场景锁住屏幕
                        displayWelcome(false);

                    }
                }
            }
        }

        /// <summary>
        /// 手势的处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void OnInteractionFrameReady(object sender, InteractionFrameReadyEventArgs e)
        {
            using (InteractionFrame frame = e.OpenInteractionFrame())
            {

                if (frame != null)
                {
                    if (this.userInfos == null)
                    {
                        this.userInfos = new UserInfo[InteractionFrame.UserInfoArrayLength];
                    }

                    frame.CopyInteractionDataTo(this.userInfos);

                }
                else
                {
                    return;
                }
            }
            /////////////
            foreach (UserInfo userInfo in this.userInfos)
            {
                foreach (InteractionHandPointer handPointer in userInfo.HandPointers)
                {
                   // string action = null;

                    switch (handPointer.HandEventType)
                    {
                        case InteractionHandEventType.Grip:
                            //action = "gripped";
                            //isDrawing.Content = "isDrawing: True";
                            isgrip = true;
                            break;

                        case InteractionHandEventType.GripRelease:
                           // action = "released";
                            isgrip = false;
                            break;
                    }

                    //if (action != null)
                    //{
                    //    string handSide = "unknown";

                    //    switch (handPointer.HandType)
                    //    {
                    //        case InteractionHandType.Left:
                    //            handSide = "left";
                    //            break;

                    //        case InteractionHandType.Right:
                    //            handSide = "right";
                    //            break;
                    //    }
                    // Console.WriteLine("User " + userInfo.SkeletonTrackingId + " " + action + " their " + handSide + "hand.");
                    //}
                }
            }
        }



        /// <summary>
        /// 深度图
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            if (this.kinectDevice != sender)
            {
                return;
            }

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (null != depthFrame)
                {
                    ////深度方块(显示)
                    short[] depthPixelDate = new short[depthFrame.PixelDataLength];
                    depthFrame.CopyPixelDataTo(depthPixelDate);
                    depthImageBitMap.WritePixels(depthImageBitmapRect, depthPixelDate, depthImageStride, 0);
                    // PlayerDepthImage(depthFrame, depthPixelDate);
                    // Hand data to Interaction framework to be processed
                    this.interactionStream.ProcessDepth(depthFrame.GetRawPixelData(), depthFrame.Timestamp);

                }
            }
        }
        /// <summary>
        /// 对深度图处理
        /// </summary>
        /// <param name="depthFrame"></param>
        /// <param name="pixelData"></param>
        private void PlayerDepthImage(DepthImageFrame depthFrame, short[] pixelData)
        {
            int playerIndex;
            int depthBytePerPixel = 4;
            byte[] enhPixelData = new byte[depthFrame.Width * depthFrame.Height * depthBytePerPixel];

            for (int i = 0, j = 0; i < pixelData.Length; i++, j += depthBytePerPixel)
            {
                playerIndex = pixelData[i] & DepthImageFrame.PlayerIndexBitmask;
            }


            this.depthImageBitMap.WritePixels(depthImageBitmapRect, pixelData, depthImageStride, 0);
        }

        /// <summary>
        /// 返回最前的骨架
        /// </summary>
        /// <param name="skeletons"></param>
        /// <returns></returns>
        private static Skeleton GetPrimarySkeleton(Skeleton[] skeletons)
        {
            Skeleton skeleton = null;

            if (skeletons != null)
            {
                //Find the closest skeleton       
                for (int i = 0; i < skeletons.Length; i++)
                {
                    if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                    {
                        if (skeleton == null)
                        {
                            skeleton = skeletons[i];
                        }
                        else
                        {
                            //只取位于kinect前特定距离的骨架信息
                            if (skeleton.Position.Z > 1.5 )
                            {
                                if (skeleton.Position.Z > skeletons[i].Position.Z)
                                {
                                    skeleton = skeletons[i];
                                }
                            }
                        }
                    }
                }
            }

            return skeleton;
        }
        /// <summary>
        /// 语音调用函数
        /// </summary>
        /// 
        private KinectAudioSource CreateAudioSource()
        {
            var source = KinectSensor.KinectSensors[0].AudioSource;
            source.AutomaticGainControlEnabled = false;
            source.EchoCancellationMode = EchoCancellationMode.None;
            return source;
        }

        private void StartSpeechRecognition()
        {
            // KinectAudioSource source = kinectDevice.AudioSource;
            // //关闭回声抑制模式
            // source.EchoCancellationMode = EchoCancellationMode.None;
            // source.AutomaticGainControlEnabled = false;

            //RecognizerInfo ri = 
            _source = CreateAudioSource();
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase)
                    && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };
            RecognizerInfo ri = SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
            _sre = new SpeechRecognitionEngine(ri.Id);
            CreateGrammars(ri);
            _sre.SpeechRecognized += sre_SpeechRecognized;
            _sre.SpeechHypothesized += sre_SpeechHypothesized;
            _sre.SpeechRecognitionRejected += sre_SpeechRecognitionRejected;

            Stream s = _source.Start();
            _sre.SetInputToAudioStream(s,
                                        new SpeechAudioFormatInfo(
                                            EncodingFormat.Pcm, 16000, 16, 1,
                                            32000, 2, null));
            _sre.RecognizeAsync(RecognizeMode.Multiple);

        }

        void sre_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            // HypothesizedText += " Rejected";
            //  Confidence = Math.Round(e.Result.Confidence, 2).ToString();
        }

        void sre_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            //  HypothesizedText = e.Result.Text;
        }
        void sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action<SpeechRecognizedEventArgs>(InterpretCommand), e);
        }
        /// <summary>
        /// 建立语音命令
        /// </summary>
        /// <param name="ri"></param>
        private void CreateGrammars(RecognizerInfo ri)
        {
            //color
            var colors = new Choices();
            colors.Add("yellow");
            colors.Add("white");
            colors.Add("blue");
            colors.Add("green");
            colors.Add("red");
            colors.Add("black");
            var gb = new GrammarBuilder();
            gb.Culture = ri.Culture;
            gb.Append(colors);
            var g = new Grammar(gb);
            _sre.LoadGrammar(g);

            //instruction
            var instructions = new Choices();
            instructions.Add("save");
            instructions.Add("reset");
            instructions.Add("undo");
            instructions.Add("load");
            instructions.Add("stop");
            instructions.Add("paint");
            instructions.Add("close");
            instructions.Add("help");
            instructions.Add("stroke decrease");
            instructions.Add("stroke increase");
            instructions.Add("stroke reset");
            instructions.Add("quit application");
            var instructElement = new GrammarBuilder { Culture = ri.Culture };
            instructElement.Append(instructions);
            var instruct = new Grammar(instructElement);

            _sre.LoadGrammar(instruct);
        }
        /// <summary>
        /// 对于语音命令的判断与处理
        /// </summary>
        /// <param name="e"></param>
        private void InterpretCommand(SpeechRecognizedEventArgs e)
        {
            var result = e.Result;
            //指令判断
            //Confidence = Math.Round(result.Confidence, 2).ToString();
            //if (result.Confidence < 95 && result.Words[0].Text == "quit" && result.Words[1].Text == "application")
            //{
            //    this.Close();
            //}
            if (result.Confidence > 0.7 && result.Words[0].Text == "quit" && result.Words[1].Text == "application")
            {
                this.Close();
            }
            else if (result.Confidence >= 0.5)
            {
                var instructionString = result.Words[0].Text;
                string path; //建立图片颜色索引地址
                // Color color;
                switch (instructionString)
                {
                    //颜色
                    case "yellow":
                        DrawCanvas.colorName = instructionString;
                        path = "..\\..\\resource\\Color_" + instructionString + ".png";
                        colorForDisplay.Source = new BitmapImage(new Uri(path, UriKind.Relative));
                        break;
                    case "white":
                        DrawCanvas.colorName = instructionString;
                        path = "..\\..\\resource\\Color_" + instructionString + ".png";
                        colorForDisplay.Source = new BitmapImage(new Uri(path, UriKind.Relative));
                        break;
                    case "blue":
                        DrawCanvas.colorName = instructionString;
                        path = "..\\..\\resource\\Color_" + instructionString + ".png";
                        colorForDisplay.Source = new BitmapImage(new Uri(path, UriKind.Relative));
                        break;
                    case "green":
                        DrawCanvas.colorName = instructionString;
                        path = "..\\..\\resource\\Color_" + instructionString + ".png";
                        colorForDisplay.Source = new BitmapImage(new Uri(path, UriKind.Relative));
                        break;
                    case "red":
                        DrawCanvas.colorName = instructionString;
                        path = "..\\..\\resource\\Color_" + instructionString + ".png";
                        colorForDisplay.Source = new BitmapImage(new Uri(path, UriKind.Relative));
                        break;
                    case "black":
                        DrawCanvas.colorName = instructionString;
                        path = "..\\..\\resource\\Color_" + instructionString + ".png";
                        colorForDisplay.Source = new BitmapImage(new Uri(path, UriKind.Relative));
                        break;

                        //指令
                    case "save":
                        Instruction.Content = instructionString;
                        DrawCanvas.save();
                        break;
                    case "reset":
                        Instruction.Content = instructionString;
                        DrawCanvas.reset();
                        theNumberOfStrokIndex = 0;
                        break;
                    case "undo":
                        if (!IsDrawing)
                        {
                            Instruction.Content = instructionString;
                            DrawCanvas.undo();
                            if(DrawCanvas.strokeList.Count>=1)
                            {
                                theNumberOfStrokIndex = DrawCanvas.strokeList[DrawCanvas.strokeList.Count - 1];
                            }
                            else if (DrawCanvas.strokeList.Count == 0)
                            {
                                theNumberOfStrokIndex = 0;
                            }
                        }
                        break;
                    case "load":
                        Instruction.Content = instructionString;
                        DrawCanvas.load();
                        break;
                    case "stop":
                        Instruction.Content = instructionString;
                        IsStopping = true;
                        //DrawCanvas.eraser();
                        break;
                    case "paint":
                        Instruction.Content = instructionString;
                        IsStopping = false;
                        //DrawCanvas.paint();
                        break;
                    case "help":
                        Instruction.Content = instructionString;
                        colorTable.Visibility = Visibility.Visible;
                        break;
                    case "close":
                        if (colorTable.Visibility == Visibility.Visible)
                        {
                            Instruction.Content = result.Words[0].Text;
                            colorTable.Visibility = Visibility.Hidden;
                        }
                        break;
                    case "stroke":
                        string next = result.Words[1].Text;
                        switch (next)
                        {
                            case "decrease":
                                Instruction.Content = instructionString + " " + next;
                                if (this.DrawCanvas.penSize > 45)
                                {
                                    this.DrawCanvas.penSize -= 40;
                                }
                                break;
                            case "increase":
                                Instruction.Content = instructionString + " " + next;
                                if (this.DrawCanvas.penSize < 300)
                                {
                                    this.DrawCanvas.penSize += 40;
                                }
                                break;
                            case "reset":

                                Instruction.Content = result.Words[0].Text + " " + result.Words[1].Text;
                                this.DrawCanvas.penSize = 100;
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        return;
                }
            }
        }


        public KinectSensor KinectDevice
        {
            get { return this.kinectDevice; }
            set
            {
                if (this.kinectDevice != value)
                {
                    //Uninitialize
                    if (this.kinectDevice != null)
                    {
                        this.kinectDevice.DepthFrameReady -= this.DepthFrameReady;
                        this.kinectDevice.SkeletonFrameReady -= this.SkeletonFrameReady;

                        this.frameSkeletons = null;
                        this.userInfos = null;

                        this.interactionStream.InteractionFrameReady -= this.OnInteractionFrameReady;
                        this.interactionStream.Dispose();
                        this.interactionStream = null;
                        this._sre.RecognizeAsyncCancel();
                        this._sre.RecognizeAsyncStop();
                        this._sre.Dispose();

                    }

                    this.kinectDevice = value;

                    //Initialize
                    if (this.kinectDevice != null)
                    {
                        if (this.kinectDevice.Status == KinectStatus.Connected)
                        {
                            // this.kinectDevice.SkeletonStream.Enable();

                            this.kinectDevice.SkeletonStream.Enable(new TransformSmoothParameters()
                            {
                                Correction = 0.5f,
                                JitterRadius = 0.1f,
                                MaxDeviationRadius = 0.5f,
                                Smoothing = 0.7f
                            });

                            //this.kinectDevice.SkeletonStream.Enable(new TransformSmoothParameters()
                            //{
                            //    Smoothing = 0.5f,
                            //    Correction = 0.1f,
                            //    Prediction = 0.5f,
                            //    JitterRadius = 0.1f,
                            //    MaxDeviationRadius = 0.1f
                            //}); 

                            DepthImageStream depthStream = this.kinectDevice.DepthStream;

                            depthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

                            depthImageBitMap = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight, 96, 96, PixelFormats.Gray16, null);
                            depthImageBitmapRect = new Int32Rect(0, 0, depthStream.FrameWidth, depthStream.FrameHeight);
                            depthImageStride = depthStream.FrameWidth * depthStream.FrameBytesPerPixel;
                            DepthImage.Source = depthImageBitMap;
                            //this.interactionStream.Dispose();

                            // Allocate space to put the skeleton and interaction data we'll receive
                            //this.frameSkeletons = new Skeleton[this.kinectDevice.SkeletonStream.FrameSkeletonArrayLength];

                            this.userInfos = new UserInfo[InteractionFrame.UserInfoArrayLength];

                            this.frameSkeletons = new Skeleton[this.kinectDevice.SkeletonStream.FrameSkeletonArrayLength];

                            client = new KinectAdapter();
                            this.interactionStream = new InteractionStream(this.kinectDevice, this.client);


                            this.KinectDevice.SkeletonFrameReady += this.SkeletonFrameReady;
                            this.kinectDevice.DepthFrameReady += this.DepthFrameReady;
                            this.interactionStream.InteractionFrameReady += this.OnInteractionFrameReady;
                            //interactionStream.InteractionFrameReady += new EventHandler<InteractionFrameReadyEventArgs>(OnInteractionFrameReady);//

                            //SkeletonViewerElement.KinectDevice = this.kinectDevice;

                            this.kinectDevice.Start();


                            //语音调用函数!
                            StartSpeechRecognition();

                        }
                    }
                }
            }
        }
        /// <summary>
        /// 以下三个函数分别对inkCanvas的操作作自定义
        /// </summary>
        /// 

        public int theNumberOfStrokIndex = 0;

        Point previousPoint, currentPoint;
        bool IsDrawing = false;
        bool IsStopping = false;
        private void inkcanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DrawCanvas.myCanvas.EditingMode == InkCanvasEditingMode.None)
            {
                if (!IsStopping)
                {
                    IsDrawing = true;
                }
               
                previousPoint = e.GetPosition((IInputElement)sender);

                StreamResourceInfo sri = Application.GetResourceStream(new Uri("resource/selected_pointer.cur", UriKind.Relative));
                Cursor customCursor = new Cursor(sri.Stream);
                DrawCanvas.myCanvas.Cursor = customCursor;
            }
        }

        Stroke st = null;
        double imageWidth = 15;//喷漆密度
        double distance = 0.0;
        private void inkcanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsDrawing)
            {
                currentPoint = e.GetPosition((IInputElement)sender);

                distance = Math.Sqrt(Math.Pow(currentPoint.X - previousPoint.X, 2) + Math.Pow(currentPoint.Y - previousPoint.Y, 2));

                while (distance >= imageWidth)
                {
                    double x = imageWidth * (Math.Sin(Math.Atan(Math.Abs(currentPoint.X - previousPoint.X) / Math.Abs(currentPoint.Y - previousPoint.Y)))) * (currentPoint.X > previousPoint.X ? 1.0 : -1.0) + previousPoint.X;
                    double y = imageWidth * (Math.Cos(Math.Atan(Math.Abs(currentPoint.X - previousPoint.X) / Math.Abs(currentPoint.Y - previousPoint.Y)))) * (currentPoint.Y > previousPoint.Y ? 1.0 : -1.0) + previousPoint.Y;
                    distance -= imageWidth;
                    previousPoint = new Point(x, y);

                    StylusPointCollection pts = new StylusPointCollection();
                    pts.Add(new StylusPoint(x, y));
                    st = new customStroke(pts,DrawCanvas);
                    DrawCanvas.myCanvas.Strokes.Add(st);

                    theNumberOfStrokIndex++;
                    
                }
            }
        }

        private void inkcanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (DrawCanvas.myCanvas.EditingMode == InkCanvasEditingMode.None)
            {
                if (st != null)
                {
                    DrawCanvas.myCanvas.Strokes.Remove(st);
                    DrawCanvas.myCanvas.Strokes.Add(st.Clone());
                }
                IsDrawing = false;
                StreamResourceInfo sri = Application.GetResourceStream(new Uri("resource/unselected_pointer.cur", UriKind.Relative));
                Cursor customCursor = new Cursor(sri.Stream);
                DrawCanvas.myCanvas.Cursor = customCursor;
            }
            //笔触记录

            DrawCanvas.strokeList.Add(theNumberOfStrokIndex);
        }

        private void inkcanvas_StrokeErased(object sender,RoutedEventArgs e)
        {
            //if(theNumberOfStrokIndex==0)
            //{
            //    //
            //}
            ////笔触数大于0
            //else if(theNumberOfStrokIndex>0)
            //{
            //    theNumberOfStrokIndex--;
            //    //若笔触数为0时 clear strkelist
            //    if(theNumberOfStrokIndex==0)
            //    {
            //        DrawCanvas.strokeList.Clear();
            //    }
            //    else
            //    {
            //        //该笔画的笔触数相应 -1
            //        DrawCanvas.strokeList[DrawCanvas.strokeList.Count - 1]--;
            //        //如果笔画数大于1  则需要判断前笔画的最后笔触是否恰好为后笔画的起始笔触
            //        if (DrawCanvas.strokeList.Count > 1)
            //        {
            //            if (DrawCanvas.strokeList[DrawCanvas.strokeList.Count - 1] == DrawCanvas.strokeList[DrawCanvas.strokeList.Count - 2])
            //            {
            //                DrawCanvas.strokeList.RemoveAt(DrawCanvas.strokeList.Count - 1);
            //            }
            //        }
            //    }
            //}
        }
    }
 
}

