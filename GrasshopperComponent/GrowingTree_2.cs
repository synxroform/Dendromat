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
			ownedTree.Junction = 0;
			ownedTree.Derived = Traverse(root, crown);
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
				node.Node.PerpendicularFrameAt(pr, out plane);
				subcurve = node.Node.Trim(0.0, pr);
				accum.Add(new BunchOfData(plane, subcurve, pr));
			}
		}

		static List<BasicTree> Traverse(Curve root, List<Curve> crown) // построение топологии.
		{
			List<BasicTree> derived = new List<BasicTree>();
			List<Curve> other = new List<Curve>();

			foreach (Curve c in crown)
			{
				CurveIntersections insect = Intersection.CurveCurve(root, c, tolerance, overlap);
				if (insect.Count > 1)
				{
					BasicTree bat = new BasicTree(c);
					bat.Junction = insect[0].ParameterA; // change there
					derived.Add(bat);
					continue;
				}
				other.Add(c);
			}
			if (derived.Count > 0)
			{
				foreach (BasicTree bat in derived)
				{
					bat.Derived = Traverse(bat.Node, other);
				}
				return derived;
			}

			return null;
		}

		static double GetLength(BasicTree tree) // длина найбольшего пути в дереве.
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
		public Curve Node;			// собственно ветка.
		public List<BasicTree> Derived = null; 	// дочерние ветки.
		public double Junction; 		// место крепления ветки.
		
		public BasicTree(Curve node)
		{
			this.Node = node;
		}
	}

	class BunchOfData				// данные возвращаемые из алгоритма.
	{
		public Plane Frame;			// система координат растущего конца.
		public Curve Subcurve;			// растущая кривая.
		public double Parameter;		// текущий параметр на ветке.

		public BunchOfData(Plane f, Curve sub, double parameter)
		{
			Frame = f;
			Subcurve = sub;
			Parameter = parameter;
		}
	}
}
