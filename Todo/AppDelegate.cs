using Foundation;
using UIKit;

using SQLite;

namespace Todo
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
	[Register ("AppDelegate")]
	public class AppDelegate : UIApplicationDelegate
	{
		// class-level declarations
		public UINavigationController navigationController;

		public override UIWindow Window {
			get;
			set;
		}

		public AppDelegate ()
		{
			Globals.SQLite = new SQLiteConnection (System.IO.Path.Combine (Globals.SQLiteDir, "settings.db"));

			Globals.SQLite.Execute("VACUUM");

			Globals.SQLite.CreateTable<Settings>();

			var settings = Globals.SQLite.Table<Settings>().Where (v => v.API_Key != null);

			if (0 != settings.Count()) {
				Globals.theSettings = settings.First ();
			}
		}

		public override bool FinishedLaunching (UIApplication application, NSDictionary launchOptions)
		{
			// create a new window instance based on the screen size
			Window = new UIWindow (UIScreen.MainScreen.Bounds);

			// If you have defined a root view controller, set it here:
			UIViewController controller;

			if (null != Globals.theSettings) {
				controller = new TodoListView();
			} else {
				controller = new LoginView ();
			}

			navigationController = new UINavigationController (controller);
			Window.RootViewController = navigationController;

			// make the window visible
			Window.MakeKeyAndVisible ();

			return true;
		}

		public override void OnResignActivation (UIApplication application)
		{
			// Invoked when the application is about to move from active to inactive state.
			// This can occur for certain types of temporary interruptions (such as an incoming phone call or SMS message) 
			// or when the user quits the application and it begins the transition to the background state.
			// Games should use this method to pause the game.
		}

		public override void DidEnterBackground (UIApplication application)
		{
			// Use this method to release shared resources, save user data, invalidate timers and store the application state.
			// If your application supports background exection this method is called instead of WillTerminate when the user quits.
		}

		public override void WillEnterForeground (UIApplication application)
		{
			// Called as part of the transiton from background to active state.
			// Here you can undo many of the changes made on entering the background.
		}

		public override void OnActivated (UIApplication application)
		{
			// Restart any tasks that were paused (or not yet started) while the application was inactive. 
			// If the application was previously in the background, optionally refresh the user interface.
		}

		public override void WillTerminate (UIApplication application)
		{
			// Called when the application is about to terminate. Save data, if needed. See also DidEnterBackground.
		}
	}
}


