using System;

namespace ElasticSearchTester.Utils
{
	public static class ConsoleUtils
	{
		public static int PromptValue(string text)
		{
			string input;
			int output;
			do
			{
				Console.Write(text);
				input = Console.ReadLine();
			} while (!int.TryParse(input, out output));

			return output;
		}
	}
}