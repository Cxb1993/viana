﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Data;
using DirectShowLib;
using System.Windows.Controls;

namespace VianaNET
{
  public class Video : DependencyObject
  {
    public event EventHandler VideoFrameChanged;
    private static Video instance;

    private VideoMode videoMode;
    private VideoBase videoElement;
    private VideoCapturer videoCaptureElement;
    private VideoPlayer videoPlayerElement;

    public VideoMode VideoMode
    {
      get { return this.videoMode; }
      set { this.SetVideoMode(value); }
    }

    public int FrameIndex
    {
      get { return this.videoElement.MediaPositionFrameIndex; }
    }

    public long FrameTimestamp
    {
      get { return this.videoElement.MediaPositionInMS; }
    }

    public bool IsDataAcquisitionRunning
    {
      get { return (bool)GetValue(IsDataAcquisitionRunningProperty); }
      set { SetValue(IsDataAcquisitionRunningProperty, value); }
    }

    public static readonly DependencyProperty IsDataAcquisitionRunningProperty =
      DependencyProperty.Register(
      "IsDataAcquisitionRunning",
      typeof(bool),
      typeof(Video),
      new UIPropertyMetadata(false));

    public ImageSource VideoSource
    {
      get { return (ImageSource)GetValue(VideoSourceProperty); }
      set { SetValue(VideoSourceProperty, value); }
    }

    public static readonly DependencyProperty VideoSourceProperty =
      DependencyProperty.Register(
      "VideoSource",
      typeof(ImageSource),
      typeof(Video),
      new UIPropertyMetadata(null));

    //public System.Drawing.Bitmap CurrentFrameBitmap
    //{
    //  get { return (System.Drawing.Bitmap)GetValue(CurrentFrameBitmapProperty); }
    //  set { SetValue(CurrentFrameBitmapProperty, value); }
    //}

    //public static readonly DependencyProperty CurrentFrameBitmapProperty =
    //  DependencyProperty.Register(
    //  "CurrentFrameBitmap",
    //  typeof(System.Drawing.Bitmap),
    //  typeof(Video),
    //  new PropertyMetadata(null));

    private Video()
    {
      // Need to have the construction of the Video objects
      // in constructor, to get the databindings to their 
      // properties to work.
      this.videoPlayerElement = new VideoPlayer();
      //this.videoCaptureElement = new VideoCapturer();

      this.videoElement = this.videoPlayerElement;
      this.videoMode = VideoMode.None;
    }

    public VideoBase VideoElement
    {
      get { return this.videoElement; }
    }

    public VideoPlayer VideoPlayerElement
    {
      get { return this.videoPlayerElement; }
    }

    public VideoCapturer VideoCapturerElement
    {
      get { return this.videoCaptureElement; }
    }

    private void SetVideoMode(VideoMode newVideoMode)
    {
      if (this.videoMode == newVideoMode)
      {
        return;
      }

      this.videoElement.VideoFrameChanged -= new EventHandler(videoElement_VideoFrameChanged);

      //BindingOperations.ClearAllBindings(this);

      this.videoMode = newVideoMode;
      switch (this.videoMode)
      {
        case VideoMode.File:
          this.videoElement = this.videoPlayerElement;
          //Binding bitmapBinding = new Binding();
          //bitmapBinding.Source = this.videoPlayerElement;
          //bitmapBinding.Path = new PropertyPath("CurrentFrameBitmap");
          //BindingOperations.SetBinding(this, Video.CurrentFrameBitmapProperty, bitmapBinding);

          break;
        case VideoMode.Capture:
          this.videoElement = this.videoCaptureElement;
          break;
      }

      this.videoElement.VideoFrameChanged += new EventHandler(videoElement_VideoFrameChanged);
      this.VideoSource = this.videoElement.ImageSource;
    }

    void videoElement_VideoFrameChanged(object sender, EventArgs e)
    {
      this.OnVideoFrameChanged();
    }

    private void OnVideoFrameChanged()
    {
      if (this.VideoFrameChanged != null)
      {
        this.VideoFrameChanged(this, EventArgs.Empty);
      }
    }

    /// <summary>
    /// Gets the <see cref="Video"/> singleton.
    /// If the underlying instance is null, a instance will be created.
    /// </summary>
    public static Video Instance
    {
      get
      {
        // check again, if the underlying instance is null
        if (instance == null)
        {
          // create a new instance
          instance = new Video();
        }

        // return the existing/new instance
        return instance;
      }
    }

    public void StepOneFrame(bool forward)
    {
      switch (this.videoMode)
      {
        case VideoMode.File:
          this.videoPlayerElement.StepOneFrame(forward);
          break;
        case VideoMode.Capture:
          // Do nothing
          break;
      }
    }

    public System.Drawing.Bitmap CreateBitmapFromCurrentImageSource()
    {
      if (this.videoElement.NaturalVideoWidth <= 0)
      {
        return null;
      }

      System.Drawing.Bitmap returnBitmap;
      DrawingVisual visual = new DrawingVisual();
      DrawingContext dc = visual.RenderOpen();
      dc.DrawImage(
        this.videoElement.ImageSource,
        new Rect(0, 0, this.videoElement.NaturalVideoWidth, this.videoElement.NaturalVideoHeight));

      dc.Close();
      RenderTargetBitmap rtp = new RenderTargetBitmap(
        (int)this.videoElement.NaturalVideoWidth,
        (int)this.videoElement.NaturalVideoHeight,
        96d,
        96d,
        PixelFormats.Default);
      rtp.Render(visual);

      using (MemoryStream outStream = new MemoryStream())
      {
        PngBitmapEncoder pnge = new PngBitmapEncoder();
        pnge.Frames.Add(BitmapFrame.Create(rtp));
        pnge.Save(outStream);
        returnBitmap = new System.Drawing.Bitmap(outStream);
        //returnBitmap.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);
      }

      return returnBitmap;
    }

    public void Play()
    {
      this.videoElement.Play();
    }

    public void Pause()
    {
      this.videoElement.Pause();
    }

    public void Stop()
    {
      this.videoElement.Stop();
    }

    public void Revert()
    {
      this.videoElement.Revert();
    }

    //public void UpdateNativeBitmap()
    //{
    //  switch (this.videoMode)
    //  {
    //    case VideoMode.File:
    //      this.videoPlayerElement.UpdateNativeBitmap();
    //      break;
    //    case VideoMode.Capture:
    //      break;
    //  }
    //}

    public void Render(Image image)
    {
      switch (this.videoMode)
      {
        case VideoMode.File:
          this.videoPlayerElement.RenderVideo();
          break;
        case VideoMode.Capture:
          // Do not render, because the mapped view is already
          // populated with the video data
          break;
      }
    }
  }
}
