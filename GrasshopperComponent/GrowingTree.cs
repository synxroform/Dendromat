using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace Zeometry
{
	public class GrowingTree
	{
		static double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
		static double overlap = tolerance;

		BasicTree ownedTree;

		public GrowingTree(Curve root, List<Curve> crown)
		{
			ownedTree = new BasicTree(root);
            ownedTree.Node.Domain = new Interval(0.0, 1.0);
            ownedTree.Junction = 0;
			ownedTree.Derived = Traverse(ownedTree, crown);
		}

		public List<Curve> CollectCurvesAt(double param)
		{
			return CollectDataAt(param).Select((x) => x.Subcurve).ToList();
		}

		public List<Plane> CollectPlanesAt(double param)
		{
			return CollectDataAt(param).Select((x) => x.Frame).ToList();
		}

		public List<double> CollectParametersAt(double param)
		{
			return CollectDataAt(param).Select((x) => x.Parameter).ToList();
		}

		List<BunchOfData> CollectDataAt(double param)
		{
			List<BunchOfData> accum = new List<BunchOfData>();
			double length = GetLength(ownedTree);
			CollectDataAtRecursion(param * length, 0, accum, ownedTree);
			return accum;
		}

		void CollectDataAtRecursion(double param, double basis, List<BunchOfData> accum, BasicTree node)
		{
			double length = node.Node.Domain.Length;
			double bs = node.Junction + basis;
			double pr = Math.Min(Math.Max(param, bs), bs + length) - bs;
			if (node.Derived != null)
			{
				foreach (BasicTree bat in node.Derived)
				{
					CollectDataAtRecursion(param, node.Junction + basis, accum, bat); 
				}
			}
			if (pr > 0)
			{
				Plane plane = Plane.WorldXY;
				Curve subcurve = null;
				node.Node.PerpendicularFrameAt(Math.Min(Math.Max(0.0, pr), 1.0), out plane);
                subcurve = node.Node.Trim(0.0, pr);
                accum.Add(new BunchOfData(plane, subcurve, pr));
			}
		}

		static List<BasicTree> Traverse(BasicTree root, List<Curve> crown) // построение топологии.
		{
			List<BasicTree> derived = new List<BasicTree>();
			List<Curve> other = new List<Curve>();
            List<Curve> ccrown = new List<Curve>(crown);

			foreach (Curve c in ccrown)
			{
                CurveIntersections insect = Intersection.CurveCurve(root.Node, c, tolerance, overlap);
				if (insect.Count > 0)
				{
					BasicTree bat = new BasicTree(c);
                    var ordered = insect.OrderBy((i) => i.ParameterA);
                    var bjunctn = ordered.ElementAt(0).ParameterB;
                    if (bjunctn == c.Domain.Min || bjunctn == c.Domain.Max)
                    {
                        bat.Junction = ordered.ElementAt(0).ParameterA;
                        if (bjunctn == c.Domain.Max) bat.Node.Reverse();      
                        bat.Node.Domain = new Interval(0.0, 1.0);
                        derived.Add(bat);
                        continue;
                    }
                    if (!c.IsClosed) {
                        var cs = c.Split(bjunctn); 
                        var c1 = cs[0]; if (c1.Domain.Max == bjunctn) c1.Reverse(); c1.Domain = new Interval(0.0, 1.0);
                        derived.Add(new BasicTree(c1) { Junction = ordered.ElementAt(0).ParameterA });
                        var c2 = cs[1]; if (c2.Domain.Max == bjunctn) c2.Reverse(); c2.Domain = new Interval(0.0, 1.0);
                        derived.Add(new BasicTree(c2) { Junction = ordered.ElementAt(0).ParameterA });
                        continue;
                    }
                    // вариант для замкнутой кривой.
                    c.ChangeClosedCurveSeam(ordered.ElementAt(0).ParameterB);
                    bat.Junction = ordered.ElementAt(0).ParameterA;
                    bat.Node.Domain = new Interval(0.0, 1.0);
                    derived.Add(bat);
                    continue;
				}
				other.Add(c);
			}
			if (derived.Count > 0)
			{
				foreach (BasicTree bat in derived)
				{
					bat.Derived = Traverse(bat, other);
				}
				return derived;
			}

			return null;
		}

		static double GetLength(BasicTree tree) // длина максимального пути в дереве.
		{
			List<double> lengths = new List<double>();
			double this_length = tree.Node.Domain.Length;
			lengths.Add(this_length);
			if (tree.Derived != null) foreach (BasicTree t in tree.Derived) lengths.Add(GetLength(t));
			return lengths.Max() + tree.Junction;
		}
	}

	class BasicTree
	{
		public Curve Node; // собственно ветка.
		public List<BasicTree> Derived = null; // дочерние ветки.
		public double Junction; // место крепления дочерней ветки к родительской.
        		
		public BasicTree(Curve node)
		{
			this.Node = node;
		}
	}

	class BunchOfData // данные возвращаемые в главное приложение.
	{
		public Plane Frame;
		public Curve Subcurve;
		public double Parameter;

		public BunchOfData(Plane f, Curve sub, double parameter)
		{
			Frame = f;
			Subcurve = sub;
			Parameter = parameter;
		}
	}
}
