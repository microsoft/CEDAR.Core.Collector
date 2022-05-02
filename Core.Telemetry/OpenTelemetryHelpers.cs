using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.CloudMine.Core.Telemetry
{
    public static class OpenTelemetryHelpers
    {
        public const string GenevaExporterName = "Geneva";

        public static readonly string Product;
        public static readonly string Service;
        public static readonly string AppEnvironment;
        public static readonly string Deployment;
        public static readonly string Region;
        public static readonly string OpenTelemetryExporter;
        
        static OpenTelemetryHelpers()
        {
            OpenTelemetryExporter = GetEnvironmentVariableWithDefault("OpenTelemetryExporter", defaultValue: "*");
            AppEnvironment = GetEnvironmentVariableWithDefault("Environment", defaultValue: "*");
            Deployment = GetEnvironmentVariableWithDefault("Deployment", defaultValue: "*");
            Region = GetEnvironmentVariableWithDefault("Region", defaultValue: "*");

            // if environment variables for exporting to geneva are not set, revert to default exporter.
            try
            {
                Product = GetEnvironmentVariableWithDefault("Product");
                Service = GetEnvironmentVariableWithDefault("Service");
            }
            catch
            {
                OpenTelemetryExporter = "*";
                Product = "*";
                Service = "Collectors";
            }
        }

        public static Activity AddDefaultTags(this Activity activity)
        {
            activity.AddTag("Product", Product);
            activity.AddTag("Service", Service);
            activity.AddTag("Environment", AppEnvironment);
            activity.AddTag("Deployment", Deployment);
            activity.AddTag("Region", Region);
            activity.AddTag("Depth", GetActivityDepth() + 1);
            return activity;
        }

        public static int GetActivityDepth()
        { 
            try
            {
                Activity a = Activity.Current;
                return (int) a.GetTagItem("Depth");
            }
            catch (Exception)
            {
                return 0;
            }
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
