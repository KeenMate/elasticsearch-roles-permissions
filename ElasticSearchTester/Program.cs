using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;
using Newtonsoft.Json;

namespace ElasticSearchTester
{
	enum Roles
	{
		Boss,
		Manager,
		Admin,
		Hr,
		Employee,
		Role1,
		Role2,
		Role3,
		Role4,
		Role5,
		Role6,
		Role7,
		Role8,
		Role9,
		Role10,
	}

	[Serializable]
	class User
	{
		[JsonIgnore] public string Username { get; set; }

		[JsonProperty(propertyName: "password")]
		public string Password { get; set; }

		[JsonProperty(propertyName: "backend_roles")]
		public List<string> Roles { get; set; }
	}

	public class Program
	{
		private static readonly Random random = new Random();

		private const string ElasticSearchAddress = "https://localhost:9200";

		private static Dictionary<string, decimal> userRoles = new Dictionary<string, decimal>
		{
			{nameof(Roles.Employee), 0.6m},
			{nameof(Roles.Manager), 0.125m},
			{nameof(Roles.Admin), 0.025m},
			{nameof(Roles.Role1), 0.024m},
			{nameof(Roles.Role2), 0.024m},
			{nameof(Roles.Role3), 0.024m},
			{nameof(Roles.Role4), 0.024m},
			{nameof(Roles.Role5), 0.024m},
			{nameof(Roles.Role7), 0.024m},
			{nameof(Roles.Role6), 0.024m},
			{nameof(Roles.Role8), 0.024m},
			{nameof(Roles.Role9), 0.024m},
			{nameof(Roles.Role10), 0.024m},
			{nameof(Roles.Hr), 0.009m},
			{nameof(Roles.Boss), 0.001m}
		};

		private static Dictionary<string, decimal> documentPermissions = new Dictionary<string, decimal>
		{
			{nameof(Roles.Employee), 0.74m},
			{nameof(Roles.Role1), 0.0120m},
			{nameof(Roles.Role2), 0.0120m},
			{nameof(Roles.Role3), 0.0120m},
			{nameof(Roles.Role4), 0.0120m},
			{nameof(Roles.Role5), 0.0120m},
			{nameof(Roles.Role6), 0.0120m},
			{nameof(Roles.Role7), 0.0120m},
			{nameof(Roles.Role8), 0.0120m},
			{nameof(Roles.Role9), 0.0120m},
			{nameof(Roles.Role10), 0.0120m},
			{nameof(Roles.Hr), 0.09m},
			{nameof(Roles.Manager), 0.04m},
			{nameof(Roles.Boss), 0.01m}
		};
		
		public static void Main(string[] args)
		{
			FlurlHttp.ConfigureClient(ElasticSearchAddress, cli =>
				cli.Settings.HttpClientFactory = new UntrustedCertClientFactory());

			int usersToCreate = 100;

			int firstUserId = 1;

			Console.WriteLine($"Generating {usersToCreate} users");
			List<User> users = new List<User>(usersToCreate);
			for (int i = firstUserId; i < firstUserId + usersToCreate; i++)
				users.Add(new User
				{
					Username = $"User{i:00000}",
					Password = i.ToString("00000"),
					Roles = get3(userRoles)
				});

			Console.WriteLine("Sending users");
			Stopwatch watches = Stopwatch.StartNew();
			$"{ElasticSearchAddress}/_searchguard/api/internalusers"
				.WithBasicAuth("admin", "admin")
				.PatchJsonAsync(users.Select(x => new
				{
					op = "add",
					path = $"/{x.Username}",
					value = x
				})).Wait();
			Console.WriteLine($"Creating {usersToCreate} users took: {watches.ElapsedMilliseconds}ms");

			// Console.WriteLine("Creating index for upcoming documents");
			// $"{ElasticSearchAddress}/cpas_test".PutJsonAsync(new
			// {
			// 	settings = new
			// 	{
			// 		number_of_replicas = 0
			// 	},
			// 	mappings = new
			// 	{
			// 		properties = new
			// 		{
			// 			title = new
			// 			{
			// 				type = "keyword"
			// 			},
			// 			author = new
			// 			{
			// 				type = "keyword"
			// 			},
			// 			created = new
			// 			{
			// 				type = "date"
			// 			},
			// 			content = new
			// 			{
			// 				type = "text"
			// 			},
			// 			roles = new
			// 			{
			// 				type = "array"
			// 			},
			// 			users = new
			// 			{
			// 				type = "array"
			// 			},
			// 			main_area = new
			// 			{
			// 				type = "keyword"
			// 			},
			// 			sub_area = new
			// 			{
			// 				type = "keyword"
			// 			},
			// 			product = new
			// 			{
			// 				type = "keyword"
			// 			},
			// 			scope = new
			// 			{
			// 				type = "keyword"
			// 			}
			// 		}
			// 	}
			// });
			//
			// Console.WriteLine("Creating demo documents...");
			// StringBuilder builder = new StringBuilder();
			// for (int i = 0; i < 2048; i++)
			// {
			// 	builder.Append(
			// 					 JsonConvert.SerializeObject(new
			// 					 {
			// 						 title = $"Document_{i:0000}.txt",
			// 						 author = users[random.Next(short.MaxValue)].Username,
			// 						 created = DateTime.Now,
			// 						 content = "Document's content",
			// 						 roles = get3(documentPermissions),
			// 						 users = new[]
			// 						 {
			// 							 getOne(users),
			// 							 getOne(users),
			// 							 getOne(users)
			// 						 }
			// 					 }))
			// 				 .Append("\n");
			// }
			//
			// watches.Restart();
			// $"{ElasticSearchAddress}/cpas_test/_bulk".PostAsync(
			// 	new StringContent(builder.ToString())
			// ).Wait();
			// Console.WriteLine($"Inserting documents took: {watches.ElapsedMilliseconds}ms");
		}

		private static T getOne<T>(IList<T> list)
		{
			return list[random.Next(list.Count)];
		}

		private static List<string> get3(Dictionary<string, decimal> options)
		{
			string option1 = getRandom(options);

			var options2 = options.Where(x => x.Key != option1).ToDictionary(x => x.Key, x => x.Value);
			string option2 = getRandom(options2);

			var options3 = options2.Where(x => x.Key != option2).ToDictionary(x => x.Key, x => x.Value);
			string option3 = getRandom(options3);

			return new List<string>(3)
			{
				option1,
				option2,
				option3
			};
		}

		/// <summary>
		/// </summary>
		/// <param name="options">Options with assigned probability</param>
		/// <returns></returns>
		private static string getRandom(Dictionary<string, decimal> options)
		{
			if (options.Values.Sum() > 1)
				throw new ArgumentOutOfRangeException(nameof(options), "Probability sum cannot exceed 1");

			List<decimal> chances = new List<decimal>(options.Count);
			for (int i = 0; i < options.Count; i++)
				chances.Add((decimal) random.NextDouble());

			List<KeyValuePair<string, decimal>> orderedOptions = options
																													 .OrderBy(pair => pair.Value)
																													 .ToList();

			int j = 0;
			foreach (var option in orderedOptions)
			{
				if (chances[j] < option.Value)
					return option.Key;

				j++;
			}

			return orderedOptions
						 .Last()
						 .Key;
		}
	}
	
	public class UntrustedCertClientFactory : DefaultHttpClientFactory
	{
		public override HttpMessageHandler CreateMessageHandler() {
			return new HttpClientHandler {
				ServerCertificateCustomValidationCallback = (a, b, c, d) => true
			};
		}
	}
}