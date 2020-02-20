using System;
using System.Linq;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.DocObjects;

namespace Dendromat
{
	//
	//
	//
	public class DmatCommands 
	{
		private static RoutedUICommand _switchMode;
		private static RoutedUICommand _exitPlugin; 
		private static RoutedUICommand _aboutPlugin;
		private static RoutedUICommand _loadData;
		private static RoutedUICommand _dragMove;
	
		public static RoutedUICommand SwitchMode { get { return _switchMode; }}
		public static RoutedUICommand ExitPlugin { get { return _exitPlugin; }}
		public static RoutedUICommand AboutPlugin { get { return _aboutPlugin; }}
		public static RoutedUICommand LoadData { get { return _loadData; }}
		public static RoutedUICommand DragMove { get { return _dragMove; }}

		static DmatCommands()
		{
			_switchMode = new RoutedUICommand("switch mode", "SwitchMode", typeof(DmatCommands));
			_exitPlugin = new RoutedUICommand("exit", "ExitPlugin", typeof(DmatCommands));
			_aboutPlugin = new RoutedUICommand("about", "AboutPlugin", typeof(DmatCommands));
			_loadData = new RoutedUICommand("load", "LoadData", typeof(DmatCommands));
			_dragMove = new RoutedUICommand("move", "DragMove", typeof(DmatCommands));
		}
	}

	//
	//
	//
	public class GetObjectFilter {
		
	}

	//
	//
	//
	public partial class MainWindow : Window
	{
		DendromatLogic _logic;
		DendromatCommand _command;
		Boolean _aboutMode = false;


		public MainWindow(DendromatCommand command)
		{
			Rhino.RhinoDoc.BeginSaveDocument += RhinoDoc_BeginSaveDocument;
			Rhino.RhinoDoc.EndSaveDocument += RhinoDoc_EndSaveDocument;
			Rhino.RhinoDoc.CloseDocument += RhinoDoc_CloseDocument;
			

			_logic = new DendromatLogic();
			_command = command;
			_command.ParentPlugin.SnapshotUpdated += ParentPlugin_SnapshotUpdated;
			_command.Instances += 1;
			if (!_command.ParentPlugin.Snapshot.IsEmpty)
				if (_command.ParentPlugin.Snapshot.Check(Rhino.RhinoDoc.ActiveDoc))
					ParentPlugin_SnapshotUpdated(this, new SnapshotEventArgs(_command.ParentPlugin.Snapshot));
				else _command.ParentPlugin.Snapshot.Clear();

			DataContext = _logic;
			InitializeComponent();
		}

		void ParentPlugin_SnapshotUpdated(object sender, SnapshotEventArgs e)
		{
			if (e.NewSnapshot.IsEmpty) {
				_logic.ResetTree();
				_logic.DataLoaded = false;
				return;
			}
			_logic.SetTree(e.NewSnapshot.Stem.Curve(), e.NewSnapshot.Crown.Select((ObjRef obj) => { return obj.Curve(); }).ToList());
			if (IsVisible) {
				_command.ParentPlugin.Snapshot.SwapVisibility();
				_logic.UpdateTreeValue();
			}
			_logic.DataLoaded = true;
		}

		private void RhinoDoc_EndSaveDocument(object sender, Rhino.DocumentSaveEventArgs e)
		{
			if (!IsVisible) return;
			_command.ParentPlugin.Snapshot.SwapVisibility();
			_logic.UpdateTreeValue();
		}

		private void RhinoDoc_BeginSaveDocument(object sender, Rhino.DocumentSaveEventArgs e)
		{
			if (!IsVisible) return;
			_logic.ClearOutput();
			_command.ParentPlugin.Snapshot.SwapVisibility();
		}

		void RhinoDoc_CloseDocument(object sender, Rhino.DocumentEventArgs e)
		{
			_logic.ClearOutput();
			_command.ParentPlugin.Snapshot.Clear();
		}

		private void ExitPlugin_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_command.Instances -= 1;
			_logic.ClearOutput();
			_command.ParentPlugin.Snapshot.SwapVisibility();
			Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
			this.Hide();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			e.Cancel = true;
			ExitPlugin_Executed(this, null);
		}

		private void DragMove_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			this.DragMove();
		}

		private void LoadData_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_logic.ClearOutput();
			if (!_command.ParentPlugin.Snapshot.IsEmpty) {
				_command.ParentPlugin.Snapshot.SwapVisibility().Unlock();
			}
			Rhino.RhinoDoc.ActiveDoc.Views.Redraw();

			GetObject go = new GetObject();
			go.GeometryFilter = ObjectType.Curve;
			go.SetCommandPrompt("Choose stem ");
			go.AcceptNothing(false);
			go.Get();
				
			if (go.CommandResult() != Result.Success) return;
			ObjRef stemRef = go.Object(0);
			Rhino.RhinoDoc.ActiveDoc.Objects.Lock(stemRef, true);

			//go.AlreadySelectedObjectSelect = false;
			//go.EnablePreSelect(false, true);
			go.SetCommandPrompt("Choose crown ");
			go.GetMultiple(1, 0);
			
			if (go.CommandResult() != Result.Success) return;
			ObjRef[] crownRef = go.Objects();
			foreach (var cref in crownRef) Rhino.RhinoDoc.ActiveDoc.Objects.Lock(cref, true);
			
			_command.ParentPlugin.SnapshotDendromat(stemRef, crownRef.ToList());
			_command.ParentPlugin.Snapshot.Unlock().SwapVisibility().Lock();
			_logic.SetTree(stemRef.Curve(), crownRef.Select((ObjRef obj) => { return obj.Curve(); }).ToList());
			_logic.UpdateTreeValue();
			_logic.DataLoaded = true;
		}

		private void AboutPlugin_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			switch (_logic.Mode) {
				case OperationMode.Setup:
						if (_aboutMode) AboutSetup.BeginAnimation(Canvas.OpacityProperty, (DoubleAnimation)FindResource("MakeTransparent"));
						else AboutSetup.BeginAnimation(Canvas.OpacityProperty, (DoubleAnimation)FindResource("MakeOpaque"));
						_aboutMode = !_aboutMode;
						AboutSetup.IsHitTestVisible = _aboutMode;					
					break;

				case OperationMode.Render:
					break;
			}
		}

		private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue) {
				_logic.UpdateTreeValue();
				_command.ParentPlugin.Snapshot.SwapVisibility();
			}
		}
	}
}
