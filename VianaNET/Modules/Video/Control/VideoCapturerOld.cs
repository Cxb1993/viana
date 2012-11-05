﻿
//using Microsoft.Win32;
//using MediaInfoLib;
//using System.Globalization;
//using System;
//using System.Windows.Controls;
//using System.Windows;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
//using System.IO;
//using DirectShowLib;
//using System.Diagnostics;

//namespace VianaNET
//{
//  public class VideoCapturer : DSCapture
//  {
//    ///////////////////////////////////////////////////////////////////////////////
//    // Defining Constants                                                        //
//    ///////////////////////////////////////////////////////////////////////////////
//    #region CONSTANTS
//    #endregion //CONSTANTS

//    ///////////////////////////////////////////////////////////////////////////////
//    // Defining Variables, Enumerations, Events                                  //
//    ///////////////////////////////////////////////////////////////////////////////
//    #region FIELDS

//    private Stopwatch frameWatch;

//    #endregion //FIELDS

//    ///////////////////////////////////////////////////////////////////////////////
//    // Construction and Initializing methods                                     //
//    ///////////////////////////////////////////////////////////////////////////////
//    #region CONSTRUCTION

//    public VideoCapturer()
//    {
//    }

//    #endregion //CONSTRUCTION

//    ///////////////////////////////////////////////////////////////////////////////
//    // Defining events, enums, delegates                                         //
//    ///////////////////////////////////////////////////////////////////////////////
//    #region EVENTS

//    public event EventHandler VideoCaptureDeviceOpened;

//    #endregion EVENTS

//    ///////////////////////////////////////////////////////////////////////////////
//    // Defining Properties                                                       //
//    ///////////////////////////////////////////////////////////////////////////////
//    #region PROPERTIES

//    public new double UniformHeight
//    {
//      get { return (double)GetValue(UniformHeightProperty); }
//      set { SetValue(UniformHeightProperty, value); }
//    }

//    public static readonly DependencyProperty UniformHeightProperty =
//      DependencyProperty.Register(
//      "UniformHeight",
//      typeof(double),
//      typeof(VideoCapturer),
//      new UIPropertyMetadata(default(double)));

//    public new double UniformWidth
//    {
//      get { return (double)GetValue(UniformWidthProperty); }
//      set { SetValue(UniformWidthProperty, value); }
//    }

//    public static readonly DependencyProperty UniformWidthProperty =
//      DependencyProperty.Register(
//      "UniformWidth",
//      typeof(double),
//      typeof(VideoCapturer),
//      new UIPropertyMetadata(default(double)));

//    /// <summary>
//    /// Time between frames in ms units.
//    /// </summary>
//    public double FrameTime
//    {
//      get { return (double)GetValue(FrameTimeProperty); }
//      set { SetValue(FrameTimeProperty, value); }
//    }

//    public static readonly DependencyProperty FrameTimeProperty =
//      DependencyProperty.Register(
//      "FrameTime",
//      typeof(double),
//      typeof(VideoCapturer),
//      new UIPropertyMetadata(default(double)));


//    public System.Drawing.Bitmap CurrentFrameBitmap
//    {
//      get { return (System.Drawing.Bitmap)GetValue(CurrentFrameBitmapProperty); }
//      set { SetValue(CurrentFrameBitmapProperty, value); }
//    }

//    public static readonly DependencyProperty CurrentFrameBitmapProperty =
//      DependencyProperty.Register(
//      "CurrentFrameBitmap",
//      typeof(System.Drawing.Bitmap),
//      typeof(VideoCapturer),
//      new PropertyMetadata(null));

//    public long CaptureTime
//    {
//      get { return this.frameWatch.ElapsedMilliseconds; }
//    }

//    #endregion //PROPERTIES

//    ///////////////////////////////////////////////////////////////////////////////
//    // Public methods                                                            //
//    ///////////////////////////////////////////////////////////////////////////////
//    #region PUBLICMETHODS
//    #endregion //PUBLICMETHODS

//    ///////////////////////////////////////////////////////////////////////////////
//    // Inherited methods                                                         //
//    ///////////////////////////////////////////////////////////////////////////////
//    #region OVERRIDES

//    protected override void OnUnloadedOverride()
//    {
//      // Don´t unload when just hiding surface...
//      //base.OnUnloadedOverride();
//    }

//    #endregion //OVERRIDES

//    ///////////////////////////////////////////////////////////////////////////////
//    // Eventhandler                                                              //
//    ///////////////////////////////////////////////////////////////////////////////
//    #region EVENTHANDLER

//    void VideoImage_SizeChanged(object sender, SizeChangedEventArgs e)
//    {
//      this.UniformWidth = this.VideoImage.ActualWidth;
//      this.UniformHeight = this.VideoImage.ActualHeight;
//    }

//    void VideoCapturer_MediaEnded(object sender, RoutedEventArgs e)
//    {
//      //this.Pause();
//    }

//    void VideoCapturer_MediaOpened(object sender, RoutedEventArgs e)
//    {
//      //this.VideoSource = this.D3DImage;
//      this.frameWatch.Start();
//      this.OnVideoCaptureDeviceOpened();
//    }

//    #endregion //EVENTHANDLER

//    ///////////////////////////////////////////////////////////////////////////////
//    // Methods and Eventhandling for Background tasks                            //
//    ///////////////////////////////////////////////////////////////////////////////
//    #region THREAD
//    #endregion //THREAD

//    ///////////////////////////////////////////////////////////////////////////////
//    // Methods for doing main class job                                          //
//    ///////////////////////////////////////////////////////////////////////////////
//    #region PRIVATEMETHODS

//    public System.Drawing.Bitmap CreateBitmapFromCurrentImageSource()
//    {
//      if (this.NaturalVideoWidth <= 0)
//      {
//        return null;
//      }

//      System.Drawing.Bitmap returnBitmap;
//      DrawingVisual visual = new DrawingVisual();
//      DrawingContext dc = visual.RenderOpen();
//      dc.DrawImage(this.D3DImage, new Rect(0, 0, this.NaturalVideoWidth, this.NaturalVideoHeight));
//      dc.Close();
//      RenderTargetBitmap rtp = new RenderTargetBitmap(this.NaturalVideoWidth, this.NaturalVideoHeight, 96d, 96d, PixelFormats.Default);
//      rtp.Render(visual);

//      using (MemoryStream outStream = new MemoryStream())
//      {
//        PngBitmapEncoder pnge = new PngBitmapEncoder();
//        pnge.Frames.Add(BitmapFrame.Create(rtp));
//        pnge.Save(outStream);
//        returnBitmap = new System.Drawing.Bitmap(outStream);
//        //returnBitmap.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);
//      }

//      return returnBitmap;
//    }

//    #endregion //PRIVATEMETHODS

//    ///////////////////////////////////////////////////////////////////////////////
//    // Small helping Methods                                                     //
//    ///////////////////////////////////////////////////////////////////////////////
//    #region HELPER

//    private void OnVideoCaptureDeviceOpened()
//    {
//      if (this.VideoCaptureDeviceOpened != null)
//      {
//        this.VideoCaptureDeviceOpened(this, EventArgs.Empty);
//      }
//    }

//    #endregion //HELPER
//  }
//}
