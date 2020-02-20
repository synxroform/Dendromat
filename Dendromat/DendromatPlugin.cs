using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;

using System.Windows.Interop;

using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;

namespace Dendromat
{
	//
	//
	//
	public class SnapshotEventArgs : EventArgs
	{
		public DendromatSnapshot NewSnapshot;
		public SnapshotEventArgs(DendromatSnapshot new_snapshot) 
		{ 
			NewSnapshot = new_snapshot; 
		}
	}
	
	//
	//
	//
	public class DendromatSnapshot
	{
		Boolean _visible;
		ObjRef _stem;
		List<ObjRef> _crown;

		public ObjRef Stem
		{
			get { return _stem; }
		}

		public List<ObjRef> Crown
		{
			get { return _crown; }
		}

		public Boolean IsEmpty
		{
			get { return (_stem == null) || (_crown == null) || (_crown.Count == 0); }
		}
		
		public DendromatSnapshot(ObjRef stem, List<ObjRef> crown)
		{
			_visible = true;
			_stem = stem;
			_crown = crown;
		}

		public DendromatSnapshot Clear()
		{
			_visible = true;
			_stem = null;
			_crown = null;
			return this;	
		}

		public DendromatSnapshot SwapVisibility()
		{
			if (IsEmpty) return this;
			if (_visible) HideAll();
			else ShowAll();
			Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
			return this;
		}

		public DendromatSnapshot Lock()
		{
			if (IsEmpty) return this;
			Rhino.RhinoDoc.ActiveDoc.Objects.Lock(_stem, true);
			foreach (ObjRef obj in _crown) Rhino.RhinoDoc.ActiveDoc.Objects.Lock(obj, true);
			return this;
		}

		public DendromatSnapshot Unlock()
		{
			if (IsEmpty) return this;
			Rhino.RhinoDoc.ActiveDoc.Objects.Unlock(_stem, true);
			foreach (ObjRef obj in _crown) Rhino.RhinoDoc.ActiveDoc.Objects.Unlock(obj, true);
			return this;
		}

		private void HideAll()
		{
			if (!IsEmpty) {
				Rhino.RhinoDoc.ActiveDoc.Objects.Hide(_stem, true);
				foreach (ObjRef obj in _crown) Rhino.RhinoDoc.ActiveDoc.Objects.Hide(obj, true);
				_visible = false;
			}
		}

		private void ShowAll()
		{
			if (!IsEmpty) {
				Rhino.RhinoDoc.ActiveDoc.Objects.Show(_stem, true);
				foreach (ObjRef obj in _crown) Rhino.RhinoDoc.ActiveDoc.Objects.Show(obj, true);
				_visible = true;
			}
		}

		public void Write(RhinoDoc doc, Rhino.FileIO.BinaryArchiveWriter archive, Rhino.FileIO.FileWriteOptions options)
		{
			archive.Write3dmChunkVersion(1, 0);
			archive.WriteObjRef(_stem);
			archive.WriteObjRefArray(_crown);
		}

		public void Read(RhinoDoc doc, Rhino.FileIO.BinaryArchiveReader archive, Rhino.FileIO.FileReadOptions options)
		{
				int major, minor;
				archive.Read3dmChunkVersion(out major, out minor);
				if (major != 1 && minor != 0) return;
				_stem = archive.ReadObjRef();
				_crown = archive.ReadObjRefArray().ToList();
				_visible = true;
		}

		public bool Check(RhinoDoc doc) // проверка наличия необходимых объектов в документе.
		{
			bool value = doc.Objects.Find(_stem.ObjectId) != null;
			if (value) foreach (ObjRef c in _crown) {
					value = doc.Objects.Find(c.ObjectId) != null;
					if (!value) break;
			}
			if (!value) Rhino.RhinoApp.WriteLine("dendromat: cannot find some parts of the tree, please redefine them.");
			return value; 
		}

		public bool Contains(Guid id)
		{
			bool value = _stem.ObjectId == id;
			if (!value) value = _crown.Any((x) => {return x.ObjectId == id;});
			return value;
		}
	}

