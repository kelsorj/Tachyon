using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Emgu.CV;
//using Emgu.CV.GPU;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;

namespace TwoCameraCapture
{
    public partial class Form1 : Form
    {
        private Capture _capture0;
        private Capture _capture1;
        private bool _captureInProgress0;
        private bool _captureInProgress1;

        public Form1()
        {
            if (!IsPlaformCompatable()) return;
            InitializeComponent();
        }

        private void ProcessFrame(object sender, EventArgs e)
        {
            Image<Bgr, Byte> frame0 = _capture0.QueryFrame();
            Rectangle roi = new Rectangle(Convert.ToInt32(leftright1.Value),
                Convert.ToInt32(updown1.Value),
                Convert.ToInt32(width1.Value),
                Convert.ToInt32(height1.Value)); // set the roi
            frame0.ROI = roi;
            Image<Bgr, Byte> frame0cr = frame0.Rotate(Convert.ToInt32(rotate1.Value), new Bgr(1,1,1), false);
            // turn image into gray
            Image<Gray, Byte> grayFrame0 = frame0.Convert<Gray, Byte>();
            Image<Gray, Byte> frame0r = grayFrame0.Rotate(Convert.ToInt32(rotate1.Value), new Gray(100), false);
            Image<Gray, Byte> smallGrayFrame0 = grayFrame0.PyrDown();
            Image<Gray, Byte> smoothedGrayFrame0 = smallGrayFrame0.PyrUp();
            Image<Gray, Byte> cannyFrame0 = smoothedGrayFrame0.Canny(new Gray(100), new Gray(60));
            imageBox1.Image = frame0;
            imageBox3.Image = grayFrame0;
        }

        private void TubeDetect(object sender, EventArgs e)
        {
            SURFDetector surfParam = new SURFDetector(400, false);
            Image<Gray, Byte> modelImage = new Image<Gray, byte>("matrixtube.png");
            ImageFeature[] modelFeatures = surfParam.DetectFeatures(modelImage, null);
            //Create a Feature Tracker
            Features2DTracker tracker = new Features2DTracker(modelFeatures);

            //Now we need to go and grab an image from the camera to see if the tube is present

            Image<Bgr, Byte> frame0 = _capture0.QueryFrame();
            Rectangle roi = new Rectangle(Convert.ToInt32(leftright1.Value),
                            Convert.ToInt32(updown1.Value),
                            Convert.ToInt32(width1.Value),
                            Convert.ToInt32(height1.Value)); // set the roi
            frame0.ROI = roi;

            Image<Gray, Byte> gray = frame0.Convert<Gray, Byte>().PyrDown().PyrUp();
            Gray cannyThreshold = new Gray(180);
            Gray cannyThresholdLinking = new Gray(120);
            Gray circleAccumulatorThreshold = new Gray(500);
            Image<Gray, Byte> cannyEdges = gray.Canny(cannyThreshold, cannyThresholdLinking);
            LineSegment2D[] lines = cannyEdges.HoughLinesBinary(
                1, //Distance resolution in pixel-related units
                Math.PI / 45.0, //Angle resolution measured in radians.
                Convert.ToInt32(thresholdBox.Value), //threshold
                Convert.ToInt32(minwidthBox.Value), //min Line width
                Convert.ToInt32(gapBox.Value) //gap between lines
                )[0]; //Get the lines from the first channel
            #region draw lines
            Image<Bgr, Byte> lineImage = frame0.CopyBlank();
            foreach (LineSegment2D line in lines)
                lineImage.Draw(line, new Bgr(Color.Green), 1);
            #endregion
            /*
            //Image<Gray, Byte> observedImage = frame0.Convert<Gray, Byte>();
            // This is practice using a snapshot image
            //Image<Gray, Byte> observedImage = new Image<Gray, byte>("live_tube.png");
            // Extract features from the observed image
            Stopwatch watch = Stopwatch.StartNew();
            ImageFeature[] imageFeatures = surfParam.DetectFeatures(observedImage, null);
            Features2DTracker.MatchedImageFeature[] matchedFeatures = tracker.MatchFeature(imageFeatures, 2, 20);
            matchedFeatures = Features2DTracker.VoteForUniqueness(matchedFeatures, 0.9);
            matchedFeatures = Features2DTracker.VoteForSizeAndOrientation(matchedFeatures, 2.5, 20);
            HomographyMatrix homography = Features2DTracker.GetHomographyMatrixFromMatchedFeatures(matchedFeatures);
            watch.Stop();
            //Merge the object image and the observed image into one image for display
            Image<Gray, Byte> res = modelImage.ConcateVertical(observedImage);

            #region draw lines between the matched features
            foreach (Features2DTracker.MatchedImageFeature matchedFeature in matchedFeatures)
            {
                PointF p = matchedFeature.ObservedFeature.KeyPoint.Point;
                p.Y += modelImage.Height;
                res.Draw(new LineSegment2DF(matchedFeature.SimilarFeatures[0].Feature.KeyPoint.Point, p), new Gray(0), 1);
            }
            #endregion

            #region draw the project region on the image
            if (homography != null)
            {  
                //draw a rectangle along the projected model
                Rectangle rect = modelImage.ROI;
                PointF[] pts = new PointF[] 
                                { 
                                new PointF(rect.Left, rect.Bottom),
                                new PointF(rect.Right, rect.Bottom),
                                new PointF(rect.Right, rect.Top),
                                new PointF(rect.Left, rect.Top)
                                };
                homography.ProjectPoints(pts);

                for (int i = 0; i < pts.Length; i++)
                    pts[i].Y += modelImage.Height;

                res.DrawPolyline(Array.ConvertAll<PointF, Point>(pts, Point.Round), true, new Gray(255.0), 1);
            }
            #endregion
            */

            imageBox1.Image = lineImage;
            imageBox5.Image = gray;
            imageBox6.Image = cannyEdges;
            //label6.Text = Convert.ToString(watch.ElapsedMilliseconds);
            imageBox3.Image = frame0;

            //ImageViewer.Show(res, String.Format("Matched in {0} milliseconds", watch.ElapsedMilliseconds));
        }

