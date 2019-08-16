using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using ElasticSearchTester.Data;
using ElasticSearchTester.Data.Models;
using ElasticSearchTester.Utils;
using Flurl.Http;
using Flurl.Http.Configuration;
using Newtonsoft.Json;

namespace ElasticSearchTester
{

	public class Program
	{
		private static readonly Random random = new Random();
		private static readonly CoverageUtils coverageUtils = new CoverageUtils(random);
		private static readonly DummyUtils dummyUtils = new DummyUtils(coverageUtils);

		public static async Task Main(string[] args)
		{
			FlurlHttp.ConfigureClient(Config.ElasticSearchAddress, cli =>
				cli.Settings.HttpClientFactory = new UntrustedCertClientFactory());

			#region Get input

			string indexName;
			int usersToCreate;
			int usersIdOffset;
			int usersBatchSize;
			int documentsToCreate;
			int documentsIdOffset;
			int documentsBatchSize;

			Console.Write("Use default values? [y/...]: ");
			if (Console.ReadLine() != "y")
			{
				Console.Write("Index name: ");
				indexName = Console.ReadLine();
				usersToCreate = ConsoleUtils.PromptValue("Users to create: ");
				usersIdOffset = ConsoleUtils.PromptValue("Users' id offset(from which id to start - inclusive): ");
				usersBatchSize = ConsoleUtils.PromptValue("Create users by batch size: ");
				documentsToCreate = ConsoleUtils.PromptValue("Documents to create: ");
				documentsIdOffset = ConsoleUtils.PromptValue("Documents' id offset(from which id to start - inclusive): ");
				documentsBatchSize = ConsoleUtils.PromptValue("Create documents by batch size: ");
			}
			else
			{
				indexName = "cz_demo_index";
				usersToCreate = 20;
				usersIdOffset = 0;
				usersBatchSize = 20;
				documentsToCreate = 1500;
				documentsIdOffset = 0;
				documentsBatchSize = 50;
			}

			#endregion

			Console.WriteLine("All needed variables gathered. Proceeding to creating data");

			Stopwatch watches = new Stopwatch();

			Tuple<List<DummyUser>, long> createUsersTuple = await dummyUtils.CreateUsers(
				usersToCreate,
				usersIdOffset,
				usersBatchSize,
				false,
				watches
			);
			Console.WriteLine($"Creating {usersToCreate} users took: {createUsersTuple.Item2}ms");

			await CreateIndex(indexName, watches);

			await CreateDocuments(
				documentsToCreate,
				documentsIdOffset,
				documentsBatchSize,
				indexName,
				createUsersTuple.Item1,
				watches);
		}

		private static async Task<long> CreateIndex(string indexName, Stopwatch watches)
		{
			Console.WriteLine("Creating index for upcoming documents");
			watches.Restart();
			await $"{Config.ElasticSearchAddress}/{indexName}"
				// .WithBasicAuth("admin", "admin")
				.PutJsonAsync(new
				{
					settings = new
					{
						number_of_replicas = 0
					}
				});

			try
			{
				dynamic czech = new
				{
					type = "text",
					analyzer = "czech"
				};

				dynamic keyword = new
				{
					type = "keyword"
				};
				
				await $"{Config.ElasticSearchAddress}/{indexName}/_mapping"
					.PutJsonAsync(new
					{
						properties = new
						{
							title = new
							{
								type = "text",
								fields = new
								{
									czech,
									keyword
								}
							},
							author = keyword,
							created = new
							{
								type = "date"
							},
							content = new
							{
								type = "text",
								fields = new
								{
									czech,
									keyword
								}
							},
							roles = keyword,
							users = keyword,
							mainarea = new
							{
								type = "text",
								fields = new
								{
									keyword
								}
							},
							subarea = new
							{
								type = "text",
								fields = new
								{
									czech,
									keyword
								}
							},
							product = new
							{
								type = "text",
								fields = new
								{
									czech,
									keyword
								}
							},
							scope = new
							{
								type = "text",
								fields = new
								{
									keyword
								}
							}
						}
					});
			}
			catch (FlurlHttpException e)
			{
			}

			return watches.ElapsedMilliseconds;
		}

		private static async Task CreateDocuments(
			int documentsToCreate,
			int idOffset,
			int documentsBatchSize,
			string indexName,
			List<DummyUser> users,
			Stopwatch watches)
		{
			Console.WriteLine("Creating demo documents...");
			StringBuilder builder = new StringBuilder();
			for (int i = 0; i < (int) Math.Ceiling(documentsToCreate / (decimal) documentsBatchSize); i++)
			{
				Console.WriteLine($"Batch {i} started");
				watches.Restart();
				for (
					int j = (i * documentsBatchSize) + idOffset;
					j < ((i + 1) * documentsBatchSize) + idOffset; //  && j < documentsToCreate + idOffset
					j++
				)
				{
					builder
						.Append(JsonConvert.SerializeObject(new
						{
							index = new
							{
								_index = indexName,
								_id = j
							}
						}))
						.AppendLine()
						.Append(
							JsonConvert.SerializeObject(new
							{
								title = $"Document_{j:0000}.txt",
								author = users[random.Next(users.Count)].Username,
								created = DateTime.Now,
								content = "Document's content",
								roles = coverageUtils.GetUpTo(3, CoverageConfig.DocumentPermissions),
								mainarea = coverageUtils.GetRandom(CoverageConfig.MainAreaCoverage),
								subarea = coverageUtils.GetRandom(CoverageConfig.SubAreaCoverage),
								product = coverageUtils.GetRandom(CoverageConfig.ProductsCoverage),
								placement = coverageUtils.GetRandom(CoverageConfig.PlacementCoverage),
								users = new[]
								{
									coverageUtils.GetOne(users).Username,
									coverageUtils.GetOne(users).Username,
									coverageUtils.GetOne(users).Username
								},
								organization = coverageUtils.GetUpTo(3, CoverageConfig.OrganizationCoverage)
							}))
						.AppendLine();
				}

				Console.WriteLine($"Generating NDJson string for batch took: {watches.ElapsedMilliseconds}ms");

				Console.WriteLine("Starting sending data to Elastic");

				StringContent content = new StringContent(builder.ToString());

				watches.Restart();
				content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
				await $"{Config.ElasticSearchAddress}/{indexName}/_bulk"
							.WithBasicAuth("admin", "admin")
							.PostAsync(
								content
							);

				Console.WriteLine($"Sending data took: {watches.ElapsedMilliseconds}ms");

				builder.Clear();
			}
		}
	}

	public class UntrustedCertClientFactory : DefaultHttpClientFactory
	{
		public override HttpMessageHandler CreateMessageHandler()
		{
			return new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback = (a, b, c, d) => true
			};
		}
	}
}