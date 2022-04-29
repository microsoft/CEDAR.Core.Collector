using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.CloudMine.Core.Telemetry
{
    public static class OpenTelemetryHelpers
    {

        public static readonly string Product = GetEnvironmentVariableWithDefault("Product");
        public static readonly string Service = GetEnvironmentVariableWithDefault("Service");
        public static readonly string AppEnvironment = GetEnvironmentVariableWithDefault("Environment", defaultValue: "*");
        public static readonly string Deployment = GetEnvironmentVariableWithDefault("Deployment", defaultValue: "*");
        public static readonly string Region = GetEnvironmentVariableWithDefault("Region", defaultValue: "*");

        public static Activity AddDefaultTags(this Activity activity)
        {
            activity.AddTag("Product", Product);
            activity.AddTag("Service", Service);
            activity.AddTag("Environment", AppEnvironment);
            activity.AddTag("Deployment", Deployment);
            activity.AddTag("Region", Region);

            return activity;
        }

        public static void AddWithDefaultTags<T>(this Counter<T> counter, T value, TagList tags) where T : struct
        {
            tags.Add("Product", Product);
            tags.Add("Service", Service);
            tags.Add("Environment", AppEnvironment);
            tags.Add("Deployment", Deployment);
            tags.Add("Region", Region);
            counter.Add(value, tags);
        }

        public static string GetEnvironmentVariableWithDefault(string variable, string defaultValue = null)
        {
            try
            {
                return Environment.GetEnvironmentVariable(variable);
            }
            catch (ArgumentNullException)
            {
                if (defaultValue == null)
                {
                    throw;
                }

                return defaultValue;
            }
        }
    }
}
