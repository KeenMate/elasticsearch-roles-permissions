using System;
using Newtonsoft.Json;

namespace ElasticSearchTester.Data.Models
{
	[Serializable]
	public class CpasUser
	{
		[JsonProperty(PropertyName = "oid")]
		public readonly string Oid;
		
		[JsonProperty(PropertyName = "login")]
		public readonly string Login;
		
		[JsonProperty(PropertyName = "role")]
		public readonly string Role;

		public CpasUser(string oid, string login, string role)
		{
			Oid = oid;
			Login = login;
			Role = role;
		}
	}
}