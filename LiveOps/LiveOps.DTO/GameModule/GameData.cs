using System.Collections.Generic;
using Newtonsoft.Json;

namespace Madbox.LiveOps.DTO.GameModule
{
    /// <summary>
    /// Aggregates multiple module payloads for a single client response.
    /// </summary>
    public class GameData
    {
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto)]
        public readonly List<IGameModuleData> ModulesData = new List<IGameModuleData>();

        public List<IGameModuleData> GetModules()
        {
            return ModulesData;
        }

        public void AddModules(List<IGameModuleData> value)
        {
            foreach (IGameModuleData gameData in value)
            {
                AddModuleData(gameData);
            }
        }

        public void AddModuleData(IGameModuleData data)
        {
            if (data != null)
            {
                ModulesData.Add(data);
            }
        }

        public T GetModuleData<T>() where T : IGameModuleData
        {
            foreach (IGameModuleData module in ModulesData)
            {
                if (module is T moduleData)
                {
                    return moduleData;
                }
            }
            return default!;
        }

        public T GetModuleData<T>(string key) where T : IGameModuleData
        {
            foreach (IGameModuleData module in ModulesData)
            {
                if (module.Key == key)
                {
                    return (T)module;
                }
            }
            return default!;
        }
    }
}
