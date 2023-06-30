using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Remote;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Todo;

namespace TodoWPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private TodoClient? client;

		public MainWindow()
		{
			InitializeComponent();
		}

		private async Task Init()
		{
			var hub = new WebSocketClientHub("main_db", "ws://localhost:5000/fliox/");
			client = new TodoClient(hub) { UserId = "admin", Token = "admin" };
			client.SetEventProcessor(new EventProcessorContext());

			var jobs = client.jobs.QueryAll();

			client.jobs.SubscribeChanges(Change.All, (changes, context) => {
				foreach (var upsert in changes.Upserts)
				{
					var job = upsert.entity;
					var item = listBox.Items.IndexOf("" + job.id);
					if (item != -1)					{
						listBox.Items[item] = "" + job.id;
					} else {
						listBox.Items.Add("" + job.id);
					}
				}
				foreach (var id in changes.Deletes)
				{
					listBox.Items.Remove("" + id);
				}
			});
			await client.SyncTasks();

			foreach (var job in jobs.Result)
			{
				listBox.Items.Add("" + job.id);
			}
		}

		private async void Window_Loaded(object sender, RoutedEventArgs e)
		{
			await Init();
		}

		private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{			
			ProcessStartInfo sInfo = new ProcessStartInfo("http://localhost:5000/fliox/") { UseShellExecute = true };
			System.Diagnostics.Process.Start(sInfo);
		}
	}
}
