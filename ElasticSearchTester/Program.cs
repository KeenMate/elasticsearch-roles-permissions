using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
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

	enum Organizations
	{
		Cp,
		Generali,
		Cp_Generali
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

		private const string ElasticSearchAddress = "https://192.168.1.141:9200";

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

		private static Dictionary<string, decimal> organizationCoverage = new Dictionary<string, decimal>
		{
			{nameof(Organizations.Cp), 0.8m},
			{nameof(Organizations.Generali), 0.15m},
			{nameof(Organizations.Cp_Generali), 0.05m},
		};

		public static async Task Main(string[] args)
		{
			FlurlHttp.ConfigureClient(ElasticSearchAddress, cli =>
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
				usersToCreate = promptValue("Users to create: ");
				usersIdOffset = promptValue("Users' id offset(from which id to start - inclusive): ");
				usersBatchSize = promptValue("Create users by batch size: ");
				documentsToCreate = promptValue("Documents to create: ");
				documentsIdOffset = promptValue("Documents' id offset(from which id to start - inclusive): ");
				documentsBatchSize = promptValue("Create documents by batch size: ");
			}
			else
			{
				indexName = "cpas_docs";
				usersToCreate = short.MaxValue;
				usersIdOffset = 0;
				usersBatchSize = 250;
				documentsToCreate = 4500;
				documentsIdOffset = 0;
				documentsBatchSize = 250;
			}

			#endregion

			Console.WriteLine("All needed variables gathered. Proceeding to creating data");
			long elapsed;

			Stopwatch watches = new Stopwatch();

			Tuple<List<User>, long> createUsersTuple = await CreateUsers(
				usersToCreate,
				usersIdOffset,
				usersBatchSize,
				false,
				watches
			);
			Console.WriteLine($"Creating {usersToCreate} users took: {createUsersTuple.Item2}ms");

			// elapsed = await CreateIndex(indexName, watches);
			// Console.WriteLine($"Creating index took: {elapsed}ms");

			await CreateDocuments(
				documentsToCreate,
				documentsIdOffset,
				documentsBatchSize,
				indexName,
				createUsersTuple.Item1,
				watches);
		}

		private static int promptValue(string text)
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

		private static async Task<long> CreateIndex(string indexName, Stopwatch watches)
		{
			Console.WriteLine("Creating index for upcoming documents");
			watches.Restart();
			await $"{ElasticSearchAddress}/{indexName}"
						.WithBasicAuth("admin", "admin")
						.PutJsonAsync(new
						{
							settings = new
							{
								number_of_replicas = 0
							},
							mappings = new
							{
								properties = new
								{
									title = new
									{
										type = "keyword"
									},
									author = new
									{
										type = "keyword"
									},
									created = new
									{
										type = "date"
									},
									content = new
									{
										type = "text"
									},
									roles = new
									{
										type = "keyword"
									},
									users = new
									{
										type = "keyword"
									},
									main_area = new
									{
										type = "keyword"
									},
									sub_area = new
									{
										type = "keyword"
									},
									product = new
									{
										type = "keyword"
									},
									scope = new
									{
										type = "keyword"
									}
								}
							}
						});

			return watches.ElapsedMilliseconds;
		}

		private static async Task CreateDocuments(
			int documentsToCreate,
			int idOffset,
			int documentsBatchSize,
			string indexName,
			List<User> users,
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
								roles = getUpTo(3, documentPermissions),
								users = new[]
								{
									getOne(users).Username,
									getOne(users).Username,
									getOne(users).Username
								},
								organization = getUpTo(3, organizationCoverage)
							}))
						.AppendLine();
				}

				Console.WriteLine($"Generating NDJson string for batch took: {watches.ElapsedMilliseconds}ms");
				
				Console.WriteLine("Starting sending data to Elastic");
				
				StringContent content = new StringContent(builder.ToString());

				watches.Restart();
				content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
				await $"{ElasticSearchAddress}/{indexName}/_bulk"
							.WithBasicAuth("admin", "admin")
							.PostAsync(
								content
							);

				Console.WriteLine($"Sending data took: {watches.ElapsedMilliseconds}ms");

				builder.Clear();
			}
		}

		private static async Task<Tuple<List<User>, long>> CreateUsers(
			int usersToCreate,
			int idOffset,
			int usersBatchSize,
			bool sendData,
			Stopwatch watches)
		{
			Console.WriteLine($"Generating {usersToCreate} users");

			List<User> usersToReturn = new List<User>(usersToCreate);
			watches.Restart();
			for (int i = 0; i < (int) Math.Ceiling(usersToCreate / (decimal) usersBatchSize); i++)
			{
				List<User> users = new List<User>(usersBatchSize);
				for (
					int j = (i * usersBatchSize) + idOffset;
					j < ((i + 1) * usersBatchSize) + idOffset && j < (usersToCreate + idOffset);
					j++
				)
					users.Add(new User
					{
						Username = $"User{j:00000}",
						Password = j.ToString("00000"),
						Roles = getUpTo(3, userRoles)
					});
				IEnumerable<dynamic> usersOperations = users.Select(x => new
				{
					op = "add",
					path = $"/{x.Username}",
					value = x
				});

				Console.WriteLine("Sending users");
				watches.Restart();

				Task request = Task.CompletedTask;
				if (sendData)
					request = $"{ElasticSearchAddress}/_searchguard/api/internalusers"
										.WithBasicAuth("admin", "admin")
										.PatchJsonAsync(usersOperations);

				usersToReturn.AddRange(users);

				await request;
			}

			return new Tuple<List<User>, long>(usersToReturn, watches.ElapsedMilliseconds);
		}

		private static List<string> getUpTo(int number, Dictionary<string, decimal> options)
		{
			int count = random.Next(1, number + 1);
			List<string> result = new List<string>();

			Dictionary<string, decimal> remaining = options
																							.ToList()
																							.ToDictionary(x => x.Key, x => x.Value);
			for (int i = 0; i < count; i++)
			{
				string chosen = getRandom(remaining);

				remaining.Remove(chosen);

				result.Add(chosen);
			}

			return result;
		}

		private static T getOne<T>(IList<T> list)
		{
			return list[random.Next(list.Count)];
		}

		// private static List<string> get3(Dictionary<string, decimal> options)
		// {
		// 	Dictionary<string, decimal> localOptions = options
		// 																						 .ToList()
		// 																						 .ToDictionary(x => x.Key, x => x.Value);
		// 	string option1 = getRandom(localOptions);
		// 	
		// 	localOptions.Remove(option1); // var options2 = options.Where(x => x.Key != option1).ToDictionary(x => x.Key, x => x.Value);
		// 	string option2 = getRandom(localOptions);
		// 	
		// 	localOptions.Remove(option2); // var options3 = options2.Where(x => x.Key != option2).ToDictionary(x => x.Key, x => x.Value);
		// 	string option3 = getRandom(localOptions);
		// 	
		// 	return new List<string>(3)
		// 	{
		// 		option1,
		// 		option2,
		// 		option3
		// 	};
		// }

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
		public override HttpMessageHandler CreateMessageHandler()
		{
			return new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback = (a, b, c, d) => true
			};
		}
	}
}