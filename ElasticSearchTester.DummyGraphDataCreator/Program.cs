using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
using Newtonsoft.Json.Linq;

namespace ElasticSearchTester.DummyGraphDataCreator
{
	class Program
	{
		private static readonly Random random = new Random();
		private static readonly CoverageUtils coverageUtils = new CoverageUtils(random);
		private static readonly DummyUtils dummyUtils = new DummyUtils(coverageUtils);

		static async Task Main(string[] _)
		{
			/**
			 * 1. Generate dummy users
			 * 2. Fetch document names from nearly-production documents
			 * 3. For picked date go for next 365 days to generate dummy traffic
			 * 		Based on the traffic coefficient generate some amount of operations over that day
			 * 4. Send dummy data in batches to ES dedicated index 
			 */

			#region GetInput

			string indexName;
			int usersToCreate;
			int batchSize;
			DateTime dateStart;

			Console.Write("Use default values? [y/...]: ");
			if (Console.ReadLine() != "y")
			{
				Console.Write("Index name: ");
				indexName = Console.ReadLine();
				usersToCreate = ConsoleUtils.PromptValue("Users to create: ");
				batchSize = ConsoleUtils.PromptValue("Data create batch size: ");
				Console.Write("Date to start from(yyyy/MM/dd): ");
				dateStart = DateTime.ParseExact(
					Console.ReadLine(),
					"yyyy/MM/dd",
					CultureInfo.CurrentCulture
				);
			}
			else
			{
				indexName = "cpas_downloaded_documents";
				usersToCreate = 800;
				batchSize = byte.MaxValue;
				dateStart = DateTime.Now;
			}

			#endregion

			Console.WriteLine("Generating dummy users");
			List<User> users = (await dummyUtils.CreateUsers(
				usersToCreate,
				0,
				usersToCreate,
				false,
				Stopwatch.StartNew()
			)).Item1;

			Console.WriteLine("Fetching real filenames");
			JObject parsedDocumentNamesResponse = JObject.Parse(
				await (await $"{Config.ElasticSearchAddress}/{Config.RealDocumentsIndexName}/_search"
					.PostJsonAsync(new
					{
						size = 10000,
						_source = new[]
						{
							"file.filename"
						}
					})).Content.ReadAsStringAsync()
			);

			Console.WriteLine("Parsing real filenames response");
			List<Tuple<Guid, string>> files = new List<Tuple<Guid, string>>();
			foreach (JToken token in parsedDocumentNamesResponse["hits"]["hits"].AsJEnumerable())
				files.Add(new Tuple<Guid, string>(Guid.NewGuid(), token["_source"]["file"]["filename"].Value<string>()));

			int userOidCounter = 0;
			int amountOfPartsPerDay = CoverageConfig.TrafficCoefficientOfDay.Length;
			Task request = Task.CompletedTask;

			Console.WriteLine("Transforming Bosses list");
			List<CpasUserEntry> parsedBosses =
				users
					.Where(x => x.Roles.Contains(nameof(Roles.Boss)) || x.Roles.Contains(nameof(Roles.Manager)))
					.Select(x => new CpasUserEntry(
						userOidCounter++,
						$"cpas\\{x.Username}",
						"The BOSS"
					)).ToList();
			double[] softmaxedCoefficients = coverageUtils.SoftmaxFn(CoverageConfig.TrafficCoefficientOfDay);
			// Parallel.For(0, 52, async i =>
			for (int i = 0; i < 52; i++)
			{
				StringBuilder builder = new StringBuilder(5_000_000);
				// each week of year
				Console.WriteLine($"Week: {i}");
				for (int j = 0; j < 7; j++)
				{
					// each day of week
					Console.WriteLine($"\tDay: {Enum.GetNames(typeof(DayOfWeek))[j]}");
					int operationsPerDay = (int) (CoverageConfig.TrafficCoefficientOfWeek[j] * random.Next(100, 200));
					Console.WriteLine($"\t\tOperations generated: {operationsPerDay}");
					for (int k = 0; k < amountOfPartsPerDay; k++)
					{
						// each part of day
						int operations = (int) (softmaxedCoefficients[k] * operationsPerDay);
						// decimal batches = Math.Ceiling(operations / (decimal) batchSize);
						// Console.WriteLine($"\t\t\tOperations after coefficient multiplication: {operations}");
						// for (int l = 0; l < batches; l++)
						// {
						// each operations batch of day part
						// Console.WriteLine($"\t\t\tBatch: {l}");
						for (int m = 0; m < operations; m++)
						{
							CpasUserEntry substitution = null;
							if (random.Next(100) == 1)
								substitution = coverageUtils.GetOne(parsedBosses);

							builder
								.Append(JsonConvert.SerializeObject(new
								{
									index = new
									{
										_index = indexName,
										_id = $"{i}_{j}_{k}_{m}" // (int) (i * 7 + j * amountOfPartsPerDay + k * operations + m + 1) // + k * batches + l * batchSize + m
									}
								}))
								.AppendLine()
								.Append(
									JsonConvert.SerializeObject(new
									{
										user = new CpasUserEntry(
											userOidCounter++,
											coverageUtils.GetOne(users).Username,
											"aaa bbb"
										),
										substitution,
										downloaded_at = dateStart
																		.AddYears(-1)
																		.AddDays(i * 7 + j)
																		.AddSeconds(25200 + random.Next(54000))
																		.ToString("yyyy-MM-dd HH:mm:ss"),
										from = random.Next(3) == 0 ? "intranet" : "extranet",
										reports = coverageUtils
															.GetUpTo(4, files)
															.Select(x => new
															{
																filename = x.Item2,
																id = x.Item1
															}),
										report_type = new
										{
											id = random.Next(15),
											title = "Report type name"
										}
									}))
								.AppendLine();
						}

						// }
					}

					Console.WriteLine("\t\tSending new request");
					StringContent content = new StringContent(builder.ToString());
					content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
					// await request;
					await $"{Config.ElasticSearchAddress}/{indexName}/_bulk"
						.PostAsync(content);

					builder.Clear();
				}
			}

			//);
		}
	}
}