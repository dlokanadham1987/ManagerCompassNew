using ManagerCompass.Api.Models;

namespace ManagerCompass.Api.Services;

/// <summary>
/// Single seam between the API and its data source. MockGuidanceDataService returns synthetic
/// sample data today; swap in a SharePoint/Power BI/Measure-What-Matters-backed implementation
/// later without changing any controller.
/// </summary>
public interface IGuidanceDataService
{
    IReadOnlyList<Topic> GetTopics();
    Topic? GetTopic(string key);
    Snapshot GetSnapshot();
    LeadershipCard GetLeadershipCard();
    AdoptionMetrics GetAdoptionMetrics();
    AskResponse Ask(AskRequest request);
}
