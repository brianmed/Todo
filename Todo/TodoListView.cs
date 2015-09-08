using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using TelerikUI;

using nsoftware.IPWorks;

using GCDiscreetNotification;

using CoreGraphics;
using Foundation;
using UIKit;

using System.Threading;

namespace Todo
{
	public class TodoItem: NSObject
	{
		[Export("Title")]
		public string Title { get; set; }

		[Export("Content")]
		public string Content { get; set; }

		[Export("TodoId")]
		public int TodoId { get; set; }
	}

	public class TodoListView : UIViewController
	{
		public TKListView listView = new TKListView();
		public TKDataSource dataSource = new TKDataSource (); 
		public static TodoListView _kludge = null;
		public GCDiscreetNotificationView notificationView;
		public static Object POSTLock = new Object();

		public TodoListView() 
		{
			this.Title = "Todo";

			this.View.BackgroundColor = UIColor.White;

			_kludge = this;
		}

		~TodoListView() 
		{
			_kludge = null;
		}

		async void DeleteButton(object sender, EventArgs e)
		{
			this.listView.EndSwipe (true);

			await POSTDeleteTodo((NSNumber)ListViewDelegate.lastOne.ValueForKey(new NSString("todo_id")));
		}
			
		public async override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			this.dataSource = new TKDataSource ();

			await this.LoadTodo ();

			var bounds = this.View.Bounds;
			bounds.Height -= this.NavigationController.NavigationBar.Bounds.Height;
			bounds.Height -= UIApplication.SharedApplication.StatusBarFrame.Height;
			bounds.Y += this.NavigationController.NavigationBar.Bounds.Height;
			bounds.Y += UIApplication.SharedApplication.StatusBarFrame.Height;

			this.listView.Frame = bounds;
			this.listView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			this.listView.WeakDataSource = this.dataSource;
			this.listView.AllowsCellSwipe = true;
			this.listView.CellSwipeLimits = new UIEdgeInsets (0, 60, 0, 60);
			this.listView.CellSwipeTreshold = 30;
			this.listView.Delegate = new ListViewDelegate (this);

			this.dataSource.Settings.ListView.CreateCell(delegate (TKListView listView, NSIndexPath indexPath, NSObject item) {
				TKListViewCell cell = listView.DequeueReusableCell("defaultCell", indexPath) as TKListViewCell;
				if (cell.SwipeBackgroundView.Subviews.Length == 0) 
				{
					var size = cell.Frame.Size;
					var rDelete = new UIButton (new CGRect (size.Width - 60, 0, 60, size.Height));
					rDelete.SetTitle ("Delete", UIControlState.Normal);
					rDelete.BackgroundColor = UIColor.Red;
					rDelete.AddTarget(DeleteButton, UIControlEvent.TouchUpInside);
					cell.SwipeBackgroundView.AddSubview (rDelete);

					var lDelete = new UIButton (new CGRect (0, 0, 60, size.Height));
					lDelete.SetTitle ("Delete", UIControlState.Normal);
					lDelete.BackgroundColor = UIColor.Red;
					lDelete.AddTarget(DeleteButton, UIControlEvent.TouchUpInside);
					cell.SwipeBackgroundView.AddSubview (lDelete);

				}
				return cell;
			});
				
			this.dataSource.Settings.ListView.InitCell (delegate (TKListView listView, NSIndexPath indexPath, TKListViewCell cell, NSObject item) {
				var dict = (NSMutableDictionary)item;
				cell.TextLabel.Text = (NSString) dict.ValueForKey(new NSString("title")); // todoItem.Title;
				cell.DetailTextLabel.Text = (NSString) dict.ValueForKey(new NSString("content"));
				cell.ContentInsets = new UIEdgeInsets(1,10,1,10);
			});

			var addItem = new UIBarButtonItem("Action", UIBarButtonItemStyle.Plain,
				(s, e) => {
					UIAlertController actionSheetAlert = UIAlertController.Create("Actions", "Select an action", UIAlertControllerStyle.ActionSheet);

					// Add Actions
					actionSheetAlert.AddAction(UIAlertAction.Create("Add Todo", UIAlertActionStyle.Default, (action) => {
						var dataForm = new TodoDataEntry();
						this.NavigationController.PushViewController(dataForm, true);
					}));

					actionSheetAlert.AddAction(UIAlertAction.Create("Logout",UIAlertActionStyle.Default, (UIAlertAction obj) => {
						Globals.theSettings = null;
						Globals.SQLite.Execute("DELETE FROM Settings");

						var appDelegate = UIApplication.SharedApplication.Delegate as AppDelegate;
						appDelegate.navigationController.PopToRootViewController(true);
						appDelegate.navigationController.ViewControllers = new List<UIViewController> { new LoginView() }.ToArray();
					}));

					actionSheetAlert.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, (action) => Application.Debug ("Cancel button pressed.")));

