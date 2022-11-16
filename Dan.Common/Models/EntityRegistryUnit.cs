using Dan.Common.Util;

namespace Dan.Common.Models;

// ReSharper disable once InconsistentNaming
public class EntityRegistryUnit
{
    [JsonProperty("organisasjonsnummer")]
    public string Organisasjonsnummer { get; set; } = null!;

    [JsonProperty("navn")]
    public string Navn { get; set; } = null!;

    [JsonProperty("organisasjonsform")]
    public Organisasjonsform Organisasjonsform { get; set; } = null!;

    [JsonProperty("hjemmeside", NullValueHandling = NullValueHandling.Ignore)]
    public Uri? Hjemmeside { get; set; }

    [JsonProperty("slettedato", NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
    public DateTimeOffset? Slettedato { get; set; }

    [JsonProperty("postadresse", NullValueHandling = NullValueHandling.Ignore)]
    public AdresseDto? Postadresse { get; set; }

    [JsonProperty("registreringsdatoEnhetsregisteret")]
    [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
    public DateTimeOffset RegistreringsdatoEnhetsregisteret { get; set; }

    [JsonProperty("registrertIMvaregisteret")]
    public bool RegistrertIMvaregisteret { get; set; }

    [JsonProperty("frivilligMvaRegistrertBeskrivelser", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? FrivilligMvaRegistrertBeskrivelser { get; set; }

    [JsonProperty("naeringskode1", NullValueHandling = NullValueHandling.Ignore)]
    public NaeringsKodeDto? Naeringskode1 { get; set; }

    [JsonProperty("naeringskode2", NullValueHandling = NullValueHandling.Ignore)]
    public NaeringsKodeDto? Naeringskode2 { get; set; }

    [JsonProperty("naeringskode3", NullValueHandling = NullValueHandling.Ignore)]
    public NaeringsKodeDto? Naeringskode3 { get; set; }

    [JsonProperty("antallAnsatte")]
    public int  AntallAnsatte { get; set; }

    [JsonProperty("overordnetEnhet", NullValueHandling = NullValueHandling.Ignore)]
    public string? OverordnetEnhet { get; set; }

    [JsonProperty("oppstartsdato", NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
    public DateTimeOffset? OppstartsDato { get; set; }

    [JsonProperty("forretningsadresse", NullValueHandling = NullValueHandling.Ignore)]
    public AdresseDto? Forretningsadresse { get; set; }

    [JsonProperty("beliggenhetsadresse", NullValueHandling = NullValueHandling.Ignore)]
    public AdresseDto? Beliggenhetsadresse { get; set; }

    [JsonProperty("stiftelsesdato", NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
    public DateTimeOffset? Stiftelsesdato { get; set; }

    [JsonProperty("institusjonellSektorkode", NullValueHandling = NullValueHandling.Ignore)]
    public SektorKodeDto? InstitusjonellSektorkode { get; set; }

    [JsonProperty("registrertIForetaksregisteret", NullValueHandling = NullValueHandling.Ignore)]
    public bool? RegistrertIForetaksregisteret { get; set; }

    [JsonProperty("registrertIStiftelsesregisteret", NullValueHandling = NullValueHandling.Ignore)]
    public bool? RegistrertIStiftelsesregisteret { get; set; }

    [JsonProperty("registrertIFrivillighetsregisteret", NullValueHandling = NullValueHandling.Ignore)]
    public bool? RegistrertIFrivillighetsregisteret { get; set; }

    [JsonProperty("sisteInnsendteAarsregnskap", NullValueHandling = NullValueHandling.Ignore)]
    public string? SisteInnsendteAarsregnskap { get; set; }

    [JsonProperty("konkurs", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Konkurs { get; set; }

    [JsonProperty("underAvvikling", NullValueHandling = NullValueHandling.Ignore)]
    public bool? UnderAvvikling { get; set; }

    [JsonProperty("underTvangsavviklingEllerTvangsopplosning", NullValueHandling = NullValueHandling.Ignore)]
    public bool? UnderTvangsavviklingEllerTvangsopplosning { get; set; }

    [JsonProperty("maalform", NullValueHandling = NullValueHandling.Ignore)]
    public string? Maalform { get; set; }

    [JsonProperty("_links")]
    public Links Links { get; set; } = null!;
}

public class AdresseDto
{
    [JsonProperty("land")]
    public string Land { get; set; } = null!;

    [JsonProperty("landkode")]
    public string Landkode { get; set; } = null!;

    [JsonProperty("postnummer", NullValueHandling = NullValueHandling.Ignore)]
    public string? Postnummer { get; set; }

    [JsonProperty("poststed")]
    public string Poststed { get; set; } = null!;

    [JsonProperty("adresse")]
    public List<string> Adresse { get; set; } = null!;

    [JsonProperty("kommune", NullValueHandling = NullValueHandling.Ignore)]
    public string? Kommune { get; set; }

    [JsonProperty("kommunenummer", NullValueHandling = NullValueHandling.Ignore)]
    public string? Kommunenummer { get; set; }
}

public class NaeringsKodeDto
{
    [JsonProperty("beskrivelse")]
    public string Beskrivelse { get; set; } = null!;

    [JsonProperty("kode")]
    public string Kode { get; set; } = null!;

    [JsonProperty("hjelpeenhetskode", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Hjelpenhetskode { get; set; }
}

public class SektorKodeDto
{
    [JsonProperty("kode")]
    public string Kode { get; set; } = null!;

    [JsonProperty("beskrivelse")]
    public string Beskrivelse { get; set; } = null!;
}

public class Links
{
    [JsonProperty("self")]
    public Link Self { get; set; } = null!;

    [JsonProperty("overordnetEnhet", NullValueHandling = NullValueHandling.Ignore)]
    public Link? OverordnetEnhet { get; set; }
}

public class Link
{
    [JsonProperty("href")]
    public Uri Href { get; set; } = null!;
}

public class Organisasjonsform
{
    [JsonProperty("kode")]
    public string? Kode { get; set; }

    [JsonProperty("beskrivelse")]
    public string? Beskrivelse { get; set; }

    [JsonProperty("_links")]
    public Links Links { get; set; } = null!;
}