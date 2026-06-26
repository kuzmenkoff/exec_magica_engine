using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class OpponentModelCatalog
{
    private const string ResourcesPath = "OpponentModels";

    private static List<OpponentModelDefinition> cachedModels;

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

    public static OpponentModelDefinition GetById(string id)
    {
        var models = GetAll();

        var model = models.FirstOrDefault(x => x.Id == id);

        if (model != null)
            return model;

        return models.FirstOrDefault(x => x.Id == "random");
    }
}
