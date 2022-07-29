using System.Globalization;
using Newtonsoft.Json;

namespace Dan.Common.Helpers.Util;

/// <summary>
/// Utility to generate dummy data
/// </summary>
public static class DummyData
{
    private static readonly string[] Adverbs = { "dearly", "deceivingly", "tediously", "rather", "bashfully", "speedily", "wildly", "woefully", "yearly", "frantically", "however", "honestly", "kindheartedly", "greedily", "intensely", "far", "occasionally", "openly", "potentially", "correctly", "sternly", "quirkily", "annually", "deeply", "briskly", "probably", "continually", "wrongly", "crazily", "hardly", "needily", "rudely", "bravely", "initially", "actually", "well", "highly", "queasily", "officially", "unexpectedly", "madly", "almost", "reproachfully", "briefly", "hopelessly", "painfully", "nervously", "lazily", "roughly", "certainly", "limply", "bitterly", "cleverly", "frankly", "instead", "strictly", "elsewhere", "helpfully", "weekly", "therefore", "fairly", "urgently", "else", "politely", "warmly", "basically", "queerly", "moreover", "upwardly", "uselessly", "coaxingly", "ahead", "carelessly", "gracefully", "only", "then", "unnecessarily", "ever", "fortunately", "victoriously", "definitely", "foolishly", "oddly", "zealously", "unimpressively", "totally", "tenderly", "abnormally", "kissingly", "hungrily", "solidly", "rarely", "playfully", "currently", "necessarily", "terrifically", "worriedly", "tightly", "joyfully", "unnaturally" };
    private static readonly string[] Adjectives = { "limping", "psychotic", "crazy", "womanly", "acidic", "unequaled", "satisfying", "silent", "dry", "shivering", "boorish", "tart", "rampant", "snobbish", "hot", "roasted", "vacuous", "cloudy", "neighborly", "humdrum", "puzzled", "delicious", "madly", "cute", "extra-large", "utopian", "brainy", "tasteless", "safe", "aboard", "gifted", "boiling", "sparkling", "disagreeable", "careful", "glamorous", "rural", "tacit", "abandoned", "ruthless", "envious", "faint", "ancient", "empty", "naive", "infamous", "hypnotic", "feigned", "used", "cool", "vivacious", "nimble", "helpless", "alike", "possessive", "bright", "macabre", "excited", "childlike", "hard-to-find", "fat", "knowledgeable", "grey", "calculating", "shaky", "black-and-white", "unaccountable", "well-groomed", "curved", "quarrelsome", "bored", "scintillating", "tense", "abashed", "mere", "mighty", "plant", "ubiquitous", "silly", "symptomatic", "bewildered", "cumbersome", "tame", "courageous", "alive", "inconclusive", "profuse", "encouraging", "standing", "lonely", "jazzy", "conscious", "venomous", "bouncy", "acoustic", "hapless", "fine", "zonked", "giant", "shocking" };
    private static readonly string[] Nouns = { "scale", "flame", "idea", "tomato", "dog", "impulse", "uncle", "meat", "moon", "talk", "twist", "fruit", "walk", "nut", "house", "bird", "frog", "pig", "bush", "control", "rail", "drop", "snake", "club", "decision", "marble", "lamp", "dad", "dirt", "bait", "boundary", "badge", "engine", "train", "pizza", "doctor", "grandmother", "spring", "profit", "letter", "umbrella", "pickle", "tin", "wool", "tub", "plot", "book", "war", "dust", "unit", "tramp", "mark", "guide", "work", "dog", "porter", "grape", "rule", "rose", "sign", "dinosaur", "toe", "bed", "feeling", "force", "neck", "judge", "teeth", "jail", "truck", "bee", "purpose", "blood", "calculator", "bit", "cent", "support", "berry", "bead", "bra", "zephyr", "nest", "scent", "part", "rate", "spider", "hand", "tiger", "zoo", "route", "parcel", "reward", "spark", "train", "bone", "end", "food", "insurance", "yarn", "women" };
    private static readonly string[] Tlds = { "com", "net", "no", "org", "cloud" };

    /// <summary>
    /// Returns a list of evidence values populated with dummy data
    /// </summary>
    /// <param name="ec">The evidence code definition</param>
    /// <param name="ehr">The evidence harvester request to use</param>
    /// <returns>A list of evidence value with dummy data</returns>
    public static List<EvidenceValue> GetDummyEvidenceValues(EvidenceCode ec, EvidenceHarvesterRequest? ehr = null)
    {
        var response = new List<EvidenceValue>();

        foreach (var ev in ec.Values)
        {
            response.Add(GenerateDummyEvidenceValue(ev, ehr));
        }

        return response;
    }

