﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using System.Windows.Shapes;
using Microsoft.Research.Kinect.Nui;
using KinectNui = Microsoft.Research.Kinect.Nui;
using System.ComponentModel;
using System.Windows.Media.Imaging;




namespace Microsoft.Samples.Kinect.WpfViewers
{
    /// <summary>
    /// Interaction logic for KinectDiagnosticViewer.xaml
    /// </summary>
    /// 

    public partial class KinectDiagnosticViewer : UserControl
    {
        // POSE RECOGNITION
        List<Example> bodyTS, leftHandTS, rightHandTS, leftLegTS, rightLegTS;
        List<String> bodyClasses, handClasses, legClasses;
        List<String> attributes;
        DTNode bodyDT, leftHandDT, rightHandDT, leftLegDT, rightLegDT;

        List<String> bodyCommonPosition, leftHandCommonPosition, rightHandCommonPosition, leftLegCommonPosition, rightLegCommonPosition;

        MotionUtils motionUtils;
        int contor;
        State bodyState, leftHandState, rightHandState, leftLegState, rightLegState;
        //TextWriter tw;


        // modified
        #region Public API
        public KinectDiagnosticViewer()
        {
            InitializeComponent();
            /*
            bodyMM.IsEnabled = false;
            leftHandMM.IsEnabled = false;
            rightHandMM.IsEnabled = false;
            leftLegMM.IsEnabled = false;
            rightLegMM.IsEnabled = false;
            */

            contor = 0;

            motionUtils = new MotionUtils();
            bodyState = new State(Constants.stateType.INTERMEDIATE, "");
            bodyState.setName("D");
            leftHandState = new State(Constants.stateType.INTERMEDIATE, "");
            leftHandState.setName("Lc");
            rightHandState = new State(Constants.stateType.INTERMEDIATE, "");
            rightHandState.setName("Lc");
            leftLegState = new State(Constants.stateType.INTERMEDIATE, "");
            leftLegState.setName("D");
            rightLegState = new State(Constants.stateType.INTERMEDIATE, "");
            rightLegState.setName("D");

            bodyCommonPosition = new List<string>();
            leftHandCommonPosition = new List<string>();
            rightHandCommonPosition = new List<string>();
            leftLegCommonPosition = new List<string>();
            rightLegCommonPosition = new List<string>();
        }

        public RuntimeOptions RuntimeOptions { get; private set; }

        public void ReInitRuntime()
        {
            // Will call Uninitialize followed by Initialize.
            this.Kinect = this.Kinect;
        }

