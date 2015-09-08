using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Foundation;
using UIKit;

namespace Todo
{
	public class Application
	{
		// This is the main entry point of the application.
		static void Main (string[] args)
		{
			// if you want to use a different Application Delegate class from "AppDelegate"
			// you can specify it here.
			UIApplication.Main (args, null, "AppDelegate");
		}

		public static void Debug(string message = "HERE",
			[CallerLineNumber] int lineNumber = 0,
			[CallerMemberName] string caller = null,
			[CallerFilePath] string file = null)
		{
			Console.WriteLine(message + " at line " + lineNumber + " (" + caller + ") " + "[" + file + "]");
		}

		public static DateTime NSDateToDateTime(NSDate date)
		{
			DateTime reference = new DateTime(2001, 1, 1, 0, 0, 0);
			DateTime currentDate = reference.AddSeconds(date.SecondsSinceReferenceDate);
			DateTime localDate = currentDate.ToLocalTime ();
			return localDate;
		}

		public static NSDate DateTimeToNSDate(DateTime date)
		{
			if (date.Kind == DateTimeKind.Unspecified)
				date = DateTime.SpecifyKind (date, DateTimeKind.Local/* DateTimeKind.Local or DateTimeKind.Utc, this depends on each app */);

			return (NSDate) date;
		}
	}
}
