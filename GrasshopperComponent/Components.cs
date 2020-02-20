using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Parameters;

using Rhino.Geometry;

namespace Zeometry
{

/*
	public class GH_Intermit : GH_Component
	{

		public GH_Intermit() : base("Intermit", "IMit", "Intermitted objects pattern.", "Zeo", "Main")
		{
			// empty constructor.
		}

 		public override Guid ComponentGuid
		{
			get { return new Guid("92696B34-A198-41CC-A675-C69A70CC6DD0"); }
		}
  
		protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
		{
			pManager.AddCurveParameter("curves", "c", "Co-planar curves.", GH_ParamAccess.list);
			pManager.AddNumberParameter("distance", "d", "Distance between offsets.", GH_ParamAccess.item, 1.0);
			pManager.AddIntegerParameter("number", "n", "Number of copies.", GH_ParamAccess.item, 4);
		}

		protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
		{
			pManager.AddCurveParameter("out", "out", "Output curves.", GH_ParamAccess.list);
		}

		protected override void SolveInstance(IGH_DataAccess DA)
		{
			List<Curve> input = new List<Curve>();
			if (DA.GetDataList("curves", input))
			{
				if (!input.TrueForAll((c) => c.IsClosed) || !input.TrueForAll((c) => c.IsPlanar())) 
				{
					AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Curves are not planar or not closed.");
					return;
				}
				if (!InCommonPlane(input))
				{
					AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Curves are not in common plane.");
					return;
				}

				double distance = 1.0; DA.GetData("distance", ref distance);
				int number = 4; DA.GetData("number", ref number);

				List<Curve> ordered = input.Where((c, idx) => (idx % 2) == 0)
					.Concat(input.Where((c, idx) => (idx % 2) == 1)).ToList();
				List<Curve> feedback = new List<Curve>(ordered);
				for (int k = 1; k <= number; k++) feedback.AddRange(Offset(ordered, k * distance));
				DA.SetDataList("out", Fold(feedback.Take(feedback.Count - 1), feedback.Skip(feedback.Count - 1)));
			}
		}

		private bool InCommonPlane(List<Curve> input)
		{
			Plane p0 = Plane.Unset;	input[0].TryGetPlane(out p0);
			double t = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
			if (input.TrueForAll((c) => { 
				Plane p1 = Plane.Unset; 
				c.TryGetPlane(out p1); 
				return p0.Normal.IsPerpendicularTo(p1.XAxis) && Math.Abs(p0.DistanceTo(p1.Origin)) < t; 
			})) return true;
			return false;
		}

		private List<Curve> Atop(Curve top, List<Curve> bottom)
		{
			double t = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
			Plane cp = Plane.WorldXY;
			top.TryGetPlane(out cp);

			List<Curve> result = new List<Curve>();
			foreach (Curve c in bottom) {
				var xs = Rhino.Geometry.Intersect.Intersection.CurveCurve(top, c, t, t);
				List<double> par = new List<double>();
				foreach (var e in xs) if (e.IsPoint) par.Add(e.ParameterB);
					List <Curve> curves = c.Split(par).ToList().FindAll((Curve cu) => {
					Point3d pt = cu.PointAtNormalizedLength(0.5);
					return top.Contains(pt, cp) == PointContainment.Outside;
				});
				Curve[] joined = Curve.JoinCurves(curves);
				result.AddRange(joined);
			}
			return result;
		}

		private List<Curve> Fold(IEnumerable<Curve> curves, IEnumerable<Curve> bottom)
		{
			List<Curve> x = bottom.ToList();
			List<Curve> y = curves.ToList();
			y.Reverse();
			foreach (Curve c in y) {
				x = Atop(c, x);
				x.Add(c);
			}
			return x;
		}

		private  List<Curve> Offset(List<Curve> input, double distance)
		{
			List<Curve> r = new List<Curve>();
			double t = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
			foreach (Curve c in input) {
				Plane cp = Plane.WorldXY;
				c.TryGetPlane(out cp);
				r.Add(c.Offset(cp, distance, t, CurveOffsetCornerStyle.Smooth)[0]);
			}
			return r;
		}
	}
*/
	
	public class GH_Dendron : GH_Component
	{
		public GH_Dendron() : base("Dendron", "Dendron", "Grows manually defined tree system.", "Transform", "Morph")
		{
			// empty constructor.
		}

		protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
		{
			pManager.AddCurveParameter("root", "root", "Root curve of the tree.", GH_ParamAccess.item);
			pManager.AddCurveParameter("crown", "crown", "Remaining branches of the tree.", GH_ParamAccess.list);
			pManager.AddNumberParameter("p", "p", "Parameter control [0..1].", GH_ParamAccess.item, 0.5);
		}

		protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
		{
			pManager.AddCurveParameter("curves", "cu", "Growing curves.", GH_ParamAccess.list);
			pManager.AddPointParameter("points", "pt", "", GH_ParamAccess.list);
		}

		protected override void SolveInstance(IGH_DataAccess DA)
		{
			Curve root = null;
			List<Curve> crown = new List<Curve>();
			if (DA.GetData("root", ref root))
			{
				if(DA.GetDataList("crown", crown))
				{
					double p = 0;
					DA.GetData("p", ref p);

					var tree = Branch.OptiMerge(root, crown);
					tree.MakeStencils();
					double len = tree.Length();
					
					var flat = tree.Flatten();
					var curves = flat.Select((x) => x.GlobalTrim(p * len)).Where((x) => x != null);
					var points = flat.Select((x) => x.GlobalPoint(p * len)).Where((x) => x != null);
										
					DA.SetDataList("curves", curves);
					DA.SetDataList("points", points);
				}
			}
		}

		public override Guid ComponentGuid
		{
			get { return new Guid("3417296C-E368-4ED4-A901-3570DA68B092"); }
		}

		protected override Bitmap Icon
		{
			get
			{
				return Zeometry.Properties.Resources.DendronIcon;
			}
		}
	}
}
