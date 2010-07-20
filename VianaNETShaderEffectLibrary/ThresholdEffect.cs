﻿using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace VianaNETShaderEffectLibrary
{
  public class ThresholdEffect : ShaderEffect
  {
    private static PixelShader _pixelShader = new PixelShader();

    #region Constructors

    static ThresholdEffect()
    {
      _pixelShader.UriSource = Global.MakePackUri("ThresholdEffect.ps");
    }

    public ThresholdEffect()
    {
      this.PixelShader = _pixelShader;

      // Update each DependencyProperty that's registered with a shader register.  This
      // is needed to ensure the shader gets sent the proper default value.
      UpdateShaderValue(InputProperty);
      UpdateShaderValue(ThresholdProperty);
      UpdateShaderValue(BlankColorProperty);
    }

    #endregion

    public Brush Input
    {
      get { return (Brush)GetValue(InputProperty); }
      set { SetValue(InputProperty, value); }
    }

    public static readonly DependencyProperty InputProperty =
        ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(ThresholdEffect), 0);

    public double Threshold
    {
      get { return (double)GetValue(ThresholdProperty); }
      set { SetValue(ThresholdProperty, value); }
    }

    public static readonly DependencyProperty ThresholdProperty =
        DependencyProperty.Register("Threshold", typeof(double), typeof(ThresholdEffect),
                new UIPropertyMetadata(0.5, PixelShaderConstantCallback(0)));

    public Color BlankColor
    {
      get { return (Color)GetValue(BlankColorProperty); }
      set { SetValue(BlankColorProperty, value); }
    }

    public static readonly DependencyProperty BlankColorProperty =
        DependencyProperty.Register("BlankColor", typeof(Color), typeof(ThresholdEffect),
                new UIPropertyMetadata(Colors.Transparent, PixelShaderConstantCallback(1)));

    public Color TargetColor
    {
      get { return (Color)GetValue(TargetColorProperty); }
      set { SetValue(TargetColorProperty, value); }
    }

    public static readonly DependencyProperty TargetColorProperty =
        DependencyProperty.Register("TargetColor", typeof(Color), typeof(ThresholdEffect),
                new UIPropertyMetadata(Colors.Red, PixelShaderConstantCallback(2)));

  }
}