        private void ProcessFrame1(object sender, EventArgs e)
        {
            Image<Bgr, Byte> frame1 = _capture1.QueryFrame();
            Image<Gray, Byte> grayFrame1 = frame1.Convert<Gray, Byte>();
            Image<Gray, Byte> smallGrayFrame1 = grayFrame1.PyrDown();
            Image<Gray, Byte> smoothedGrayFrame1 = smallGrayFrame1.PyrUp();
            Image<Gray, Byte> cannyFrame1 = smoothedGrayFrame1.Canny(new Gray(100), new Gray(60));
            imageBox2.Image = frame1;
            imageBox4.Image = cannyFrame1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_capture0 == null)
            {
                try
                {
                    _capture0 = new Capture(0);
                }
                catch (NullReferenceException excpt)
                {
                    MessageBox.Show("Error on Camera 0: " + excpt.Message);
                }
            }
            if (_capture0 != null)
            {
                if (_captureInProgress0)
                {
                    button1.Text = "Start Capture 1";
                    Application.Idle -= ProcessFrame;
                }
                else
                {
                    button1.Text = "Stop";
                    Application.Idle += ProcessFrame;
                }
                _captureInProgress0 = !_captureInProgress0;             
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (_capture1 == null)
            {
                try
                {
                    _capture1 = new Capture(1);
                }
                catch (NullReferenceException excpt)
                {
                    MessageBox.Show("Error on Camera 1: " + excpt.Message);
                }
            }
            if (_capture1 != null)
            {
                if (_captureInProgress1)
                {  //stop the capture
                    button2.Text = "Start Capture 2";
                    Application.Idle -= ProcessFrame1;
                }
                else
                {
                    //start the capture
                    button2.Text = "Stop";
                    Application.Idle += ProcessFrame1;
                }
                _captureInProgress1 = !_captureInProgress1;               
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //Now we need to go and grab an image from the camera to see if the tube is present
            Image<Bgr, Byte> frame0 = _capture0.QueryFrame();
            Rectangle roi = new Rectangle(Convert.ToInt32(leftright1.Value),
                            Convert.ToInt32(updown1.Value),
                            Convert.ToInt32(width1.Value),
                            Convert.ToInt32(height1.Value)); // set the roi
            frame0.ROI = roi;
            // Change the color image into gray scale for the analysis
            Image<Gray, Byte> gray = frame0.Convert<Gray, Byte>().PyrDown().PyrUp();
            Gray cannyThreshold = new Gray(180);
            Gray cannyThresholdLinking = new Gray(120);
            Gray circleAccumulatorThreshold = new Gray(500);
            Image<Gray, Byte> cannyEdges = gray.Canny(cannyThreshold, cannyThresholdLinking);
            LineSegment2D[] lines = cannyEdges.HoughLinesBinary(
                1, //Distance resolution in pixel-related units
                Math.PI / 45.0, //Angle resolution measured in radians.
                Convert.ToInt32(thresholdBox.Value), //threshold
                Convert.ToInt32(minwidthBox.Value), //min Line width
                Convert.ToInt32(gapBox.Value) //gap between lines
                )[0]; //Get the lines from the first channel

            #region Find a tube
            List<Triangle2DF> triangleList = new List<Triangle2DF>();
            List<MCvBox2D> boxList = new List<MCvBox2D>(); //a box is a rotated rectangle
            using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
                for (
                    Contour<Point> contours = cannyEdges.FindContours(
                        Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                        Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST,
                        storage);
                contours != null;
                contours = contours.HNext)
                {
                    Contour<Point> currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);
                    if (currentContour.Area > Convert.ToInt32(contourareaBox.Value)) //only consider contours with area greater than 250
                    {
                        if (currentContour.Total == 4) //The contour has 4 vertices.
                        {
                            #region determine if all the angles in the contour are within [80, 100] degree
                            bool isRectangle = true;
                            Point[] pts = currentContour.ToArray();
                            LineSegment2D[] edges = PointCollection.PolyLine(pts, true);
                            /*
                            for (int i = 0; i < edges.Length; i++)
                            {
                                double angle = Math.Abs(
                                   edges[(i + 1) % edges.Length].GetExteriorAngleDegree(edges[i]));
                                if (angle < 80 || angle > 100)
                                {
                                    isRectangle = false;
                                    break;
                                }
                            }
                            */
                            #endregion
                            if (isRectangle) boxList.Add(currentContour.GetMinAreaRect());
                        }
                    }
                }
            #region draw triangles and rectangles
            Image<Bgr, Byte> triangleRectangleImage = frame0.CopyBlank();
            foreach (Triangle2DF triangle in triangleList)
                triangleRectangleImage.Draw(triangle, new Bgr(Color.DarkBlue), 2);
            foreach (MCvBox2D box in boxList)
                triangleRectangleImage.Draw(box, new Bgr(Color.DarkOrange), 2);
            //triangleRectangleImageBox.Image = triangleRectangleImage;
            #endregion

            //Draw the lines
            Image<Bgr, Byte> lineImage = frame0.CopyBlank();
            int tubei = 0;
            label6.Text = "There is no tube present";
            foreach (LineSegment2D line in lines)
            {
                tubei++;
                lineImage.Draw(line, new Bgr(Color.Green), 1);
                if (tubei > 3)
                    label6.Text = "There is a tube present";
            }
            #endregion

            #region Use Fast Library for Approximate Nearest Neighbors (FLANN) to find tubes
            // This is an alternate feature detection algorithm
            SURFDetector surfParam = new SURFDetector(400, false);
            // This is the model image to use to try and hunt for the image
            Image<Gray, Byte> modelImage = new Image<Gray, byte>("matrixtube.png");
            ImageFeature[] modelFeatures = surfParam.DetectFeatures(modelImage, null);
            //Create a Feature Tracker
            Features2DTracker tracker = new Features2DTracker(modelFeatures);
            Image<Gray, Byte> observedImage = gray;
            // This is practice using a snapshot image
            //Image<Gray, Byte> observedImage = new Image<Gray, byte>("live_tube.png");
            // Extract features from the observed image
            Stopwatch watch = Stopwatch.StartNew();
            ImageFeature[] imageFeatures = surfParam.DetectFeatures(observedImage, null);
            Features2DTracker.MatchedImageFeature[] matchedFeatures = tracker.MatchFeature(imageFeatures, 2, 20);
            matchedFeatures = Features2DTracker.VoteForUniqueness(matchedFeatures, 0.9);
            matchedFeatures = Features2DTracker.VoteForSizeAndOrientation(matchedFeatures, 2.5, 20);
            HomographyMatrix homography = Features2DTracker.GetHomographyMatrixFromMatchedFeatures(matchedFeatures);
            //Merge the object image and the observed image into one image for display
            Image<Gray, Byte> res = modelImage.ConcateVertical(observedImage);
            

            #region draw lines between the matched features
            foreach (Features2DTracker.MatchedImageFeature matchedFeature in matchedFeatures)
            {
                PointF p = matchedFeature.ObservedFeature.KeyPoint.Point;
                p.Y += modelImage.Height;
                res.Draw(new LineSegment2DF(matchedFeature.SimilarFeatures[0].Feature.KeyPoint.Point, p), new Gray(0), 1);
            }
            #endregion

            #region draw the project region on the image
            if (homography != null)
            {  
                //draw a rectangle along the projected model
                Rectangle rect = modelImage.ROI;
                PointF[] pts = new PointF[] 
                                { 
                                new PointF(rect.Left, rect.Bottom),
                                new PointF(rect.Right, rect.Bottom),
                                new PointF(rect.Right, rect.Top),
                                new PointF(rect.Left, rect.Top)
                                };
                homography.ProjectPoints(pts);

                for (int i = 0; i < pts.Length; i++)
                    pts[i].Y += modelImage.Height;

                res.DrawPolyline(Array.ConvertAll<PointF, Point>(pts, Point.Round), true, new Gray(255.0), 1);
            }
            #endregion

            #endregion

            imageBox1.Image = frame0;
            imageBox4.Image = gray;
            imageBox5.Image = triangleRectangleImage;
            imageBox6.Image = cannyEdges;
            imageBox3.Image = lineImage;
            imageBox2.Image = res;
        }