        public KinectNui.Runtime Kinect
        {
            get { return _Kinect; }
            set
            {
                //Clean up existing runtime if we are being set to null, or a new Runtime.
                if (_Kinect != null)
                {
                    kinectColorViewer.Kinect = null;
                    kinectDepthViewer.Kinect = null;
                    _Kinect.SkeletonFrameReady -= new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);
                    _Kinect.Uninitialize();
                }

                _Kinect = value;

                if (_Kinect != null)
                {
                    InitRuntime();
                    kinectColorViewer.Kinect = _Kinect;
                    kinectDepthViewer.RuntimeOptions = RuntimeOptions;
                    kinectDepthViewer.Kinect = _Kinect;
                    _Kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);
                    UpdateUi();
                }
            }
        }

        //Status and InstanceIndex can change. Those properties in the Runtime should be made to support INotifyPropertyChange.
        public void UpdateUi()
        {
            //kinectIndex.Text = _Kinect.InstanceIndex.ToString();
            //kinectName.Text = _Kinect.InstanceName;
            //kinectStatus.Text = _Kinect.Status.ToString();
        }
        #endregion Public API

        #region Init
        private void InitRuntime()
        {
            //Some Runtimes' status will be NotPowered, or some other error state. Only want to Initialize the runtime, if it is connected.
            if (_Kinect.Status == KinectStatus.Connected)
            {
                bool skeletalViewerAvailable = IsSkeletalViewerAvailable;

                // NOTE:  Skeletal tracking only works on one Kinect per process right now.
                RuntimeOptions = skeletalViewerAvailable ?
                                     RuntimeOptions.UseDepthAndPlayerIndex | RuntimeOptions.UseSkeletalTracking | RuntimeOptions.UseColor
                                     : RuntimeOptions.UseDepth | RuntimeOptions.UseColor;
                _Kinect.Initialize(RuntimeOptions);
                skeletonPanel.Visibility = skeletalViewerAvailable ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                if (RuntimeOptions.HasFlag(RuntimeOptions.UseSkeletalTracking))
                {
                    _Kinect.SkeletonEngine.TransformSmooth = true;
                }
            }
        }

        /// <summary>
        /// Skeletal tracking only works on one Kinect right now.  So return false if it is already in use.
        /// </summary>
        private bool IsSkeletalViewerAvailable
        {
            get { return KinectNui.Runtime.Kinects.All(k => k.SkeletonEngine == null); }
        }

        #endregion Init

        #region Skeleton Processing
        private void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {

            SkeletonFrame skeletonFrame = e.SkeletonFrame;
            //tw = new StreamWriter("date.txt", true);
            //KinectSDK TODO: this shouldn't be needed, but if Power is removed from the Kinect, you may still get an event here, but skeletonFrame will be null.
            if (skeletonFrame == null)
            {
                return;
            }

            int iSkeleton = 0;
            Brush[] brushes = new Brush[6];
            brushes[0] = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            brushes[1] = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            brushes[2] = new SolidColorBrush(Color.FromRgb(64, 255, 255));
            brushes[3] = new SolidColorBrush(Color.FromRgb(255, 255, 64));
            brushes[4] = new SolidColorBrush(Color.FromRgb(255, 64, 255));
            brushes[5] = new SolidColorBrush(Color.FromRgb(128, 128, 255));

            skeletonCanvas.Children.Clear();

            foreach (SkeletonData data in skeletonFrame.Skeletons)
            {
                if (SkeletonTrackingState.Tracked == data.TrackingState)
                {
                    // Draw bones
                    Brush brush = brushes[iSkeleton % brushes.Length];
                    skeletonCanvas.Children.Add(getBodySegment(data.Joints, brush, JointID.HipCenter, JointID.Spine, JointID.ShoulderCenter, JointID.Head));
                    skeletonCanvas.Children.Add(getBodySegment(data.Joints, brush, JointID.ShoulderCenter, JointID.ShoulderLeft, JointID.ElbowLeft, JointID.WristLeft, JointID.HandLeft));
                    skeletonCanvas.Children.Add(getBodySegment(data.Joints, brush, JointID.ShoulderCenter, JointID.ShoulderRight, JointID.ElbowRight, JointID.WristRight, JointID.HandRight));
                    skeletonCanvas.Children.Add(getBodySegment(data.Joints, brush, JointID.HipCenter, JointID.HipLeft, JointID.KneeLeft, JointID.AnkleLeft, JointID.FootLeft));
                    skeletonCanvas.Children.Add(getBodySegment(data.Joints, brush, JointID.HipCenter, JointID.HipRight, JointID.KneeRight, JointID.AnkleRight, JointID.FootRight));

                    // Draw joints
                    //tw.WriteLine("");

                    Point3D[] points = new Point3D[20];
                    foreach (Joint joint in data.Joints)
                    {
                        //tw.WriteLine(joint.Position.X + " " + joint.Position.Y + " " + joint.Position.Z);
                        Point jointPos = getDisplayPosition(joint);
                        Line jointLine = new Line();
                        jointLine.X1 = jointPos.X - 3;
                        jointLine.X2 = jointLine.X1 + 6;
                        jointLine.Y1 = jointLine.Y2 = jointPos.Y;
                        jointLine.Stroke = jointColors[joint.ID];
                        jointLine.StrokeThickness = 6;
                        skeletonCanvas.Children.Add(jointLine);
                        points[(int)joint.ID] = new Point3D(joint.Position.X, joint.Position.Y, joint.Position.Z);

                    }

                    if (b1.IsEnabled == false)
                    {
                        // get position for each body part
                        // BODY
                        Example bodyExample = new Example();
                        double angle = GeometryUtils.getAngle(points[2], points[1], points[13]);
                        bodyExample.add("A.2.1.13", angle);
                        angle = GeometryUtils.getAngle(points[2], points[1], points[17]);
                        bodyExample.add("A.2.1.17", angle);
                        angle = GeometryUtils.getAngle(points[1], points[2], Constants.planeType.XOY);
                        bodyExample.add("A.1.2.XoY", angle);
                        angle = GeometryUtils.getAngle(points[1], points[2], Constants.planeType.YOZ);
                        bodyExample.add("A.1.2.YoZ", angle);

                        // LEFT HAND
                        Example leftHandExample = new Example();
                        angle = GeometryUtils.getAngle(points[4], points[5], Constants.planeType.XOY);
                        leftHandExample.add("A.4.5.XoY", angle);
                        angle = GeometryUtils.getAngle(points[4], points[5], Constants.planeType.YOZ);
                        leftHandExample.add("A.4.5.YoZ", angle);
                        angle = points[4].getY() - points[5].getY();
                        leftHandExample.add("D.4y.5y", angle);

                        Example rightHandExample = new Example();
                        angle = GeometryUtils.getAngle(points[8], points[9], Constants.planeType.XOY);
                        rightHandExample.add("A.8.9.XoY", angle);
                        angle = GeometryUtils.getAngle(points[8], points[9], Constants.planeType.YOZ);
                        rightHandExample.add("A.8.9.YoZ", angle);
                        angle = points[8].getY() - points[9].getY();
                        rightHandExample.add("D.8y.9y", angle);

                        Example leftLegExample = new Example();
                        angle = GeometryUtils.getAngle(points[12], points[13], points[14]);
                        leftLegExample.add("A.12.13.14", angle);
                        angle = GeometryUtils.getAngle(points[13], points[14], Constants.planeType.XOY);
                        leftLegExample.add("A.13.14.XoY", angle);
                        angle = GeometryUtils.getAngle(points[13], points[14], Constants.planeType.YOZ);
                        leftLegExample.add("A.13.14.YoZ", angle);

                        Example rightLegExample = new Example();
                        angle = GeometryUtils.getAngle(points[16], points[17], points[18]);
                        rightLegExample.add("A.16.17.18", angle);
                        angle = GeometryUtils.getAngle(points[17], points[18], Constants.planeType.XOY);
                        rightLegExample.add("A.17.18.XoY", angle);
                        angle = GeometryUtils.getAngle(points[17], points[18], Constants.planeType.YOZ);
                        rightLegExample.add("A.17.18.YoZ", angle);
                        rightLegPositionText.Text = DTUtils.findValueInTree(rightLegDT, rightLegExample);

                        String bodyPosition = DTUtils.findValueInTree(bodyDT, bodyExample);
                        String leftHandPosition = DTUtils.findValueInTree(leftHandDT, leftHandExample);
                        String rightHandPosition = DTUtils.findValueInTree(rightHandDT, rightHandExample);
                        String leftLegPosition = DTUtils.findValueInTree(leftLegDT, leftLegExample);
                        String rightLegPosition = DTUtils.findValueInTree(rightLegDT, rightLegExample);

                        if (contor < Constants.SnapshotFrequency)
                        {
                            bodyCommonPosition.Add(bodyPosition);
                            rightHandCommonPosition.Add(rightHandPosition);
                            leftHandCommonPosition.Add(leftHandPosition);
                            rightLegCommonPosition.Add(rightLegPosition);
                            leftLegCommonPosition.Add(leftLegPosition);
                            contor++;
                        }
                        else
                        {
                            if (rightLegPosition.Equals("indoit") && leftLegPosition.Equals("indoit") && bodyPosition.Equals("aplecat frontal"))
                            {
                                bodyPositionText.Text = "spre ghemuire";
                            }
                            else
                            {
                                // get position and motion
                                bodyPositionText.Text = PositionUtils.getCommon(bodyCommonPosition);
                                if (bodyPositionText.Text.Equals("culcat") == true)
                                {
                                    leftHandPositionText.Text = "";
                                    rightHandPositionText.Text = "";
                                    leftLegPositionText.Text = "";
                                    rightLegPositionText.Text = "";

                                    bodyState = motionUtils.getNextBodyState(bodyState.getName(), bodyPositionText.Text);

                                    //bodyMotionText.Text = bodyState.getMotion();
                                    leftHandMotionText.Text = "";
                                    rightHandMotionText.Text = "";
                                    leftLegMotionText.Text = "";
                                    rightLegMotionText.Text = "";

                                    bodyCommonPosition = new List<string>();
                                    leftHandCommonPosition = new List<string>();
                                    rightHandCommonPosition = new List<string>();
                                    leftLegCommonPosition = new List<string>();
                                    rightLegCommonPosition = new List<string>();
                                }
                                else
                                {
                                    leftHandPositionText.Text = PositionUtils.getCommon(leftHandCommonPosition);
                                    rightHandPositionText.Text = PositionUtils.getCommon(rightHandCommonPosition);
                                    leftLegPositionText.Text = PositionUtils.getCommon(leftLegCommonPosition);
                                    rightLegPositionText.Text = PositionUtils.getCommon(rightLegCommonPosition);

                                    bodyState = motionUtils.getNextBodyState(bodyState.getName(), bodyPositionText.Text);
                                    leftHandState = motionUtils.getNextHandState(leftHandState.getName(), leftHandPositionText.Text);
                                    rightHandState = motionUtils.getNextHandState(rightHandState.getName(), rightHandPositionText.Text);
                                    leftLegState = motionUtils.getNextLegState(leftLegState.getName(), leftLegPositionText.Text);
                                    rightLegState = motionUtils.getNextLegState(rightLegState.getName(), rightLegPositionText.Text);

                                    bodyMotionText.Text = bodyState.getMotion();
                                    leftHandMotionText.Text = leftHandState.getMotion();
                                    rightHandMotionText.Text = rightHandState.getMotion();
                                    leftLegMotionText.Text = leftLegState.getMotion();
                                    rightLegMotionText.Text = rightLegState.getMotion();

                                    bodyCommonPosition = new List<string>();
                                    leftHandCommonPosition = new List<string>();
                                    rightHandCommonPosition = new List<string>();
                                    leftLegCommonPosition = new List<string>();
                                    rightLegCommonPosition = new List<string>();
                                }
                                // reset counter
                                contor = 0;
                            }
                        }
                    }
                }
                iSkeleton++;

            } // for each skeleton
            //tw.Close();
        }

        private Polyline getBodySegment(Microsoft.Research.Kinect.Nui.JointsCollection joints, Brush brush, params JointID[] ids)
        {
            PointCollection points = new PointCollection(ids.Length);
            for (int i = 0; i < ids.Length; ++i)
            {
                points.Add(getDisplayPosition(joints[ids[i]]));
            }

            Polyline polyline = new Polyline();
            polyline.Points = points;
            polyline.Stroke = brush;
            polyline.StrokeThickness = 5;
            return polyline;
        }

        private Point getDisplayPosition(Joint joint)
        {
            float depthX, depthY;
            Kinect.SkeletonEngine.SkeletonToDepthImage(joint.Position, out depthX, out depthY);
            depthX = depthX * 320; //convert to 320, 240 space
            depthY = depthY * 240; //convert to 320, 240 space
            int colorX, colorY;
            ImageViewArea iv = new ImageViewArea();
            // only ImageResolution.Resolution640x480 is supported at this point
            Kinect.NuiCamera.GetColorPixelCoordinatesFromDepthPixel(ImageResolution.Resolution640x480, iv, (int)depthX, (int)depthY, (short)0, out colorX, out colorY);

            // map back to skeleton.Width & skeleton.Height
            return new Point((int)(skeletonCanvas.Width * colorX / 640.0), (int)(skeletonCanvas.Height * colorY / 480));
        }

        private static Dictionary<JointID, Brush> jointColors = new Dictionary<JointID, Brush>() { 
            {JointID.HipCenter, new SolidColorBrush(Color.FromRgb(169, 176, 155))},
            {JointID.Spine, new SolidColorBrush(Color.FromRgb(169, 176, 155))},
            {JointID.ShoulderCenter, new SolidColorBrush(Color.FromRgb(168, 230, 29))},
            {JointID.Head, new SolidColorBrush(Color.FromRgb(200, 0,   0))},
            {JointID.ShoulderLeft, new SolidColorBrush(Color.FromRgb(79,  84,  33))},
            {JointID.ElbowLeft, new SolidColorBrush(Color.FromRgb(84,  33,  42))},
            {JointID.WristLeft, new SolidColorBrush(Color.FromRgb(255, 126, 0))},
            {JointID.HandLeft, new SolidColorBrush(Color.FromRgb(215,  86, 0))},
            {JointID.ShoulderRight, new SolidColorBrush(Color.FromRgb(33,  79,  84))},
            {JointID.ElbowRight, new SolidColorBrush(Color.FromRgb(33,  33,  84))},
            {JointID.WristRight, new SolidColorBrush(Color.FromRgb(77,  109, 243))},
            {JointID.HandRight, new SolidColorBrush(Color.FromRgb(37,   69, 243))},
            {JointID.HipLeft, new SolidColorBrush(Color.FromRgb(77,  109, 243))},
            {JointID.KneeLeft, new SolidColorBrush(Color.FromRgb(69,  33,  84))},
            {JointID.AnkleLeft, new SolidColorBrush(Color.FromRgb(229, 170, 122))},
            {JointID.FootLeft, new SolidColorBrush(Color.FromRgb(255, 126, 0))},
            {JointID.HipRight, new SolidColorBrush(Color.FromRgb(181, 165, 213))},
            {JointID.KneeRight, new SolidColorBrush(Color.FromRgb(71, 222,  76))},
            {JointID.AnkleRight, new SolidColorBrush(Color.FromRgb(245, 228, 156))},
            {JointID.FootRight, new SolidColorBrush(Color.FromRgb(77,  109, 243))}
        };
        #endregion Skeleton Processing

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        #endregion INotifyPropertyChanged


        #region Private State
        private KinectNui.Runtime _Kinect;
        private String bodyPosition = "dreeept";
        #endregion Private State

        void Onb2Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Begin training...");

            bodyTS = IOUtils.readBodyTS();
            text2.Text = "Body training set read.";
            leftHandTS = IOUtils.readLeftHandTS();
            text2.Text = "Left hand training set read.";
            rightHandTS = IOUtils.readRightHandTS();
            text2.Text = "Right Hand training set read.";
            leftLegTS = IOUtils.readLeftLegTS();
            text2.Text = "Left leg training set read.";
            rightLegTS = IOUtils.readRightLegTS();
            text2.Text = "Right leg training set read.";

            Console.WriteLine("Initialize attributes and classses...");
            bodyClasses = new List<String>();
            bodyClasses.Add("drept");
            bodyClasses.Add("aplecat frontal");
            bodyClasses.Add("aplecat lateral");
            bodyClasses.Add("asezat");
            bodyClasses.Add("culcat");

            handClasses = new List<String>();
            handClasses.Add("langa corp");
            handClasses.Add("ridicat lateral");
            handClasses.Add("ridicat frontal");
            handClasses.Add("ridicat sus");

            legClasses = new List<String>();
            legClasses.Add("drept");
            legClasses.Add("ridicat lateral");
            legClasses.Add("ridicat frontal");
            legClasses.Add("indoit");

            // set attributes for each decision tree
            attributes = new List<String>();
            attributes.Add("A.2.1.13");
            attributes.Add("A.2.1.17");
            attributes.Add("A.1.2.XoY");
            attributes.Add("A.1.2.YoZ");

            attributes.Add("A.2.1.13");
            attributes.Add("A.2.1.17");
            attributes.Add("A.1.2.XoY");
            attributes.Add("A.1.2.YoZ");
            bodyDT = new DTNode(bodyTS, attributes, bodyClasses);
            
            attributes = new List<String>();
            attributes.Add("A.4.5.XoY");
            attributes.Add("A.4.5.YoZ");
            attributes.Add("D.4y.5y");

            //attributes.Add("A.4.5.XoY");
            //attributes.Add("A.4.5.YoZ");
            //attributes.Add("D.4y.5y");
            leftHandDT = new DTNode(leftHandTS, attributes, handClasses);

            attributes = new List<String>();
            attributes.Add("A.8.9.XoY");
            attributes.Add("A.8.9.YoZ");
            attributes.Add("D.8y.9y");

            attributes.Add("A.8.9.XoY");
            attributes.Add("A.8.9.YoZ");
            attributes.Add("D.8y.9y");
            rightHandDT = new DTNode(rightHandTS, attributes, handClasses);

            attributes = new List<String>();
            attributes.Add("A.12.13.14");
            attributes.Add("A.13.14.XoY");
            attributes.Add("A.13.14.YoZ");

            attributes.Add("A.12.13.14");
            attributes.Add("A.13.14.XoY");
            attributes.Add("A.13.14.YoZ");
            leftLegDT = new DTNode(leftLegTS, attributes, legClasses);

            attributes = new List<String>();
            attributes.Add("A.16.17.18");
            attributes.Add("A.17.18.XoY");
            attributes.Add("A.17.18.YoZ");

            attributes.Add("A.16.17.18");
            attributes.Add("A.17.18.XoY");
            attributes.Add("A.17.18.YoZ");
            rightLegDT = new DTNode(rightLegTS, attributes, legClasses);
            
            Console.WriteLine("Build trees...");
            DTUtils.buildTree(bodyDT);
            Console.WriteLine("Body tree built.");
            DTUtils.buildTree(leftHandDT);
            Console.WriteLine("Left hand tree built.");
            DTUtils.buildTree(rightHandDT);
            Console.WriteLine("Right hand built.");
            DTUtils.buildTree(leftLegDT);
            Console.WriteLine("Left leg built.");
            DTUtils.buildTree(rightLegDT);
            Console.WriteLine("Right leg built.");

            text2.Text = "Done!";
            b1.IsEnabled = false;
            /*
            bodyMM.IsEnabled = true;
            leftHandMM.IsEnabled = true;
            rightHandMM.IsEnabled = true;
            leftLegMM.IsEnabled = true;
            rightLegMM.IsEnabled = true;
             * */
        }
    }
}