	//
	//
	//
	public class DendromatPlugin : Rhino.PlugIns.PlugIn
	{
		DendromatSnapshot _snapshot;
		public event EventHandler<SnapshotEventArgs> SnapshotUpdated;

		public DendromatSnapshot Snapshot
		{
			get { return _snapshot; }
		}

		public DendromatPlugin() 
		{
			_snapshot = new DendromatSnapshot(null, null);
			Instance = this;
		}

		public static DendromatPlugin Instance 
		{
			get; private set;
		}

		protected override Rhino.PlugIns.LoadReturnCode OnLoad(ref string errorMessage)
		{
			Rhino.RhinoApp.WriteLine("dendromat: loading plugin");
			Rhino.RhinoDoc.NewDocument += new EventHandler<DocumentEventArgs>(RhinoDoc_NewDocument);
			Rhino.RhinoDoc.DeleteRhinoObject += RhinoDoc_DeleteRhinoObject;
			Rhino.RhinoDoc.BeginOpenDocument += RhinoDoc_BeginOpenDocument;
			return Rhino.PlugIns.LoadReturnCode.Success;
		}

		void RhinoDoc_DeleteRhinoObject(object sender, RhinoObjectEventArgs e)
		{
			// если пользователь удаляет объект на который ссылается дендромат, то необходимо
			// осуществить сброс внетренних структур.
			if (_snapshot.Contains(e.ObjectId)) {
				_snapshot.Clear();
				if(SnapshotUpdated != null) SnapshotUpdated(this, new SnapshotEventArgs(_snapshot));
				Rhino.RhinoApp.WriteLine("dendromat: part of the tree was removed, select new part.");
			}
		}

		public void SnapshotDendromat(ObjRef stem, List<ObjRef> crown)
		{
			_snapshot = new DendromatSnapshot(stem, crown);
		}

		protected override void WriteDocument(RhinoDoc doc, Rhino.FileIO.BinaryArchiveWriter archive, Rhino.FileIO.FileWriteOptions options)
		{
			if (!_snapshot.IsEmpty && !options.WriteSelectedObjectsOnly) _snapshot.Write(doc, archive, options);
		}

		protected override void ReadDocument(RhinoDoc doc, Rhino.FileIO.BinaryArchiveReader archive, Rhino.FileIO.FileReadOptions options)
		{
			if (options.OpenMode || options.NewMode) {
				_snapshot.Read(doc, archive, options);
				if (!_snapshot.IsEmpty && SnapshotUpdated != null) SnapshotUpdated(this, new SnapshotEventArgs(_snapshot));
			}
		}

		protected override bool ShouldCallWriteDocument(Rhino.FileIO.FileWriteOptions options)
		{
			return !_snapshot.IsEmpty;
		}
		
		void RhinoDoc_BeginOpenDocument(object sender, DocumentOpenEventArgs e)
		{
			if (!e.Merge) _snapshot.Clear();
		}

		void RhinoDoc_NewDocument(object sender, DocumentEventArgs e)
		{
			_snapshot.Clear();
		}

	}

	//
	//
	//
	[System.Runtime.InteropServices.Guid("90A701B2-7CE4-42F6-A233-A66AAC7DD57C")]
	public class DendromatCommand : Command
	{
		MainWindow _panel;
		public DendromatPlugin ParentPlugin; 
		public int Instances = 0;

		public override string EnglishName
		{
			get { return "Dendromat"; }
		}

		public DendromatCommand()
		{
			ParentPlugin = (DendromatPlugin)PlugIn;
		}

		protected override Result RunCommand(RhinoDoc doc, RunMode mode)
		{
			if (mode == RunMode.Scripted) return Result.Nothing;
			if (Instances > 0) {
				RhinoApp.WriteLine("dendromat: only one instance of plugin is allowed.");
				return Result.Nothing;
			}
			if (_panel != null) _panel.Show();
			else {

				_panel = new MainWindow(this);
				new WindowInteropHelper(_panel).Owner = Rhino.RhinoApp.MainWindowHandle();
				_panel.Show();
			}
			
			return Result.Nothing;
		}
	}
}
