using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Remote;
using Todo;

namespace TodoWinForms;

public partial class Form1 : Form
{
	private TodoClient? client;

	public Form1() {
        InitializeComponent();
	}

	private async void Form1_Load(object sender, EventArgs e)
	{
		await Init();
	}

	private async Task Init()
	{
		var hub = new WebSocketClientHub("main_db", "ws://localhost:8010/fliox/");
		client = new TodoClient(hub) { UserId = "admin", Token = "admin" };
		client.SetEventProcessor(new EventProcessorContext());

		var jobs = client.jobs.QueryAll();

		client.jobs.SubscribeChanges(Change.All, (changes, context) => {
			foreach (var upsert in changes.Upserts) {
				var job = upsert.entity;
				var item = listView1.Items["" + job.id];
				if (item != null) { 
					item.Text = job.title;
				} else {
					listView1.Items.Add("" + job.id, job.title, "");
				}
			}
			foreach (var id in changes.Deletes)			{
				listView1.Items.RemoveByKey("" + id);
			}
		});
		await client.SyncTasks();

		foreach (var job in jobs.Result)
		{
			listView1.Items.Add("" + job.id, job.title, "");
		}
	}
}