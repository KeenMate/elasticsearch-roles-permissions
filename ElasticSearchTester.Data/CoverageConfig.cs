using System.Collections.Generic;
using ElasticSearchTester.Data.Enums;
using ElasticSearchTester.Data.Models;

namespace ElasticSearchTester.Data
{
	public static class CoverageConfig
	{
		public static readonly Dictionary<string, decimal> MainAreaCoverage = new Dictionary<string, decimal>
		{
			{nameof(MainAreas.Area1), 0.4m},
			{nameof(MainAreas.Area2), 0.3m},
			{nameof(MainAreas.Area3), 0.3m}
		};

		public static readonly Dictionary<string, decimal> SubAreaCoverage = new Dictionary<string, decimal>
		{
			{nameof(SubAreas.SubArea1), 0.1m},
			{nameof(SubAreas.SubArea2), 0.3m},
			{nameof(SubAreas.SubArea3), 0.2m},
			{nameof(SubAreas.SubArea4), 0.1m},
			{nameof(SubAreas.SubArea5), 0.2m},
			{nameof(SubAreas.SubArea6), 0.1m}
		};

		public static readonly Dictionary<string, decimal> ProductsCoverage = new Dictionary<string, decimal>
		{
			{nameof(Products.Product1), 0.06m},
			{nameof(Products.Product2), 0.04m},
			{nameof(Products.Product3), 0.01m},
			{nameof(Products.Product4), 0.09m},
			{nameof(Products.Product5), 0.05m},
			{nameof(Products.Product6), 0.05m},
			{nameof(Products.Product7), 0.05m},
			{nameof(Products.Product8), 0.05m},
			{nameof(Products.Product9), 0.03m},
			{nameof(Products.Product10), 0.07m},
			{nameof(Products.Product11), 0.05m},
			{nameof(Products.Product12), 0.05m},
			{nameof(Products.Product13), 0.05m},
			{nameof(Products.Product14), 0.02m},
			{nameof(Products.Product15), 0.08m},
			{nameof(Products.Product16), 0.05m},
			{nameof(Products.Product17), 0.05m},
			{nameof(Products.Product18), 0.08m},
			{nameof(Products.Product19), 0.05m},
			{nameof(Products.Product20), 0.02m}
		};

		public static readonly Dictionary<string, decimal> PlacementCoverage = new Dictionary<string, decimal>
		{
			{nameof(Scopes.Placement1), 0.2m},
			{nameof(Scopes.Placement2), 0.1m},
			{nameof(Scopes.Placement3), 0.2m},
			{nameof(Scopes.Placement4), 0.1m},
			{nameof(Scopes.Placement5), 0.3m},
			{nameof(Scopes.Placement6), 0.1m},
		};

		public static readonly Dictionary<string, decimal> UserRoles = new Dictionary<string, decimal>
		{
			{nameof(Roles.VPA), 0.6m},
			{nameof(Roles.MS), 0.125m},
			{nameof(Roles.RP), 0.025m},
			// {nameof(Roles.Role1), 0.024m},
			// {nameof(Roles.Role2), 0.024m},
			// {nameof(Roles.Role3), 0.024m},
			// {nameof(Roles.Role4), 0.024m},
			// {nameof(Roles.Role5), 0.024m},
			// {nameof(Roles.Role7), 0.024m},
			// {nameof(Roles.Role6), 0.024m},
			// {nameof(Roles.Role8), 0.024m},
			// {nameof(Roles.Role9), 0.024m},
			// {nameof(Roles.Role10), 0.024m},
			// {nameof(Roles.Hr), 0.249m},
			{nameof(Roles.RR), 0.001m}
		};

		public static readonly Dictionary<string, decimal> DocumentPermissions = new Dictionary<string, decimal>
		{
			{nameof(Roles.VPA), 0.74m},
			// {nameof(Roles.Role1), 0.0120m},
			// {nameof(Roles.Role2), 0.0120m},
			// {nameof(Roles.Role3), 0.0120m},
			// {nameof(Roles.Role4), 0.0120m},
			// {nameof(Roles.Role5), 0.0120m},
			// {nameof(Roles.Role6), 0.0120m},
			// {nameof(Roles.Role7), 0.0120m},
			// {nameof(Roles.Role8), 0.0120m},
			// {nameof(Roles.Role9), 0.0120m},
			// {nameof(Roles.Role10), 0.0120m},
			{nameof(Roles.RP), 0.21m},
			{nameof(Roles.RR), 0.04m},
			{nameof(Roles.MS), 0.01m}
		};

		public static readonly Dictionary<string, decimal> OrganizationCoverage = new Dictionary<string, decimal>
		{
			{nameof(Organizations.Cp), 0.8m},
			{nameof(Organizations.Generali), 0.15m},
			{nameof(Organizations.CpGenerali), 0.05m},
		};
		
		public static readonly Dictionary<string, decimal> DocumentTypeCoverage = new Dictionary<string, decimal>
		{
			{nameof(DocumentTypes.Pdf), 0.3m},
			{nameof(DocumentTypes.MSExcel), 0.05m},
			{nameof(DocumentTypes.MSWord), 0.5m},
			{nameof(DocumentTypes.MSPowerPoint), 0.1m},
			{nameof(DocumentTypes.Txt), 0.05m}
		};

		public static readonly decimal[] TrafficCoefficientOfWeek = {
			// Sun
			0.15m,
			// Mon
			1.71m,
			// Tue
			1.8m,
			// Wed
			1.6m,
			// Thu
			1.3m,
			// Fri
			1m,
			// Sat
			0.35m
		};

		public static readonly decimal[] TrafficCoefficientOfDay =
		{
			// Morning
			0.8m,
			// Afternoon
			1.4m,
			// Evening
			0.45m
		};
	}
}