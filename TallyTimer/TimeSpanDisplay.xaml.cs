// Copyright (c) Adam Nathan.  All rights reserved.
//
// By purchasing the book that this source code belongs to, you may use and
// modify this code for commercial and non-commercial applications, but you
// may not publish the source code.
// THE SOURCE CODE IS PROVIDED "AS IS", WITH NO WARRANTIES OR INDEMNITIES.
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace TallyTimer
{
  public partial class TimeSpanDisplay : UserControl
  {
    int digitWidth;
    TimeSpan time;

    public TimeSpanDisplay()
    {
      InitializeComponent();

      // In design mode, show something other than an empty StackPanel
      if (DesignerProperties.IsInDesignTool)
        this.LayoutRoot.Children.Add(new TextBlock { Text = "0:00.0" });
    }

    public int DigitWidth {
      get { return this.digitWidth; }
      set
      {
        this.digitWidth = value;
        // Force a display update using the new width:
        this.Time = this.time;
      }
    }

    public TimeSpan Time
    {
      get { return this.time; }
      set
      {
        this.LayoutRoot.Children.Clear();
        if (value < new TimeSpan(0, 0, 0, 0, 0))
        {
            value = InvertTimespan(value);
            this.LayoutRoot.Children.Add(new TextBlock { Text = "-" });
        }
        // Carve out the appropriate digits and add each individually

        // Support an arbitrary # of minutes digits (with no leading 0)
        string minutesString = value.Minutes.ToString();
        for (int i = 0; i < minutesString.Length; i++)
          AddDigitString(minutesString[i].ToString());

        this.LayoutRoot.Children.Add(new TextBlock { Text = ":" });

        // Seconds (always two digits, including a leading zero if necessary)
        AddDigitString((value.Seconds / 10).ToString());
        AddDigitString((value.Seconds % 10).ToString());

        // Add the decimal separator (a period for en-US)
        this.LayoutRoot.Children.Add(new TextBlock { Text = 
          CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator });

        // The Remainder (always a single digit)
        AddDigitString((value.Milliseconds / 100).ToString());

        this.time = value;
      }
    }

    private TimeSpan InvertTimespan(TimeSpan ts)
    {
        var days = ts.Days < 0 ? ts.Days * (-1) : ts.Days;
        var hours = ts.Hours < 0 ? ts.Hours * (-1) : ts.Hours;
        var minutes = ts.Minutes < 0 ? ts.Minutes * (-1) : ts.Minutes;
        var seconds = ts.Seconds < 0 ? ts.Seconds * (-1) : ts.Seconds;
        var milliseconds = ts.Milliseconds < 0 ? ts.Milliseconds * (-1) : ts.Milliseconds;
        return new TimeSpan(0, hours, minutes, seconds, milliseconds);
    }

    void AddDigitString(string digitString)
    {
      Border border = new Border { Width = this.DigitWidth };
      border.Child = new TextBlock { Text = digitString,
        HorizontalAlignment = HorizontalAlignment.Center };
      this.LayoutRoot.Children.Add(border);
    }
  }
}