using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ElasticSearchTester.Data;
using ElasticSearchTester.Data.Models;
using Flurl.Http;

namespace ElasticSearchTester.Utils
{
	public class DummyUtils
	{
		private readonly CoverageUtils coverage;
		
		public DummyUtils(CoverageUtils coverage)
		{
			this.coverage = coverage;
		}
		
		public async Task<Tuple<List<User>, long>> CreateUsers(
			int usersToCreate,
			int idOffset,
			int usersBatchSize,
			bool sendData,
			Stopwatch watches)
		{
			Console.WriteLine($"Generating {usersToCreate} users");

			if (usersBatchSize == 0)
				usersBatchSize = usersToCreate;
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
						Roles = coverage.GetUpTo(3, CoverageConfig.UserRoles)
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
					request = $"{Config.ElasticSearchAddress}/_searchguard/api/internalusers"
										.WithBasicAuth("admin", "admin")
										.PatchJsonAsync(usersOperations);

				usersToReturn.AddRange(users);

				await request;
			}

			return new Tuple<List<User>, long>(usersToReturn, watches.ElapsedMilliseconds);
		}
	}
}