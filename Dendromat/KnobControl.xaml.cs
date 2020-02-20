using System;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dendromat
{
	
	public partial class KnobControl : UserControl
	{
		Point _capturePoint = new Point(0, 0);

		public static DependencyProperty ValueProperty;
		public static DependencyProperty DataLoadedProperty;

		static KnobControl()
		{
			var valueMTD = new FrameworkPropertyMetadata(0.0);
			valueMTD.BindsTwoWayByDefault = true;
			valueMTD.AffectsRender = true;
			valueMTD.PropertyChangedCallback = OnPropertyChanged;

			ValueProperty = DependencyProperty.Register("Value", typeof(Double), typeof(KnobControl), valueMTD);

			var dataLoadedMTD = new FrameworkPropertyMetadata(false);
			dataLoadedMTD.AffectsRender = true;

			DataLoadedProperty = DependencyProperty.Register("DataLoaded", typeof(Boolean), typeof(KnobControl), dataLoadedMTD);
		}

		public Double Value {
            get { return (Double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
		}

		public Boolean DataLoaded {
            get { return (Boolean)GetValue(DataLoadedProperty); }
            set { SetValue(DataLoadedProperty, value); }
		}

		private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			KnobControl sender = d as KnobControl;
			if (sender == null) return;
			switch (e.Property.Name) {
				case "Value" :
						sender.Value = Math.Min(1.0, Math.Max(0.0, (Double)e.NewValue));
						sender.KnobControlLightXform.Angle = sender.Value * 360;
						sender.NeonBase.SpineAngle = sender.Value * 1.34 * Math.PI;
						sender.PercentReadout.Content = sender.Value.ToString("000%");
				break;
			}
		}
		
		public KnobControl()
		{
			InitializeComponent();
		}

		private void Knob_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			KnobSurface.CaptureMouse();
		}

		private void Knob_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			KnobSurface.ReleaseMouseCapture();
			_capturePoint = new Point();
		}
		
		private void Knob_MouseMove(object sender, MouseEventArgs e)
		{
			if (!KnobSurface.IsMouseCaptured) return;
			if (_capturePoint.X == 0 && _capturePoint.Y == 0) {
				_capturePoint = e.GetPosition(KnobSurface);
				return;
			}
			Value += (_capturePoint.Y - e.GetPosition(KnobSurface).Y) * 0.005;
			_capturePoint = e.GetPosition(KnobSurface);
		}

		private void SetData_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			DmatCommands.LoadData.Execute(null, this);
		}
	}
}
