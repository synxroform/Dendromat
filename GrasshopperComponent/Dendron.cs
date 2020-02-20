using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace Zeometry
{
	class Branch
	{
		Curve Path;
		List<Branch> SideBranches = null;
		double AttachPoint;
		double MinStencil;
		double MaxStencil;

		Branch(Curve branch, double attachpoint = 0)
		{
			Path = branch;
			AttachPoint = attachpoint;
			MinStencil = MaxStencil = 0;
		}

		public Branch(Curve root, List<Curve> crown)
		{
			Path = root;
			AttachPoint = 0;
			Merge(crown);
			MakeStencils();
		}

		public void MakeStencils(double min = 0)
		{
			MinStencil = min + AttachPoint;
			MaxStencil = MinStencil + Path.Domain.Length;
			if (SideBranches != null)
			{
				foreach (var child in SideBranches)
				{
					child.MakeStencils(MinStencil);
				}
			}
		}

		public static Branch OptiMerge(Curve root_curve, ICollection<Curve> crown)
		{
			var root = new Branch(root_curve);
			var global_slice = new List<Branch> {root};
			var global_remain = crown;

			double mat = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;			

			while (true)
			{
				var local_slice = new List<Branch>();

				foreach (var s in global_slice)
				{
					var local_remain = new List<Curve>();

					foreach (var cu in global_remain)
					{
						// begin intersection
						using (var x = Intersection.CurveCurve(s.Path, cu, mat, mat))
						{
							if (x.Count > 0)
							{
								if (s.SideBranches == null) s.SideBranches = new List<Branch>();
								var ptm = x.OrderBy((e) => e.ParameterA).ElementAt(0);
								var ptb = ptm.ParameterB;

								if (Math.Abs(ptb - cu.Domain.Min) <= mat * 10 || Math.Abs(ptb - cu.Domain.Max) <= mat * 10)
								{
									var child = new Branch(cu, ptm.ParameterA - s.Path.Domain.Min);
									if (Math.Abs(ptb - cu.Domain.Max) <= mat * 10)
									{
										child.Path.Reverse();
									}
									s.SideBranches.Add(child);
									local_slice.Add(child);
								}
								else
								{
									if (!cu.IsClosed)
									{
										var split = cu.Split(ptb);
										foreach (var sp in split)
										{
											if (Math.Abs(sp.Domain.Max - ptb) <= mat * 10) sp.Reverse();
											var child = new Branch(sp, ptm.ParameterA - s.Path.Domain.Min);
											s.SideBranches.Add(child);
											local_slice.Add(child);
										}
									}
									else
									{
										cu.ChangeClosedCurveSeam(ptm.ParameterB);
										var child = new Branch(cu, ptm.ParameterA - s.Path.Domain.Min);
										s.SideBranches.Add(child);
										local_slice.Add(child);
									}
								}
							}
							else local_remain.Add(cu);
						}
						//end interection
					}
					global_remain = local_remain;
				}

				if (local_slice.Count == 0) break;
				else global_slice = local_slice;
			}

			return root;
		}

		List<Curve> Merge(ICollection<Curve> crown)
		{
			double mat = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
			var acc = new List<Curve>();

			foreach (Curve cu in crown)
			{
				using (var x = Intersection.CurveCurve(Path, cu, mat, mat))
				{
					if (x.Count > 0)
					{
						if (SideBranches == null) SideBranches = new List<Branch>();
						var ptm = x.OrderBy((e) => e.ParameterA).ElementAt(0);
						var ptb = ptm.ParameterB;
						
						if (Math.Abs(ptb - cu.Domain.Min) <= mat * 10 || Math.Abs(ptb - cu.Domain.Max) <= mat * 10)
						{
							var child = new Branch(cu, ptm.ParameterA - Path.Domain.Min);
							if (ptb == cu.Domain.Max)
							{
								child.Path.Reverse();
							}
							SideBranches.Add(child);
						}
						else
						{
							if (!cu.IsClosed)
							{
								var split = cu.Split(ptb);
								foreach (var sp in split)
								{
									if (sp.Domain.Max == ptb) sp.Reverse();
									SideBranches.Add(new Branch(sp, ptm.ParameterA - Path.Domain.Min));
								}
							}
							else
							{
								cu.ChangeClosedCurveSeam(ptm.ParameterB);
								SideBranches.Add(new Branch(cu, ptm.ParameterA - Path.Domain.Min));
							}
						}
					}
					else acc.Add(cu);
				}
			}

			if (SideBranches != null && acc.Count != 0)
			{
				foreach (var child in SideBranches)
				{
					acc = child.Merge(acc);
				}
			}

			return acc;
		}

		public Curve GlobalTrim(double param)
		{
			if (param >= MaxStencil) 
				return Path;
			
			if (param >= MinStencil) 
			{
				var min = Path.Domain.Min; 
				return Path.Trim(min, min + (param - MinStencil));
			}
			
			return null;
		}

		public Point GlobalPoint(double param)
		{
			if (param < MaxStencil && param > MinStencil)
			{
				var min = Path.Domain.Min;
				return new Point(Path.PointAt(min + (param - MinStencil)));
			}
			else return null;
		}
		
		public double Length()
		{
			if (SideBranches != null)
			{
				var accum = new double[SideBranches.Count + 1];
				accum[0] = MaxStencil;
				for (int n = 0; n < SideBranches.Count; n++)
				{
					accum[n+1] = SideBranches[n].Length();
				}
				return accum.Max();
			}
			else return MaxStencil;
		}
		
		public ICollection<Branch> Flatten(ICollection<Branch> accum = null)
		{
			if (accum == null) accum = new List<Branch>();
			if (SideBranches != null)
				foreach (var child in SideBranches) child.Flatten(accum);
			accum.Add(this);
			return accum;
		}	
	}
}
