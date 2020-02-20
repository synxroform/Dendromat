using System;


using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Dendromat
{

	

	public partial class SwitchControl : UserControl
	{
		static DoubleAnimation MoveHandleToLeft;
		static DoubleAnimation MoveHandleToRight;

		public static DependencyProperty ModeProperty;

		public OperationMode Mode 
		{
			get { return (OperationMode)GetValue(ModeProperty); }
			set { SetValue(ModeProperty, value); }
		}
		
		static SwitchControl()
		{
			var modeMTD = new FrameworkPropertyMetadata(OperationMode.Setup);
			modeMTD.AffectsRender = true;
			modeMTD.BindsTwoWayByDefault = true;
			modeMTD.PropertyChangedCallback = ModeChanged;
			
			ModeProperty = DependencyProperty.Register("Mode", typeof(OperationMode), typeof(SwitchControl), modeMTD);
			
			MoveHandleToRight = new DoubleAnimation(0, new Duration(new TimeSpan(0, 0, 0, 0, 700)));
			MoveHandleToRight.EasingFunction = new CubicEase() {EasingMode = EasingMode.EaseInOut };
			MoveHandleToLeft = new DoubleAnimation(-157.8, new Duration(new TimeSpan(0, 0, 0, 0, 700)));
			MoveHandleToLeft.EasingFunction = new CubicEase() {EasingMode = EasingMode.EaseInOut };
		}

		private static void ModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			SwitchControl sw = d as SwitchControl;
			if (d == null) return;
			switch ((OperationMode)e.NewValue) {
				case OperationMode.Setup :
					sw.SwitchHandleXform.BeginAnimation(TranslateTransform.XProperty, MoveHandleToRight);
				break;
				case OperationMode.Render :
					sw.SwitchHandleXform.BeginAnimation(TranslateTransform.XProperty, MoveHandleToLeft);
				break;
			}
		} 

		public SwitchControl()
		{
			InitializeComponent();
		}

		private void Switch_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			switch (Mode) {
				case OperationMode.Setup :
					Mode = OperationMode.Render;
				break;
				case OperationMode.Render :
					Mode = OperationMode.Setup;
				break;
			}
		}
	}
}
