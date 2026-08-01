using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Cloners;


namespace Aishi_trader;


[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 2)]
public class AishiLogger(
    ISptLogger<AishiLogger> logger,
    ICloner cloner,
    DatabaseService databaseService,
    LocaleService localeService)
{

    public void SetTraderUpdateTime(TraderConfig traderConfig, TraderBase baseJson, int refreshTimeSecondsMin, int refreshTimeSecondsMax)
    {
        // Add refresh time in seconds to config
        var traderRefreshRecord = new UpdateTime
        {
            TraderId = baseJson.Id,
            Seconds = new MinMax<int>(refreshTimeSecondsMin, refreshTimeSecondsMax)
        };

        traderConfig.UpdateTime.Add(traderRefreshRecord);
    }

    /// <summary>
    /// Add a traders base data to the server, no assort items
    /// </summary>
    /// <param name="traderDetailsToAdd">trader details</param>
    public void AddTraderWithEmptyAssortToDb(TraderBase traderDetailsToAdd)
    {
        var emptyTraderItemAssortObject = new TraderAssort
        {
            Items = [],
            BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
            LoyalLevelItems = new Dictionary<MongoId, int>()
        };

        var traderDataToAdd = new Trader
        {
            Assort = emptyTraderItemAssortObject,
            Base = cloner.Clone(traderDetailsToAdd),
            QuestAssort = new() // quest assort is empty as trader has no assorts unlocked by quests
            {
                // We create 3 empty arrays, one for each of the main statuses that are possible
                { "Started", new() },
                { "Success", new() },
                { "Fail", new() }
            },
            Dialogue = []
        };

        if (!databaseService.GetTables().Traders.TryAdd(traderDetailsToAdd.Id, traderDataToAdd))
        {
            //Failed to add trader!
        }
    }
    /// <param name="baseJson">json file for trader (db/base.json)</param>
    /// <param name="firstName">First name of trader</param>
    /// <param name="description">Flavor text of whom the trader is</param>
    public void AddTraderToLocales(TraderBase baseJson, string firstName, string description)
    {
        var locales = databaseService.GetTables().Locales.Global;
        var newTraderId = baseJson.Id;
        var fullName = baseJson.Name;
        var nickName = baseJson.Nickname;
        var location = baseJson.Location;

        foreach (var (localeKey, localeKvP) in locales)
        {
            localeKvP.AddTransformer(lazyloadedLocaleData =>
            {
                lazyloadedLocaleData.Add($"{newTraderId} FullName", fullName);
                lazyloadedLocaleData.Add($"{newTraderId} FirstName", firstName);
                lazyloadedLocaleData.Add($"{newTraderId} Nickname", nickName);
                lazyloadedLocaleData.Add($"{newTraderId} Location", location);
                lazyloadedLocaleData.Add($"{newTraderId} Description", description);
                return lazyloadedLocaleData;
            });
        }
    }

    /// <summary>
    /// Overwrite the desired traders assorts with the ones provided
    /// </summary>
    /// <param name="traderId">Trader to override assorts of</param>
    /// <param name="newAssorts">new assorts we want to add</param>
    public void OverwriteTraderAssort(string traderId, TraderAssort newAssorts)
    {
        if (!databaseService.GetTables().Traders.TryGetValue(traderId, out var traderToEdit))
        {
            logger.Warning($"Unable to update assorts for trader: {traderId}, they couldn't be found on the server");

            return;
        }

        traderToEdit.Assort = newAssorts;
    }
}