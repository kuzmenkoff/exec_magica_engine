/// <summary>
/// Stores one loaded card set file and its parsed card data.
/// 
/// Example:
/// SetName = "NeutralCoreCards"
/// Cards = cards loaded from NeutralCoreCards.json
/// </summary>
public class CardSetInfo
{
    public string FileName;
    public string SetName;
    public int OrderPriority;
    public AllCards Cards;

    /// <summary>Human-readable set name; falls back to a prettified file name.</summary>
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(SetName))
                return SetName;

            if (!string.IsNullOrWhiteSpace(FileName))
                return FileName.Replace("_", " ").Replace("-", " ");

            return "Unknown Card Set";
        }
    }
}