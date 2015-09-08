
using System;

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using TelerikUI;

using Foundation;
using ObjCRuntime;
using UIKit;

using Newtonsoft.Json;
using nsoftware.IPWorks;

using System.Threading;

using GCDiscreetNotification;

namespace Todo
{
	public class LoginView : TKDataFormViewController
	{
		public static Object POSTLock = new Object();

		public LoginView ()
		{
			this.Title = "Login";
		}

		TKDataFormEntityDataSource dataSource;

		public override void ViewDidLoad()
		{
			base.ViewDidLoad ();

			this.dataSource = new TKDataFormEntityDataSource ();
			this.dataSource.SelectedObject = new LoginDetails();
			this.dataSource.AllowPropertySorting = true;

			dataSource.EntityModel.PropertyWithName ("Mode").GroupKey = " ";
			dataSource.EntityModel.PropertyWithName ("Mode").PropertyIndex = 0;

			dataSource.EntityModel.PropertyWithName ("Username").GroupKey = "Login";
			dataSource.EntityModel.PropertyWithName ("Password").GroupKey = "Login";

			dataSource.EntityModel.PropertyWithName ("Username").PropertyIndex = 1;
			dataSource.EntityModel.PropertyWithName ("Password").PropertyIndex = 2;

			dataSource.EntityModel.PropertyWithName ("Verify").GroupKey = "Register";
			dataSource.EntityModel.PropertyWithName ("Question").GroupKey = "Register";
			dataSource.EntityModel.PropertyWithName ("Answer").GroupKey = "Register";

			dataSource.EntityModel.PropertyWithName ("Verify").PropertyIndex = 4;
			dataSource.EntityModel.PropertyWithName ("Question").PropertyIndex = 5;
			dataSource.EntityModel.PropertyWithName ("Answer").PropertyIndex = 6;

			dataSource.EntityModel.PropertyWithName ("Submit").PropertyIndex = 7;

			this.DataForm.RegisterEditor (new Class (typeof(ActionEditor)), this.dataSource.EntityModel.PropertyWithName ("Submit"));
			this.DataForm.RegisterEditor (new Class (typeof(TKDataFormSegmentedEditor)), this.dataSource.EntityModel.PropertyWithName ("Mode"));

			var currentDelegate = new LoginViewDelegate ();
			this.DataForm.Delegate = currentDelegate;
			this.DataForm.DataSource = this.dataSource;
			this.DataForm.CommitMode = TKDataFormCommitMode.Immediate;

			this.Title = "Login";
		}
	}

	public class LoginDetails : NSObject
	{
		[Export("Mode")]
		public int Mode { get; set;}

		[Export("Username")]
		public string Username { get; set;}

		[Export("Password")]
		public string Password { get; set;}

		[Export("Verify")]
		public string Verify { get; set;}

		[Export("Question")]
		public string Question { get; set;}

		[Export("Answer")]
		public string Answer { get; set;}

		[Export("Submit")]
		public string Submit { get; set;}

		public LoginDetails ()
		{
			this.Username = "";
			this.Password = "";
			this.Mode = 0;
			this.Verify = "";
			this.Question = "";
			this.Answer = "";
			this.Submit = "";
		}
	}

	public class LoginViewDelegate : TKDataFormDelegate
	{
		NSNumber _state = null;
		bool submitAdded;