					// Required for iPad - You must specify a source for the Action Sheet since it is
					// displayed as a popover
					UIPopoverPresentationController presentationPopover = actionSheetAlert.PopoverPresentationController;
					if (presentationPopover != null) {
						presentationPopover.SourceView = this.View;
						presentationPopover.PermittedArrowDirections = UIPopoverArrowDirection.Up;
					}

					// Display the alert
					this.PresentViewController(actionSheetAlert,true,null);
				}
			);

			this.NavigationItem.SetRightBarButtonItems(new UIBarButtonItem[] { addItem }, true);

			this.View.AddSubview (this.listView);
		}


		public async Task LoadTodo ()
		{
			var notificationView = new GCDiscreetNotificationView (
				text: "Loading todos",
				activity: false,
				presentationMode: GCDNPresentationMode.Bottom,
				view: this.View
			);

			notificationView.SetShowActivity(true, true);
			notificationView.Show (true);

			try {
				var json = new Json ();

				var data = new Dictionary<string, string> ();

				data ["api_key"] = Globals.theSettings.API_Key;
				data ["username"] = Globals.theSettings.username;

				json.PostData = Newtonsoft.Json.JsonConvert.SerializeObject (data);

				await json.PostAsync (String.Concat (Globals.EndpointBase, "/api/v1/account/get_todos"));

				this.dataSource.LoadDataFromJSONString(json.TransferredData, "todos");
			}
			catch (Exception ex) {
				Application.Debug (ex.Message);
			}

			notificationView.Hide(true);
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

		public async Task POSTDeleteTodo (NSNumber todo_id)
		{
			if (false == Monitor.TryEnter(TodoListView.POSTLock)) {
				return;
			}

			var json = new Json();

			var data = new Dictionary<string, string>();

			data["todo_id"] = todo_id.ToString();

			data["api_key"] = Globals.theSettings.API_Key;
			data["username"] = Globals.theSettings.username;

			var appDelegate = UIApplication.SharedApplication.Delegate as AppDelegate;

			var notificationView = new GCDiscreetNotificationView (
				text: "Deleting todo",
				activity: false,
				presentationMode: GCDNPresentationMode.Bottom,
				view: appDelegate.navigationController.View
			);

			notificationView.SetShowActivity(true, true);
			notificationView.Show (true);

			json.ContentType = "application/json";

			try {
				json.PostData = Newtonsoft.Json.JsonConvert.SerializeObject (data);

				var endpoint = String.Concat(Globals.EndpointBase, "/api/v1/account/del_todo");

				await json.PostAsync(endpoint);

				json.XPath = "/json/status";
				if ("\"success\"" == json.XText) {
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

				DisplayAlert ("Alert", "Unable to delete the todo", "OK");
			}

			notificationView.Hide (true);

			Monitor.Exit(TodoListView.POSTLock);
		}
	}

	class ListViewDelegate: TKListViewDelegate
	{
		TodoListView owner;
		public static int lastIndex = -1;
		public static NSMutableDictionary lastOne = null;

		public ListViewDelegate(TodoListView owner)
		{
			this.owner = owner;
		}

		public override void DidSwipeCell (TKListView listView, TKListViewCell cell, NSIndexPath indexPath, CGPoint offset)
		{
			ListViewDelegate.lastIndex = indexPath.Row;
			ListViewDelegate.lastOne = (NSMutableDictionary) owner.dataSource.Items [indexPath.Row];
		}

		public override void DidFinishSwipeCell (TKListView listView, TKListViewCell cell, NSIndexPath indexPath, CGPoint offset)
		{
			ListViewDelegate.lastIndex = indexPath.Row;
			ListViewDelegate.lastOne = (NSMutableDictionary) owner.dataSource.Items [indexPath.Row];
		}
	}
}
