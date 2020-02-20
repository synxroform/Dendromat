using System.Windows.Controls;
using System.Windows.Input;

namespace Dendromat
{
	/// <summary>
	/// Логика взаимодействия для HeaderControl.xaml
	/// </summary>
	public partial class HeaderControl : UserControl
	{
		public HeaderControl()
		{
			InitializeComponent();
		}

		private void Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			DmatCommands.AboutPlugin.Execute(null, this);
		}

		private void Close_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			DmatCommands.ExitPlugin.Execute(null, this);
		}

		private void Titlebar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			DmatCommands.DragMove.Execute(null, this);
		}

	}
}
