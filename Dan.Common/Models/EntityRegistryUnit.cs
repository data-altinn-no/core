using Dan.Common.Util;

namespace Dan.Common.Models;

// Not our model, can add descriptions to fields on request
#pragma warning disable 1591
// ReSharper disable once InconsistentNaming
public class EntityRegistryUnit
{
    [JsonProperty("organisasjonsnummer", Required = Required.Always)]
    public string Organisasjonsnummer { get; set; } = null!;

    [JsonProperty("navn", Required = Required.Always)]
    public string Navn { get; set; } = null!;

    [JsonProperty("organisasjonsform", Required = Required.Always)]
    public Organisasjonsform Organisasjonsform { get; set; } = null!;

    [JsonProperty("hjemmeside", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public Uri? Hjemmeside { get; set; }

    [JsonProperty("slettedato", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
    public DateTimeOffset? Slettedato { get; set; }

    [JsonProperty("postadresse", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public AdresseDto? Postadresse { get; set; }

    [JsonProperty("registreringsdatoEnhetsregisteret", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
    public DateTimeOffset? RegistreringsdatoEnhetsregisteret { get; set; }

    [JsonProperty("registrertIMvaregisteret", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public bool? RegistrertIMvaregisteret { get; set; }

    [JsonProperty("frivilligMvaRegistrertBeskrivelser", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public List<string>? FrivilligMvaRegistrertBeskrivelser { get; set; }

    [JsonProperty("naeringskode1", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public NaeringsKodeDto? Naeringskode1 { get; set; }

    [JsonProperty("naeringskode2", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public NaeringsKodeDto? Naeringskode2 { get; set; }

    [JsonProperty("naeringskode3", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public NaeringsKodeDto? Naeringskode3 { get; set; }

    [JsonProperty("antallAnsatte", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public int? AntallAnsatte { get; set; }

    [JsonProperty("overordnetEnhet", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public string? OverordnetEnhet { get; set; }
    
    [JsonProperty("underEnheter", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public List<string>? Underenheter { get; set; }

    [JsonProperty("oppstartsdato", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
    public DateTimeOffset? OppstartsDato { get; set; }

    [JsonProperty("forretningsadresse", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public AdresseDto? Forretningsadresse { get; set; }

    [JsonProperty("beliggenhetsadresse", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public AdresseDto? Beliggenhetsadresse { get; set; }

    [JsonProperty("stiftelsesdato", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
    public DateTimeOffset? Stiftelsesdato { get; set; }

    [JsonProperty("institusjonellSektorkode", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public SektorKodeDto? InstitusjonellSektorkode { get; set; }

    [JsonProperty("registrertIForetaksregisteret", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public bool? RegistrertIForetaksregisteret { get; set; }

    [JsonProperty("registrertIStiftelsesregisteret", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public bool? RegistrertIStiftelsesregisteret { get; set; }

    [JsonProperty("registrertIFrivillighetsregisteret", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public bool? RegistrertIFrivillighetsregisteret { get; set; }

    [JsonProperty("sisteInnsendteAarsregnskap", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public string? SisteInnsendteAarsregnskap { get; set; }

    [JsonProperty("konkurs", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public bool? Konkurs { get; set; }

    [JsonProperty("underAvvikling", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public bool? UnderAvvikling { get; set; }

    [JsonProperty("underTvangsavviklingEllerTvangsopplosning", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public bool? UnderTvangsavviklingEllerTvangsopplosning { get; set; }

    [JsonProperty("maalform", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public string? Maalform { get; set; }

    [JsonProperty("_links", Required = Required.Always)]
    public Links Links { get; set; } = null!;
}

public class AdresseDto
{
    [JsonProperty("land", Required = Required.Always)]
    public string Land { get; set; } = null!;

    [JsonProperty("landkode", Required = Required.Always)]
    public string Landkode { get; set; } = null!;

    [JsonProperty("postnummer", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public string? Postnummer { get; set; }

    [JsonProperty("poststed", Required = Required.Always)]
    public string Poststed { get; set; } = null!;

    [JsonProperty("adresse", Required = Required.Always)]
    public List<string> Adresse { get; set; } = null!;

    [JsonProperty("kommune", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public string? Kommune { get; set; }

    [JsonProperty("kommunenummer", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public string? Kommunenummer { get; set; }
}

public class NaeringsKodeDto
{
    [JsonProperty("beskrivelse", Required = Required.Always)]
    public string Beskrivelse { get; set; } = null!;

    [JsonProperty("kode", Required = Required.Always)]
    public string Kode { get; set; } = null!;

    [JsonProperty("hjelpeenhetskode", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public bool? Hjelpenhetskode { get; set; }
}

public class SektorKodeDto
{
    [JsonProperty("kode", Required = Required.Always)]
    public string Kode { get; set; } = null!;

    [JsonProperty("beskrivelse", Required = Required.Always)]
    public string Beskrivelse { get; set; } = null!;
}

public class Links
{
    [JsonProperty("self", Required = Required.Always)]
    public Link Self { get; set; } = null!;

    [JsonProperty("overordnetEnhet", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public Link? OverordnetEnhet { get; set; }
}

public class Link
{
    [JsonProperty("href", Required = Required.Always)]
    public Uri Href { get; set; } = null!;
}

public class Organisasjonsform
{
    [JsonProperty("kode", Required = Required.Always)]
    public string Kode { get; set; } = null!;

    [JsonProperty("beskrivelse", Required = Required.Always)]
    public string Beskrivelse { get; set; } = null!;

    [JsonProperty("_links", Required = Required.Always)]
    public Links Links { get; set; } = null!;
}
#pragma warning restore 1591