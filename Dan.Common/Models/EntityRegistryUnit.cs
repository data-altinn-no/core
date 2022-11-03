﻿namespace Dan.Common.Models;

// ReSharper disable once InconsistentNaming
public class EntityRegistryUnit
{
    [JsonProperty("organisasjonsnummer")]
    public long Organisasjonsnummer { get; set; }

    [JsonProperty("navn")]
    public string Navn { get; set; } = string.Empty;

    [JsonProperty("organisasjonsform")]
    public Organisasjonsform Organisasjonsform { get; set; } = new();

    [JsonProperty("slettedato")]
    public DateTime Slettedato { get; set; }

    [JsonProperty("postadresse")]
    public Adresse Postadresse { get; set; } = new();

    [JsonProperty("registreringsdatoEnhetsregisteret")]
    public DateTimeOffset RegistreringsdatoEnhetsregisteret { get; set; }

    [JsonProperty("registrertIMvaregisteret")]
    public bool RegistrertIMvaregisteret { get; set; }

    [JsonProperty("frivilligMvaRegistrertBeskrivelser")]
    public List<string> FrivilligMvaRegistrertBeskrivelser { get; set; } = new();

    [JsonProperty("naeringskode1")]
    public InstitusjonellSektorkode Naeringskode1 { get; set; } = new();

    [JsonProperty("naeringskode2")]
    public InstitusjonellSektorkode? Naeringskode2 { get; set; }

    [JsonProperty("naeringskode3")]
    public InstitusjonellSektorkode? Naeringskode3 { get; set; }

    [JsonProperty("antallAnsatte")]
    public long AntallAnsatte { get; set; }

    [JsonProperty("overordnetEnhet")]
    public string? OverordnetEnhet { get; set; }

    [JsonProperty("forretningsadresse")]
    public Adresse Forretningsadresse { get; set; } = new();

    [JsonProperty("stiftelsesdato")]
    public DateTimeOffset Stiftelsesdato { get; set; }

    [JsonProperty("institusjonellSektorkode")]
    public InstitusjonellSektorkode InstitusjonellSektorkode { get; set; } = new();

    [JsonProperty("registrertIForetaksregisteret")]
    public bool RegistrertIForetaksregisteret { get; set; }

    [JsonProperty("registrertIStiftelsesregisteret")]
    public bool RegistrertIStiftelsesregisteret { get; set; }

    [JsonProperty("registrertIFrivillighetsregisteret")]
    public bool RegistrertIFrivillighetsregisteret { get; set; }

    [JsonProperty("sisteInnsendteAarsregnskap")]
    public long SisteInnsendteAarsregnskap { get; set; }

    [JsonProperty("konkurs")]
    public bool Konkurs { get; set; }

    [JsonProperty("underAvvikling")]
    public bool UnderAvvikling { get; set; }

    [JsonProperty("underTvangsavviklingEllerTvangsopplosning")]
    public bool UnderTvangsavviklingEllerTvangsopplosning { get; set; }

    [JsonProperty("maalform")]
    public string? Maalform { get; set; }

    [JsonProperty("_links")]
    public Links Links { get; set; } = new();
}

public class Adresse
{
    [JsonProperty("land")]
    public string? Land { get; set; }

    [JsonProperty("landkode")]
    public string? Landkode { get; set; }

    [JsonProperty("postnummer")]
    public long Postnummer { get; set; }

    [JsonProperty("poststed")]
    public string? Poststed { get; set; }

    [JsonProperty("adresse")]
    public List<string> AdresseAdresse { get; set; } = new();

    [JsonProperty("kommune")]
    public string? Kommune { get; set; }

    [JsonProperty("kommunenummer")]
    public string? Kommunenummer { get; set; }
}

public class InstitusjonellSektorkode
{
    [JsonProperty("kode")]
    public string? Kode { get; set; }

    [JsonProperty("beskrivelse")]
    public string? Beskrivelse { get; set; }
}

public class Links
{
    [JsonProperty("self")]
    public Self Self { get; set; } = new();
}

public class Self
{
    [JsonProperty("href")]
    public Uri Href { get; set; } = new("about:blank");
}

public class Organisasjonsform
{
    [JsonProperty("kode")]
    public string? Kode { get; set; }

    [JsonProperty("beskrivelse")]
    public string? Beskrivelse { get; set; }

    [JsonProperty("_links")]
    public Links Links { get; set; } = new();
}