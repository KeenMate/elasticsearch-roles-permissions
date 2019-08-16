using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ElasticSearchTester.Data.Models
{
	[Serializable]
	public class DummyUser
	{
		[JsonIgnore] public string Username { get; set; }

		[JsonProperty(propertyName: "password")]
		public string Password { get; set; }

		[JsonProperty(propertyName: "backend_roles")]
		public List<string> Roles { get; set; }
	}
}