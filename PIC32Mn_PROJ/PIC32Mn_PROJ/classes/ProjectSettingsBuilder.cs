using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PIC32Mn_PROJ.classes
{
    public static class ProjectSettingsBuilder
    {
        public static void BuildOrUpdateProjectSettings(
            string projectDirectory,
            IEnumerable<string> selectedFeatures,
            string device = null,
            Dictionary<string, object>? generalSettings = null)
        {
            // Load existing settings or create new
            var settings = ProjectSettingsManager.Load(projectDirectory);

            // Set device if provided
            if (!string.IsNullOrEmpty(device))
                settings.Device = device;

            // Set general settings if provided
            if (generalSettings != null)
                settings.General = new Dictionary<string, object>(generalSettings);

            // Ensure Features dictionary exists
            settings.Features ??= new Dictionary<string, Dictionary<string, object>>();

            // Add or update selected features
            foreach (var feature in selectedFeatures)
            {
                if (FeatureDefaults.All.TryGetValue(feature, out var defaults))
                {
                    if (!settings.Features.ContainsKey(feature))
                        settings.Features[feature] = new Dictionary<string, object>(defaults);
                    else
                    {
                        // Add any new keys from defaults, don't overwrite user values
                        foreach (var kvp in defaults)
                            if (!settings.Features[feature].ContainsKey(kvp.Key))
                                settings.Features[feature][kvp.Key] = kvp.Value;
                    }
                }
            }

            // Optionally: Remove unselected features
            var toRemove = new List<string>();
            foreach (var feature in settings.Features.Keys)
                if (!selectedFeatures.Contains(feature))
                    toRemove.Add(feature);
            foreach (var feature in toRemove)
                settings.Features.Remove(feature);

            // Save updated settings
            ProjectSettingsManager.Save(projectDirectory, settings);
        }
    }
}