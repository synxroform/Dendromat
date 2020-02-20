using System;

using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Dendromat
{
	public class CircularCut : Shape
    {
        public static readonly DependencyProperty SpineRadiusProperty;
		public static readonly DependencyProperty SpineAngleProperty; 

        static CircularCut()
        {
            FrameworkPropertyMetadata SpineRadiusMTD = new FrameworkPropertyMetadata(100.0);
            SpineRadiusMTD.AffectsRender = true;
			SpineRadiusMTD.BindsTwoWayByDefault = true;
            SpineRadiusProperty = DependencyProperty.Register("SpineRadius", typeof(Double), typeof(CircularCut), SpineRadiusMTD);

            FrameworkPropertyMetadata SpineAngleMTD = new FrameworkPropertyMetadata(0.5);
            SpineAngleMTD.AffectsRender = true;
			SpineAngleMTD.BindsTwoWayByDefault = true;
            SpineAngleProperty = DependencyProperty.Register("SpineAngle", typeof(Double), typeof(CircularCut), SpineAngleMTD);
        }

        #region Dynamic Properties
        public Double SpineRadius
        {
            get { return (Double)GetValue(SpineRadiusProperty); }
            set { SetValue(SpineRadiusProperty, value); }
        }

        public Double SpineAngle
        {
            get { return (Double)GetValue(SpineAngleProperty); }
            set { SetValue(SpineAngleProperty, value); }
        }

        protected override Geometry DefiningGeometry
        {
            get { return GetOffsetGeometry(); }
        }

        public override Geometry RenderedGeometry
        {
            get { return GetOffsetGeometry(); }
        } 
        #endregion
	
		private Geometry GetOffsetGeometry()
		{
			StreamGeometry g = new StreamGeometry();
			using (StreamGeometryContext c = g.Open())
			{
                bool largecircle = true;
				double r1 = SpineRadius;
                if (SpineAngle < Math.PI) largecircle = false;

				Point p1 = new Point((r1 * Math.Cos(0)), (r1 * Math.Sin(0)));
				Point p2 = new Point((r1 * Math.Cos(SpineAngle)), (r1 * Math.Sin(SpineAngle)));
                p1.X += ActualWidth/2; 
                p1.Y += ActualHeight/2; 
                p2.X += ActualWidth/2; 
                p2.Y += ActualHeight/2;
				c.BeginFigure(p1, true, false);
				c.ArcTo(p2, new Size(r1, r1), 0, largecircle, SweepDirection.Clockwise, true, true);
			 }
			return g;
		}
    }
}
