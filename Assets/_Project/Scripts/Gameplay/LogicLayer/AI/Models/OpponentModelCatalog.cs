using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Loads and caches all <see cref="OpponentModelDefinition"/> assets from Resources/OpponentModels
/// and resolves them by id.
/// </summary>
public static class OpponentModelCatalog
{
    private const string ResourcesPath = "OpponentModels";

    private static List<OpponentModelDefinition> cachedModels;

    /// <summary>Returns all defined models (cached), sorted by display name.</summary>
    public static IReadOnlyList<OpponentModelDefinition> GetAll()
    {
        if (cachedModels == null)
        {
            cachedModels = Resources
                .LoadAll<OpponentModelDefinition>(ResourcesPath)
                .Where(model => model != null && !string.IsNullOrWhiteSpace(model.Id))
                .OrderBy(model => model.DisplayName)
                .ToList();
        }

        return cachedModels;
    }

    /// <summary>Returns the model with the given id, falling back to the "random" model if not found.</summary>
    public static OpponentModelDefinition GetById(string id)
    {
        var models = GetAll();

        var model = models.FirstOrDefault(x => x.Id == id);

        if (model != null)
            return model;

        return models.FirstOrDefault(x => x.Id == "random");
    }
}
