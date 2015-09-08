using System;

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Foundation;
using TelerikUI;
using UIKit;

using ObjCRuntime;

using Newtonsoft.Json;
using nsoftware.IPWorks;

using System.Threading;

using GCDiscreetNotification;

namespace Todo
{
	public class TodoDataEntry : TKDataFormViewController
	{
		TKDataFormEntityDataSource dataSource;
		public static Object POSTLock = new Object();

		public override void ViewDidLoad()
		{
			base.ViewDidLoad ();

			dataSource = new TKDataFormEntityDataSource ();
			dataSource.SelectedObject = new TodoDataInfo ();
			dataSource.AllowPropertySorting = true;

			dataSource.EntityModel.PropertyWithName ("Title").PropertyIndex = 1;
			dataSource.EntityModel.PropertyWithName ("Content").PropertyIndex = 2;
			dataSource.EntityModel.PropertyWithName ("Submit").PropertyIndex = 3;

			this.DataForm.RegisterEditor (new Class (typeof(ActionEditor)), this.dataSource.EntityModel.PropertyWithName ("Submit"));

			var currentDelegate = new TodoDataEntryFormDelegate ();

			this.DataForm.Delegate = currentDelegate;
			this.DataForm.DataSource = dataSource;

			this.DataForm.CommitMode = TKDataFormCommitMode.Immediate;

			this.Title = "Add Entry";
		}
	}

	public class TodoDataInfo : NSObject
	{
		[Export("Title")]
		public string Title { get; set;}

		[Export("Content")]
		public string Content { get; set;}
			
		[Export("Submit")]
		public string Submit { get; set;}

		public TodoDataInfo ()
		{
			this.Title = "";
			this.Content = "";
			this.Submit = "";
		}
	}

	public class TodoDataEntryFormDelegate : TKDataFormDelegate
	{
		bool submitAdded;

		async void Submit(TKDataForm dataForm)
		{
			await POSTTodoEntry (dataForm);
		}

		public void DisplayAlert(string title, string msg, string btn = "OK")
		{
			TKAlert alert = new TKAlert();

			alert.Style.CornerRadius = 3;
			alert.Title = title;
			alert.Message = msg;

			alert.AddActionWithTitle (btn, (TKAlert a, TKAlertAction action) => { return true; });

			alert.Show(true);
		}			

		public override void DidCommitProperty (TKDataForm dataForm, TKDataFormEntityProperty property)
		{
			Application.Debug (property.Name);
		}

		public override void UpdateEditor (TKDataForm dataForm, TKDataFormEditor editor, TKDataFormEntityProperty property)
		{
			if ("Title" == property.Name) {
				((UITextField)editor.Editor).Placeholder = "Required";
			}

			if ("Content" == property.Name) {
				((UITextField)editor.Editor).Placeholder = "Required";
			}

			if ("Submit" == property.Name) {				
				editor.Style.TextLabelDisplayMode = TKDataFormEditorTextLabelDisplayMode.Hidden;
				editor.Style.EditorOffset = new UIOffset (35, 0);

				((ActionEditor)editor).ActionButton.SetTitle (property.DisplayName, UIControlState.Normal);
				if (!submitAdded) {
					((ActionEditor)editor).ActionButton.TouchUpInside += (o, s) => {
						Submit(dataForm);
					};

					submitAdded = true;
				}
			}
		}

		public async Task POSTTodoEntry (TKDataForm dataForm)
		{
			if (false == Monitor.TryEnter(TodoDataEntry.POSTLock)) {
				return;
			}

			var json = new Json();

			var data = new Dictionary<string, string>();

			data["title"] = ((NSString)((TKDataFormEntityDataSource)dataForm.DataSource).EntityModel.PropertyWithName ("Title").Value).ToString();
			data["content"] = ((NSString)((TKDataFormEntityDataSource)dataForm.DataSource).EntityModel.PropertyWithName ("Content").Value).ToString();

			data["api_key"] = Globals.theSettings.API_Key;
			data["username"] = Globals.theSettings.username;

			if (String.IsNullOrWhiteSpace(data["title"])) {				
				DisplayAlert ("Alert", "Title required", "OK");
				Monitor.Exit (TodoDataEntry.POSTLock);
				return;
			}

			if (String.IsNullOrWhiteSpace(data["content"])) {				
				DisplayAlert ("Alert", "Content required", "OK");
				Monitor.Exit (TodoDataEntry.POSTLock);
				return;
			}

			var appDelegate = UIApplication.SharedApplication.Delegate as AppDelegate;

			var notificationView = new GCDiscreetNotificationView (
				text: "Saving todo",
				activity: false,
				presentationMode: GCDNPresentationMode.Bottom,
				view: appDelegate.navigationController.View
			);

			notificationView.SetShowActivity(true, true);
			notificationView.Show (true);

			json.ContentType = "application/json";

			try {
				json.PostData = Newtonsoft.Json.JsonConvert.SerializeObject (data);

				var endpoint = String.Concat(Globals.EndpointBase, "/api/v1/account/add_todo");

				await json.PostAsync(endpoint);

				json.XPath = "/json/status";
				if ("\"success\"" == json.XText) {
					appDelegate.navigationController.PopViewController(true);

					await TodoListView._kludge.LoadTodo();
					TodoListView._kludge.dataSource.ReloadData();
					TodoListView._kludge.listView.ReloadData();
				}
				else {
					json.XPath = "/json/data/message";
					var newStr = json.XText.Replace("\"", "");
					DisplayAlert("Alert", newStr, "OK");
				}
			}
			catch (Exception e) {
				Application.Debug(e.Message);

				DisplayAlert ("Alert", "Unable to POST the todo", "OK");
			}

			notificationView.Hide (true);

			Monitor.Exit(TodoDataEntry.POSTLock);
		}
	}
}