namespace Dan.Common.Models;


/// <summary>
/// Wrapper class for paginated Brreg responses
/// </summary>
/// <typeparam name="T">Page data type</typeparam>
public class BrregPage<T> where T : class
{
    /// <summary>
    /// Page data
    /// </summary>
    [JsonProperty("_embedded")]
    public T? Embedded { get; set; }
        
    /// <summary>
    /// Pagination links
    /// </summary>
    [JsonProperty("_links")]
    public BrregLinks? Links { get; set; }
        
    /// <summary>
    /// Page info
    /// </summary>
    [JsonProperty("page")]
    public BrregPageInfo? Page { get; set; }
}

/// <summary>
/// Brreg sub units page wrapper class
/// </summary>
public class Subunits
{
    /// <summary>
    /// List of Brreg sub units
    /// </summary>
    [JsonProperty("underenheter")]
    public List<EntityRegistryUnit>? SubUnits { get; set; }
}

/// <summary>
/// Page metadata
/// </summary>
public class BrregPageInfo
{
    /// <summary>
    /// Page size
    /// </summary>
    [JsonProperty("size")]
    public int Size { get; set; }
    
    /// <summary>
    /// Total amount of elements across all pages
    /// </summary>
    [JsonProperty("totalElements")]
    public int TotalElements { get; set; }
    
    /// <summary>
    /// Total amount of pages based on current size
    /// </summary>
    [JsonProperty("totalPages")]
    public int TotalPages { get; set; }
    
    /// <summary>
    /// Current number of page
    /// </summary>
    [JsonProperty("number")]
    public int Number { get; set; }
}

/// <summary>
/// Pagination links
/// </summary>
public class BrregLinks
{
    /// <summary>
    /// Link to first page
    /// </summary>
    [JsonProperty("first")]
    public BrregLink? First { get; set; }
    
    /// <summary>
    /// Link to previous page
    /// </summary>
    [JsonProperty("prev")]
    public BrregLink? Prev { get; set; }
    
    /// <summary>
    /// Link to this page
    /// </summary>
    [JsonProperty("self")]
    public BrregLink? Self { get; set; }
    
    /// <summary>
    /// Link to next page
    /// </summary>
    [JsonProperty("next")]
    public BrregLink? Next { get; set; }
    
    /// <summary>
    /// Link to last page
    /// </summary>
    [JsonProperty("last")]
    public BrregLink? Last { get; set; }
}

/// <summary>
/// Pagination link class
/// </summary>
public class BrregLink
{
    /// <summary>
    /// Link url value
    /// </summary>
    [JsonProperty("href")]
    public string? Href { get; set; }
}