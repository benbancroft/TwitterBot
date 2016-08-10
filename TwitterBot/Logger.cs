using System;

namespace TwitterBot
{
	public class Logger
	{
		private static readonly object loggerLock = new object ();

		public static void LogInfo(String format, params object[] args)
		{
			lock (Logger.loggerLock) {
				Console.WriteLine (format, args);
				using (var file = new System.IO.StreamWriter ("TwitterBot.log", true))
					file.WriteLine (format, args);
			}

		}

		public static void LogError(String format, params object[] args)
		{
			Logger.LogInfo (format, args);
		}
	}
}

