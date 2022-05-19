// Copyright (c) Microsoft Corporation. All rights reserved.

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
            OpenTelemetryExporter = Environment.GetEnvironmentVariable("OpenTelemetryExporter") ?? "*";
            AppEnvironment = Environment.GetEnvironmentVariable("Environment") ?? "*";
            Deployment = Environment.GetEnvironmentVariable("Deployment") ?? "*";
            Region = Environment.GetEnvironmentVariable("Region") ?? "*";
            Product = Environment.GetEnvironmentVariable("Product");
            Service = Environment.GetEnvironmentVariable("Service");

            if( Product == null || Service == null )
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

        public static Activity Fail(this Activity activity)
        {
            activity.SetTag("success", false);
            return activity;
        }

        public static int GetActivityDepth()
        { 
            if (Activity.Current?.GetTagItem("Depth") is int depth)
            {
                return depth;
            }

            return -1;
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
    }
}