        static bool IsPlaformCompatable()
        {
            int clrBitness = Marshal.SizeOf(typeof(IntPtr)) * 8;
            if (clrBitness != CvInvoke.UnmanagedCodeBitness)
            {
                MessageBox.Show(String.Format("Platform mismatched: CLR is {0} bit, C++ code is {1} bit."
                   + " Please consider recompiling the executable with the same platform target as C++ code.",
                   clrBitness, CvInvoke.UnmanagedCodeBitness));
                return false;
            }
            return true;
        }

        private void leftright1_ValueChanged(object sender, EventArgs e)
        {
        }

        private void updown1_ValueChanged(object sender, EventArgs e)
        {
        }

        private void width1_ValueChanged(object sender, EventArgs e)
        {
        }

        private void height1_ValueChanged(object sender, EventArgs e)
        {
        }

        private void rotate1_ValueChanged(object sender, EventArgs e)
        {
        }

        private void leftright2_ValueChanged(object sender, EventArgs e)
        {
        }

        private void updown2_ValueChanged(object sender, EventArgs e)
        {
        }

        private void width2_ValueChanged(object sender, EventArgs e)
        {
        }

        private void height2_ValueChanged(object sender, EventArgs e)
        {
        }

        private void rotate2_ValueChanged(object sender, EventArgs e)
        {
        }

        private void minwidthBox_ValueChanged(object sender, EventArgs e)
        {

        }

        private void thresholdBox_ValueChanged(object sender, EventArgs e)
        {

        }

        private void gapBox_ValueChanged(object sender, EventArgs e)
        {

        }
    }
}
