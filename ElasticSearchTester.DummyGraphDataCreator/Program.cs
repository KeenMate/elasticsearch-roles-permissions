using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using ElasticSearchTester.Data;
using ElasticSearchTester.Data.Enums;
using ElasticSearchTester.Data.Models;
using ElasticSearchTester.Utils;
using Flurl.Http;
using Newtonsoft.Json;

namespace ElasticSearchTester.DummyGraphDataCreator
{
	class Program
	{
		private const char delimiter = ';';

		private static readonly Random random = new Random();

		private static readonly CoverageUtils coverageUtils = new CoverageUtils(random);

		// private static readonly DummyUtils dummyUtils = new DummyUtils(coverageUtils);

		static async Task Main(string[] args)
		{
			#region Validations

			if (args.Length < 3)
			{
				Console.WriteLine("Not enough arguments");
				return;
			}

			if (!File.Exists(args[0]))
			{
				Console.WriteLine($"Users file: {args[0]} Could not be found.");
				return;
			}

			if (!File.Exists(args[1]))
			{
				Console.WriteLine($"Document types: {args[1]} Could not be found.");
				return;
			}

			if (!File.Exists(args[2]))
			{
				Console.WriteLine($"File names: {args[1]} Could not be found.");
				return;
			}

			#endregion

			string usersFileName = args[0];
			string reportTypesFileName = args[1];
			string fileNamesFileName = args[2];

			#region GetInput

			string indexName;
			int totalOperations;

			DateTime dateEnd;

			Console.Write("Use default values? [y/...]: ");
			if (Console.ReadLine() != "y")
			{
				Console.Write("Index name: ");
				indexName = Console.ReadLine();
				totalOperations = ConsoleUtils.PromptValue("Total operations: ");
				Console.Write("Date to start from(yyyy/MM/dd): ");
				dateEnd = DateTime.ParseExact(
					Console.ReadLine(),
					"yyyy/MM/dd",
					CultureInfo.CurrentCulture
				);
			}
			else
			{
				indexName = "cpas_downloaded_documents";
				totalOperations = 7_000;
				dateEnd = DateTime.Now;
			}

			#endregion

			int operationsPerDay = totalOperations / 365;
			Console.WriteLine($"Operations per day: {operationsPerDay}");

			List<CpasUser> users = GetUsers(usersFileName);
			List<CpasUser> parsedBosses = GetSubstitutionableUsers(users)
				.ToList();

			List<ReportFile> files = GetFilenames(fileNamesFileName);

			List<ReportType> reportTypes = GetReportTypes(reportTypesFileName, out List<double> weights);
			double[] softmaxedWeights = coverageUtils
				.SoftmaxFn(weights.ToArray());

			Console.WriteLine("Softmaxed: ");
			foreach (var t in softmaxedWeights)
			{
				Console.WriteLine(t);
			}
			
			List<CoverageInfo<ReportType>> reportTypesCoverage = reportTypes
				.Select((x, i) => new CoverageInfo<ReportType>(x, softmaxedWeights[i]))
				.ToList();


			int operationsGenerated = 0;
			int totalReports = 0;
			DateTime dateStart = dateEnd.AddYears(-1);
			StringBuilder builder = new StringBuilder(500_000);
			for (int i = 0; i < 365; i++)
			{
				dateStart = dateStart.AddDays(1);
				if (i % 7 == 0)
					Console.WriteLine($"Week: {i / 7}");

				int operations = (int) (operationsPerDay *
																CoverageConfig.TrafficCoefficientOfWeek[(int) dateStart.DayOfWeek] +
																random.Next(10));
				operationsGenerated += operations;
				for (int j = 0; j < operations; j++)
				{
					AppendNewDocument(
						builder,
						indexName,
						files,
						coverageUtils.GetProbabilisticRandom(reportTypesCoverage),
						users,
						parsedBosses,
						dateStart,
						$"{i}_{j}",
						ref totalReports
					);
				}

				StringContent content = new StringContent(builder.ToString());
				content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);

				await $"{Config.ElasticSearchAddress}/_bulk"
					.PostAsync(content);
				// Console.WriteLine($"Sending data: {builder}");

				builder.Clear();
			}

			Console.WriteLine($"Operations in total: {operationsGenerated}");
			Console.WriteLine($"Total reports sent: {totalReports}");
		}

		private static void AppendNewDocument(
			StringBuilder builder,
			string indexName,
			List<ReportFile> files,
			ReportType reportType,
			List<CpasUser> users,
			List<CpasUser> substitutionable,
			DateTime baseDate,
			object docId,
			ref int reportsSelected)
		{
			CpasUser substitution = null;
			if (random.Next(100) == 1)
				substitution = coverageUtils.GetOne(substitutionable);

			List<ReportFile> reports = coverageUtils
				.GetUpTo(4, files);

			reportsSelected += reports.Count;

			builder
				.Append(JsonConvert.SerializeObject(new
				{
					index = new
					{
						_index = indexName,
						_id = docId
					}
				}))
				.AppendLine()
				.Append(
					JsonConvert.SerializeObject(new
					{
						user = coverageUtils.GetOne(users),
						substitution,
						downloaded_at = GetDateForReport(baseDate),
						from = GetRandomLocation(),
						reports,
						report_type = reportType
					}))
				.AppendLine();
		}

		private static string GetRandomLocation() => random.Next(3) == 0 ? "intranet" : "extranet";

		private static string GetDateForReport(DateTime date)
		{
			return date
				.AddSeconds(25200 + random.Next(54000))
				.ToString("yyyy-MM-dd HH:mm:ss");
		}

		private static IEnumerable<CpasUser> GetSubstitutionableUsers(IEnumerable<CpasUser> users)
		{
			return users
				.Where(x =>
					x.Role.Contains(nameof(Roles.MS)) ||
					x.Role.Contains(nameof(Roles.RR)));
		}

		private static List<CpasUser> GetUsers(string csvFile)
		{
			return readTextFile(csvFile, row =>
			{
				string[] columns = row.Split(delimiter);

				return new CpasUser(columns[0], columns[1], columns[2]);
			});
		}

		private static List<ReportType> GetReportTypes(string csvFile, out List<double> weights)
		{
			List<double> weightsTmp = new List<double>();
			List<ReportType> result = readTextFile(csvFile, row =>
			{
				string[] columns = row.Split(delimiter);

				weightsTmp.Add(double.Parse(columns[2]));
				return new ReportType(columns[0], columns[1]);
			});

			weights = weightsTmp;
			weights.ForEach(x => Console.WriteLine($"Weight from file: {x}"));

			return result;
		}

		private static List<ReportFile> GetFilenames(string csvFile)
		{
			return readTextFile(csvFile, row =>
				new ReportFile(Guid.NewGuid(), row));
		}

		private static List<T> readTextFile<T>(string csvFile, Func<string, T> magic)
		{
			if (!File.Exists(csvFile))
				throw new FileNotFoundException("Text file could not be found", csvFile);

			List<T> result = new List<T>();

			using (FileStream file = File.OpenRead(csvFile))
			using (StreamReader reader = new StreamReader(file))
			{
				while (!reader.EndOfStream)
				{
					string row = reader
						.ReadLine();

					result.Add(magic(row));
				}
			}

			return result;
		}
	}
}