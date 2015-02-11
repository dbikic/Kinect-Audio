using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.IO;

namespace Kinect_audio
{
    public partial class MainWindow : Window
    {
        private const float canvasWidth = 640.0f;
        private const float canvasHeight = 480.0f;
        private const float rectangleWidth = 100;
        private const float rectangleHeight = 100;
        private List<Rect> rectangleArray = new List<Rect>();
        private const double ClipBoundsThickness = 10;
        private const double BodyCenterThickness = 10;
        private const double JointThickness = 6;
        private DrawingGroup drawingGroup;
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);
        private KinectSensor sensor;

        public MainWindow()
        {
            InitializeComponent();
            AudioRectangle.init();
        }


        private void start(object sender, RoutedEventArgs e)
        {
            
            AudioRectangle.addRectangle(new Rect(90, 100, rectangleWidth, rectangleHeight), one, "1");
            AudioRectangle.addRectangle(new Rect(200, 50, rectangleWidth, rectangleHeight), two, "2");
            AudioRectangle.addRectangle(new Rect(350, 50, rectangleWidth, rectangleHeight), three, "3");
            AudioRectangle.addRectangle(new Rect(460, 100, rectangleWidth, rectangleHeight), four, "4");
            AudioRectangle.addRectangle(new Rect(90, 230, rectangleWidth, rectangleHeight), five, "5");
            AudioRectangle.addRectangle(new Rect(200, 330, rectangleWidth, rectangleHeight), six, "6");
            AudioRectangle.addRectangle(new Rect(350, 330, rectangleWidth, rectangleHeight), seven, "7");
            AudioRectangle.addRectangle(new Rect(460, 230, rectangleWidth, rectangleHeight), eight, "8");

            this.backgroundMusic1.Volume = 0.3;
            this.backgroundMusic2.Volume = 0.3;

            this.drawingGroup = new DrawingGroup();
            slika.Source = new DrawingImage(this.drawingGroup);

            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                this.sensor.SkeletonStream.Enable();
                this.sensor.SkeletonFrameReady += this.SkeletonReady;

                try
                {
                    this.sensor.Start();
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            /*
            myPicture.Source = new BitmapImage(
                                     new Uri(
                                         "pack://application:,,,/Kinect_audio;component/gore.png"));
            */
        }


        public void SkeletonReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            // array skeletona
            Skeleton[] skeletons = new Skeleton[0];
            // using varijabla skeletonFrame, scope unutar vitičastih
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];

                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                AudioRectangle.setDrawingContext(dc);
               // dc.DrawRectangle(Brushes.Gold, null, new Rect(0.0, 0.0, canvasWidth, canvasHeight));


                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        // RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            Brushes.CadetBlue,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }

                        this.handleRectangles(skel);
                        AudioRectangle.drawRectangles();
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, canvasWidth, canvasHeight));
            }
        }
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, canvasHeight - ClipBoundsThickness, canvasWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, canvasWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, canvasHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(canvasWidth - ClipBoundsThickness, 0, ClipBoundsThickness, canvasHeight));
            }
        }

        private void handleRectangles(Skeleton skeleton)
        {

            if (skeleton.Joints[JointType.HandLeft].TrackingState == JointTrackingState.Tracked)
                AudioRectangle.setLeftHandPosition(this.SkeletonPointToScreen(skeleton.Joints[JointType.HandLeft].Position));
            if (skeleton.Joints[JointType.HandRight].TrackingState == JointTrackingState.Tracked)
                AudioRectangle.setRightHandPosition(this.SkeletonPointToScreen(skeleton.Joints[JointType.HandRight].Position));
        }

        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

        }

        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
             ColorImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToColorPoint(skelpoint, ColorImageFormat.RgbResolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

           if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }
           
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        private void stopAllBackgroundMusic()
        {
            this.backgroundMusic1.Position = TimeSpan.Zero;
            this.backgroundMusic1.Stop();
            this.backgroundMusic2.Position = TimeSpan.Zero;
            this.backgroundMusic2.Stop();
            this.backgroundMusic3.Position = TimeSpan.Zero;
            this.backgroundMusic3.Stop();
        }

        private void Global_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                AudioRectangle.toogleChangePosition = !AudioRectangle.toogleChangePosition;

                if (AudioRectangle.toogleChangePosition)
                    this.testLabel.Content = "Za povratak na sviranje stisni SPACE";
                else
                    this.testLabel.Content = "Pritiskom na SPACE ulaziš u mode za promjenu mjesta kvadrata";
            }
            if (e.Key == Key.J)
            {
                if (this.backgroundMusic1.Position == TimeSpan.Zero){
                    stopAllBackgroundMusic();
                    this.backgroundMusic1.Play();
                }
                else
                {
                    this.backgroundMusic1.Stop();
                }                    
                
            }
            if (e.Key == Key.K)
            {
                if (this.backgroundMusic2.Position == TimeSpan.Zero)
                {
                    stopAllBackgroundMusic();
                    this.backgroundMusic2.Play();
                }
                else
                {
                    this.backgroundMusic2.Stop();
                }   
            }
            if (e.Key == Key.L)
            {
                if (this.backgroundMusic3.Position == TimeSpan.Zero)
                {
                    stopAllBackgroundMusic();
                    this.backgroundMusic3.Play();
                }
                else
                {
                    this.backgroundMusic3.Stop();                    
                } 
            }
        }

    }

}