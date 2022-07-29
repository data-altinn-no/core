namespace Dan.Common.Helpers.Util;

/// <summary>
/// The evidence builder.
/// </summary>
public class EvidenceBuilder
{
    private readonly EvidenceCode? _evidenceCode;
    private readonly List<EvidenceValue> _evidenceValues = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EvidenceBuilder"/> class.
    /// </summary>
    /// <param name="metadata">
    /// The metadata describing all evidence codes for this source
    /// </param>
    /// <param name="evidenceCodeName">
    /// The evidence code.
    /// </param>
    /// <exception cref="Exception">
    /// If a evidence code not present in the metadata is supplied
    /// </exception>
    public EvidenceBuilder(IEvidenceSourceMetadata metadata, string evidenceCodeName)
    {
        _evidenceCode = metadata.GetEvidenceCodes().Find(x => x.EvidenceCodeName == evidenceCodeName);
        if (_evidenceCode == null)
        {
            throw new ArgumentException("Invalid evidenceCodeName supplied", nameof(evidenceCodeName));
        }
    }

    /// <summary>
    /// Adds a value to the evidence
    /// </summary>
    /// <param name="evidenceValueName">
    /// The evidence value name.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public void AddEvidenceValue(string evidenceValueName, object? value)
    {
        var evidenceValue = GetEvidenceValue(evidenceValueName);
        evidenceValue.Value = value == null ? null : ConvertToEvidenceType(evidenceValue.ValueType, value);
        evidenceValue.Timestamp = DateTime.UtcNow;
        _evidenceValues.Add(evidenceValue);
    }

    /// <summary>
    /// Adds a value to the evidence
    /// </summary>
    /// <param name="evidenceValueName">
    /// The evidence value name.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    /// <param name="source">
    /// The data source
    /// </param>
    /// <param name="includeJsonSchemaDef">
    /// Option to omit jsonschemadefition in response to reduce size
    /// </param>
    public void AddEvidenceValue(string evidenceValueName, object? value, string source, bool includeJsonSchemaDef = true)
    {
        var evidenceValue = GetEvidenceValue(evidenceValueName);
        evidenceValue.Value = value == null ? null : ConvertToEvidenceType(evidenceValue.ValueType, value);
        evidenceValue.Timestamp = DateTime.UtcNow;
        evidenceValue.Source = source;

        if (!includeJsonSchemaDef && evidenceValue.ValueType == EvidenceValueType.JsonSchema)
            evidenceValue.JsonSchemaDefintion = null;

        _evidenceValues.Add(evidenceValue);
    }


    /// <summary>
    /// Gets all the evidence values.
    /// </summary>
    /// <returns>
    /// The <see cref="EvidenceValue"/>.
    /// </returns>
    public List<EvidenceValue> GetEvidenceValues()
    {
        return _evidenceValues;
    }

    /// <summary>
    /// Returns a new instance of EvidenceValue with the provided name
    /// </summary>
    /// <param name="evidenceValueName">
    /// The evidence Value Name.
    /// </param>
    /// <exception cref="Exception">
    /// If a evidence value name not present in the metadata is supplied
    /// </exception>
    /// <returns>
    /// The <see cref="EvidenceValue"/>.
    /// </returns>
    private EvidenceValue GetEvidenceValue(string evidenceValueName)
    {
        var evidenceValue = _evidenceCode?.Values.Find(x => x.EvidenceValueName == evidenceValueName);
        if (evidenceValue == null)
        {
            throw new ArgumentException("Invalid evidenceValueName supplied", nameof(evidenceValueName));
        }

        return (EvidenceValue)evidenceValue.Clone();
    }

    private static object? ConvertToEvidenceType(EvidenceValueType? type, object value)
    {
        switch (type)
        {
            case EvidenceValueType.String:
            case EvidenceValueType.Uri:
            case EvidenceValueType.Amount:
                return value?.ToString();
            case EvidenceValueType.Number:
            {
                var legalNumberTypes = new List<Type>
                {
                    typeof(int),
                    typeof(long),
                    typeof(decimal),
                    typeof(float),
                    typeof(double),
                };
                if (legalNumberTypes.Contains(value.GetType()))
                {
                    return value;
                }

                break;
            }
            case EvidenceValueType.DateTime when value is DateTime time:
                return time;
            case EvidenceValueType.Boolean when value is bool b:
                return b;
            case EvidenceValueType.Attachment when value is byte[] bytes:
                return Convert.ToBase64String(bytes);
            case EvidenceValueType.JsonSchema:
                // TODO! Validate against schema
                return value;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        throw new NotImplementedException();
    }
}