		async void Submit(TKDataForm dataForm)
		{
			var mode = ((TKDataFormEntityDataSource)dataForm.DataSource).EntityModel.PropertyWithName ("Mode");
			NSNumber value = (NSNumber)mode.Value;

			if (0 == value.Int32Value) {
				await POSTRequest ("signin", dataForm);
			} else {
				await POSTRequest ("register", dataForm);
			}
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

		public bool isLoginMode(TKDataForm dataForm)
		{
			var mode = ((TKDataFormEntityDataSource)dataForm.DataSource).EntityModel.PropertyWithName ("Mode");

			NSNumber value = (NSNumber)mode.Value;
			if (null != value) {
				return 0 == value.Int32Value ? true : false;
			}

			return false;
		}

		public override void UpdateEditor (TKDataForm dataForm, TKDataFormEditor editor, TKDataFormEntityProperty property)
		{
			if ("Username" == property.Name) {
				((UITextField)editor.Editor).Placeholder = "Required";
			}

			if ("Password" == property.Name) {
				((UITextField)editor.Editor).Placeholder = "Required";
				((UITextField)editor.Editor).SecureTextEntry = true;
			}

			if ("Mode" == property.Name) {
				editor.Style.TextLabelDisplayMode = TKDataFormEditorTextLabelDisplayMode.Hidden;
				editor.Style.EditorOffset = new UIOffset (25, 0);

				((TKDataFormSegmentedEditor)editor).Segments = new NSString[] { (NSString)"Login", (NSString)"Register" };
				UISegmentedControl segmentedControl = (UISegmentedControl)editor.Editor;

				NSNumber value = (NSNumber)property.Value;
				if (null != value) {
					// Need a better way
					if (null == _state) {
						_state = value;

						dataForm.ReloadData ();
					} else if (_state != value) {
						_state = value;

						dataForm.ReloadData ();
					}
				}
			}

			if ("Verify" == property.Name) {
				((UITextField)editor.Editor).SecureTextEntry = true;
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

		public async Task POSTRequest(string action, TKDataForm dataForm)
		{
			if (false == Monitor.TryEnter(LoginView.POSTLock)) {
				return;
			}

			var json = new Json();

			var inUsername = (NSString)((TKDataFormEntityDataSource)dataForm.DataSource).EntityModel.PropertyWithName ("Username").Value.ToString();
			var inPassword = (NSString)((TKDataFormEntityDataSource)dataForm.DataSource).EntityModel.PropertyWithName ("Password").Value.ToString();
			var inVerify = (NSString)((TKDataFormEntityDataSource)dataForm.DataSource).EntityModel.PropertyWithName ("Verify").Value.ToString();
			var mode = ((NSNumber)((TKDataFormEntityDataSource)dataForm.DataSource).EntityModel.PropertyWithName ("Mode").Value).Int32Value;
			var inQuestion = (NSString)((TKDataFormEntityDataSource)dataForm.DataSource).EntityModel.PropertyWithName ("Question").Value.ToString();
			var inAnswer = (NSString)((TKDataFormEntityDataSource)dataForm.DataSource).EntityModel.PropertyWithName ("Answer").Value.ToString();

			var isRegister = 1 == mode ? true : false;

			if (String.IsNullOrWhiteSpace(inUsername)) {
				DisplayAlert ("Alert", "Username required", "OK");
				Monitor.Exit(LoginView.POSTLock);
				return;
			}

			if (String.IsNullOrWhiteSpace(inPassword)) {
				DisplayAlert ("Alert", "Password required", "OK");
				Monitor.Exit(LoginView.POSTLock);
				return;
			}

			if (isRegister) {
				if (String.IsNullOrWhiteSpace(inVerify)) {
					DisplayAlert ("Alert", "Verify required", "OK");
					Monitor.Exit(LoginView.POSTLock);
					return;
				}

				if (inVerify != inPassword) {
					DisplayAlert ("Alert", "Passwords much match", "OK");
					Monitor.Exit(LoginView.POSTLock);
					return;
				}

				if (null == inQuestion) {
					DisplayAlert ("Alert", "Please select a security question", "OK");
					Monitor.Exit(LoginView.POSTLock);
					return;
				}

				if (String.IsNullOrWhiteSpace(inQuestion)) {
					DisplayAlert ("Alert", "Please select a security question", "OK");
					Monitor.Exit(LoginView.POSTLock);
					return;
				}

				if (String.IsNullOrWhiteSpace (inAnswer)) {
					DisplayAlert ("Alert", "Security answer required", "OK");
					Monitor.Exit(LoginView.POSTLock);
					return;
				}
			}				

			var appDelegate = UIApplication.SharedApplication.Delegate as AppDelegate;

			var notificationView = new GCDiscreetNotificationView (
				text: isRegister ? "Signing up" : "Logging in",
				activity: false,
				presentationMode: GCDNPresentationMode.Bottom,
				view: appDelegate.navigationController.View
			);

			notificationView.SetShowActivity(true, true);
			notificationView.Show (true);

			json.ContentType = "application/json";
			var data = new Dictionary<string, string>();

			data ["username"] = inUsername;
			data ["password"] = inPassword;

			if (isRegister) {
				data ["question"] = inQuestion;
				data ["answer"] = inAnswer;
			}

			try {
				json.PostData = Newtonsoft.Json.JsonConvert.SerializeObject (data);

				var endpoints = new Dictionary<string, string>();
				endpoints.Add("signin", String.Concat(Globals.EndpointBase, "/api/v1/account/signin"));
				endpoints.Add("register", String.Concat(Globals.EndpointBase, "/api/v1/account/register"));
				var endpoint = endpoints[action];

				await json.PostAsync(endpoint);

				json.XPath = "/json/status";
				if ("\"success\"" == json.XText) {
					json.XPath = "/json/data/api_key";

					var s = new Settings { 
						API_Key = json.XText.Replace("\"", ""),
						username = inUsername,
					};
					Globals.SQLite.Insert (s);

					var settings = Globals.SQLite.Table<Settings>().Where (v => v.API_Key != null);
					Globals.theSettings = settings.First();

					appDelegate.navigationController.PopToRootViewController(true);
					appDelegate.navigationController.ViewControllers = new List<UIViewController> { new TodoListView() }.ToArray();
				}
				else {
					json.XPath = "/json/data/message";
					var newStr = json.XText.Replace("\"", "");
					DisplayAlert("Alert", newStr, "OK");
				}
			}
			catch (Exception e) {
				var msgs = new Dictionary<string, string>();
				msgs.Add("signin", "Unable to signin");
				msgs.Add("register", "Unable to register");
				var msg = msgs[action];

				DisplayAlert ("Alert", msg, "OK");
			}

			notificationView.Hide (true);

			Monitor.Exit(LoginView.POSTLock);
		}
	}
}