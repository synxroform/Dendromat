using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

using Rhino.Geometry;
using Rhino.DocObjects;

namespace Dendromat
{
	//
	//
	//
	public enum OperationMode { Setup, RenderSetup, Render }

	//
	//
	//
	public abstract class ObservableObject : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propName)
		{
			Debug.Assert(GetType().GetProperty(propName) != null);
			var pc = PropertyChanged;
			if (pc != null) pc(this, new PropertyChangedEventArgs(propName));
		}

		protected bool SetProperty<T>(ref T field, T value, string propName) 
		{
			if (!EqualityComparer<T>.Default.Equals(field, value)) {
				field = value;
				OnPropertyChanged(propName);
				return true;
			}
			return false;
		}
	}

	//
	//
	//
	class DendromatLogic : ObservableObject
	{
		List<Guid> _output = new List<Guid>();
		RisingTree _tree;

		Boolean _dataLoaded = false;
		OperationMode _mode;
		Double _knobValue;

		public DendromatLogic() {}

		public void SetTree(Curve stem, List<Curve> crown)
		{
			_tree = new RisingTree(stem, crown);
		}

		public void ResetTree()
		{
			ClearOutput();
			_tree = null;
		}

		public Boolean DataLoaded
		{
			get { return _dataLoaded; }
			set { SetProperty(ref _dataLoaded, value, "DataLoaded"); }
		}

		public OperationMode Mode
		{
			get { return _mode; }
			set { SetProperty(ref _mode, value, "Mode"); }
		}

		public Double KnobValue
		{
			get {return _knobValue; }
			set { 
					SetProperty(ref _knobValue, value, "KnobValue");
					UpdateTreeValue(); 
				}
		}

		public void ClearOutput()
		{
			if (_output != null) { 
				foreach (Guid guid in _output) Rhino.RhinoDoc.ActiveDoc.Objects.Purge((new ObjRef(guid)).Object());
				_output.Clear();
			}
		}

		public void UpdateTreeValue()
		{
			if (_tree == null) return;
			ClearOutput();
			var curves = _tree.CollectCurvesAt(KnobValue);
			ObjectAttributes att = new ObjectAttributes();
			att.Visible = true;
			foreach (Curve c in curves) {
				_output.Add(Rhino.RhinoDoc.ActiveDoc.Objects.AddCurve(c, att));
			}
			Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
		}
	}
}
