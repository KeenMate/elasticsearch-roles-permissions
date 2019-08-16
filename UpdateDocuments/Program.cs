using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using ElasticSearchTester.Data;
using ElasticSearchTester.Data.Models;
using ElasticSearchTester.Utils;
using Flurl.Http;
using Flurl.Http.Configuration;
using Newtonsoft.Json.Linq;

namespace UpdateDocuments
{
	public class Program
	{
		private static readonly CoverageUtils coverageUtils = new CoverageUtils(new Random());
		private static readonly DummyUtils dummyUtils = new DummyUtils(coverageUtils);

		public static async Task Main(string[] args)
		{
			FlurlHttp.ConfigureClient(Config.ElasticSearchAddress, cli =>
				cli.Settings.HttpClientFactory = new UntrustedCertClientFactory());

			FlurlHttp.GlobalSettings.BeforeCall = call =>
			{
				Console.WriteLine($"Before: {call.Request.RequestUri}");
			};
			
			FlurlHttp.GlobalSettings.BeforeCall = call =>
			{
				Console.WriteLine($"After: {call.Request.RequestUri}");
			};

			#region Get input

			string indexName;
			int usersToCreate;

			Console.Write("Use default values? [y/...]: ");
			if (Console.ReadLine() != "y")
			{
				Console.Write("Index name: ");
				indexName = Console.ReadLine();
				usersToCreate = ConsoleUtils.PromptValue("Users to create: ");
			}
			else
			{
				indexName = "cz_index";
				usersToCreate = 1500;
			}

			#endregion

			Tuple<List<DummyUser>, long> users = await dummyUtils.CreateUsers(
				usersToCreate, 0, short.MaxValue, false, Stopwatch.StartNew());

			Console.WriteLine("Getting all documents");
			HttpResponseMessage documentsResponse = await $"{Config.ElasticSearchAddress}/{indexName}/_search"
				.PostJsonAsync(
					new
					{
						_source = new dynamic[0],
						size = 10000
					});

			Console.WriteLine($"Parsing all documents");
			JObject parsedDocs = JObject.Parse(await documentsResponse.Content.ReadAsStringAsync());
			Console.WriteLine($"{parsedDocs["hits"]["total"]["value"].Value<int>()} documents parsed");
			List<Task> requests = new List<Task>(
				parsedDocs["hits"]["total"]["value"].Value<int>()
			);
			Console.WriteLine("Sending update requests");
			foreach (JToken document in parsedDocs["hits"]["hits"].AsJEnumerable())
			{
				requests.Add($"{Config.ElasticSearchAddress}/{indexName}/_update/{document["_id"]}"
					.PostJsonAsync(new
					{
						doc = new
						{
							mainarea = coverageUtils.GetRandom(CoverageConfig.MainAreaCoverage),
							subarea = coverageUtils.GetRandom(CoverageConfig.SubAreaCoverage),
							product = coverageUtils.GetRandom(CoverageConfig.ProductsCoverage),
							placement = coverageUtils.GetRandom(CoverageConfig.PlacementCoverage),
							users = new[]
							{
								coverageUtils.GetOne(users.Item1),
								coverageUtils.GetOne(users.Item1),
								coverageUtils.GetOne(users.Item1)
							}
						}
					}));
			}

			Console.WriteLine("Waiting for unfinished requests");
			Task.WaitAll(requests.ToArray());

			Console.WriteLine("Done!");
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