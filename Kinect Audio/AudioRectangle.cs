using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Globalization;

namespace Kinect_audio
{
    class AudioRectangle
    {
        public enum handType : byte { Left, Right };
        private Rect myRect;
        private MediaElement sound;
        private string rectName;
        private bool isOn = false;
        private bool firstTime = true;
        private bool containsRightHand = false;
        private bool containsLeftHand = false;
        private readonly Pen myPen = new Pen(Brushes.Teal, 3);
        private List<handType> stickyHands;
        private static List<handType> takenHands;
        private static List<AudioRectangle> RectList = new List<AudioRectangle>();
        public static DrawingGroup drawingGroup = new DrawingGroup();
        private static DrawingContext drawingContext = AudioRectangle.drawingGroup.Open();
        private static Point leftHandPosition;
        private static Point rightHandPosition;
        private bool isPlaying = false;
        public static bool toogleChangePosition = false;


        public AudioRectangle(Rect rect, MediaElement snd, String name)
        {
            this.myRect = rect;
            this.stickyHands = new List<handType>();
            this.sound = snd;
            this.sound.MediaEnded += SoundOver;
            this.rectName = name;

            AudioRectangle.RectList.Add(this);
        }

        public static void init()
        {
            AudioRectangle.takenHands = new List<handType>();
        }

        public static void addRectangle(Rect rect, MediaElement snd, String name)
        {
            new AudioRectangle(rect, snd, name);
        }

        public void turnOn()
        {
            bool wasOn = this.isOn;
            this.isOn = true;
            if (!wasOn)
                this.drawMyRect();
            this.drawMyRect();
        }

        public void turnOff()
        {
            bool wasOn = this.isOn;
            this.isOn = false;
            if (wasOn || firstTime)
            {
                firstTime = false;
                this.drawMyRect();
            }
        }

        public void drawMyRect()
        {

            Brush myBrush = null;

            if (this.isOn)
            {
                myBrush = Brushes.Gold;
                if (isPlaying == false)
                {
                    isPlaying = true;
                    this.sound.Play();
                }

            }
            else
            {
                if (isPlaying == true)
                {
                    isPlaying = false;
                    this.sound.Stop();
                }

                myBrush = Brushes.Azure;
            }

            AudioRectangle.drawingContext.DrawRectangle(myBrush, myPen, this.myRect);
            AudioRectangle.drawingContext.DrawText(new FormattedText(
                   this.rectName,
                   CultureInfo.GetCultureInfo("en-us"),
                   FlowDirection.LeftToRight,
                   new Typeface("Verdana"),
                   32,
                   Brushes.Black)
                   , new Point(this.myRect.TopLeft.X + 40 , this.myRect.TopLeft.Y + 25)
                   );
        }

        public bool Contains(Point point)
        {
            return this.myRect.Contains(point);
        }

        public void setPosition(Point point)
        {
            point.Offset(-this.myRect.Width / 2, -this.myRect.Height / 2);
            this.myRect.Location = point;
        }            

        public static void setDrawingContext(DrawingContext dc)
        {
            AudioRectangle.drawingContext = dc;
        }

        public static void setLeftHandPosition(Point point)
        {
            AudioRectangle.leftHandPosition = point;
            AudioRectangle.handleHandPosition(point, handType.Left);
        }

        public static void setRightHandPosition(Point point)
        {
            AudioRectangle.rightHandPosition = point;
            AudioRectangle.handleHandPosition(point, handType.Right);
        }

        private static void handleHandPosition(Point point, handType type)
        {
            bool inRect = false;

            foreach (AudioRectangle rect in AudioRectangle.RectList)
            {
                if (AudioRectangle.toogleChangePosition)
                {
                    if (rect.Contains(point))
                    {
                        inRect = true;
                        if (!AudioRectangle.takenHands.Contains(type) && rect.stickyHands.Count == 0)
                        {
                            AudioRectangle.takenHands.Add(type);
                            rect.stickyHands.Add(type);
                        }
                    }
                    else
                    {
                        rect.stickyHands.Remove(type);
                    }

                    if (rect.stickyHands.Contains(type))
                    {
                        rect.setPosition(point);
                    }

                    continue;
                }
                else
                {
                    rect.stickyHands.Clear();
                    AudioRectangle.takenHands.Clear();
                }


                if (rect.Contains(point))
                {
                    if (type == handType.Left)
                        rect.containsLeftHand = true;
                    if (type == handType.Right)
                        rect.containsRightHand = true;
                }
                else
                {
                    if (type == handType.Left)
                        rect.containsLeftHand = false;
                    if (type == handType.Right)
                        rect.containsRightHand = false;
                }

                if (rect.containsLeftHand || rect.containsRightHand)
                    rect.turnOn();
                else
                    rect.turnOff();
            }

            if (!inRect)
            {
                AudioRectangle.takenHands.Remove(type);
            }
        }

        public static void drawRectangles()
        {
            foreach (AudioRectangle rect in RectList)
            {
                rect.drawMyRect();
            }
        }

        void SoundOver(object sender, RoutedEventArgs e)
        {
            this.sound.Position = TimeSpan.Zero;
            this.sound.Play();
        }

    }
}