    /// <summary>
    /// Returns a single evidence value populated with dummy data
    /// </summary>
    /// <param name="ev">The evidence value definition</param>
    /// <param name="ehr">The evidence harvester request to use</param>
    /// <returns>A evidence value with dummy data</returns>
    public static EvidenceValue GenerateDummyEvidenceValue(EvidenceValue ev, EvidenceHarvesterRequest? ehr)
    {
        ehr ??= new EvidenceHarvesterRequest();
        ehr.AccreditationId = Guid.Empty.ToString();
        ehr.JWT = null;
        var digest = GetDigest(ev, ehr);

        switch (ev.ValueType)
        {
            case EvidenceValueType.Attachment:
                ev.Value = GenerateDummyBase64(digest);
                break;
            case EvidenceValueType.Boolean:
                ev.Value = GenerateDummyBoolean(digest);
                break;
            case EvidenceValueType.DateTime:
                ev.Value = GenerateDummyDateTime(digest);
                break;
            case EvidenceValueType.Number:
                ev.Value = GenerateDummyNumber(digest);
                break;
            case EvidenceValueType.String:
                ev.Value = GenerateDummyString(digest);
                break;
            case EvidenceValueType.Uri:
                ev.Value = GenerateDummaryUri(digest);
                break;
            case EvidenceValueType.Amount:
                ev.Value = GenerateDummyAmount(digest);
                break;
            case EvidenceValueType.JsonSchema:
                ev.Value = GenerateDummyComplexObject(digest);
                break;
        }

        ev.Timestamp = DateTime.Now;
        return ev;
    }

    private static string GenerateDummyAmount(int digest)
    {
        return GenerateDummyNumber(digest).ToString(CultureInfo.InvariantCulture) + " NOK";
    }

    private static string GenerateDummaryUri(int digest)
    {
        return "https://" + GetDummySentence(digest) + "." + GetDummyTld(digest) + "/" + GetDummySentence(digest >> 1, "-");
    }

    private static string GenerateDummyString(int digest)
    {
        return GetDummySentence(digest, " ");
    }

    private static float GenerateDummyNumber(int digest)
    {
        float result = Clamp(digest, 0, 1000000);
        if (Clamp(digest, 0, 9) > 6)
        {
            result = result + ((float)Clamp(digest, 0, 100) / 100);
        }

        return result;
    }

    private static DateTime GenerateDummyDateTime(int digest)
    {
        DateTime dt = DateTime.Parse("2016-12-24");
        dt = dt.AddYears(Clamp(digest, -5, 2));
        dt = dt.AddMonths(Clamp(digest, -5, 5));
        dt = dt.AddDays(Clamp(digest, -10, 10));
        dt = dt.AddHours(Clamp(digest, -10, 10));
        dt = dt.AddMinutes(Clamp(digest, -30, 30));
        dt = dt.AddSeconds(Clamp(digest, -30, 30));

        return dt;
    }

    private static bool GenerateDummyBoolean(int digest)
    {
        return Clamp(digest, 0, 9) > 4;
    }

    private static string GenerateDummyBase64(int digest)
    {
        var str = new StringBuilder();
        var repeats = Clamp(digest, 10, 30);
        for (var i = 0; i < repeats; i++)
        {
            str = str.Append(digest);
        }

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(str.ToString()));
    }

    private static string GetDummySentence(int digest, string spacer = "")
    {
        return GetEntryFromDigest(digest, Adverbs) + spacer + GetEntryFromDigest(digest, Adjectives) + spacer + GetEntryFromDigest(digest, Nouns);
    }

    private static string GetDummyTld(int digest)
    {
        return GetEntryFromDigest(digest, Tlds);
    }

    private static object GenerateDummyComplexObject(int digest)
    {
        return new { SomeNumber = digest, SomeName = GetDummySentence(digest) };
    }

    private static int GetDigest(EvidenceValue ev, EvidenceHarvesterRequest? ehr)
    {
        using (MD5 md5 = MD5.Create())
        {
            var serialized = JsonConvert.SerializeObject(ev) + JsonConvert.SerializeObject(ehr);
            return BitConverter.ToInt32(md5.ComputeHash(Encoding.UTF8.GetBytes(serialized)), 0);
        }
    }

    private static string GetEntryFromDigest(int digest, string[] input)
    {
        return input[Clamp(digest, 0, input.Length)];
    }

    private static int Clamp(int num, int min, int max)
    {
        return (Math.Abs(num) % (max - min)) + min;
    }
}