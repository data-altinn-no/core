using Dan.Common;
using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.ServiceContextTexts;
using Dan.Core.Services.Interfaces;

namespace Dan.Core.Services;

public class ServiceContextService : IServiceContextService
{
    public async Task<List<ServiceContext>> GetRegisteredServiceContexts()
    {
        var serviceContexts = new List<ServiceContext>() {
            new ServiceContext() {
                Name = "eBevis",
                Id = "ebevis-product",
                Owner = "Brønnøysundregistrene",
                ValidLanguages = new List<string>() {Constants.LANGUAGE_CODE_NORWEGIAN_NB },
                AuthorizationRequirements = new List<Requirement>() {
                    new PartyTypeRequirement()
                    {
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Requestor,PartyTypeConstraint.PublicAgency)
                        }
                    }
                },
                ServiceContextTextTemplate = new EBevisServiceContextTextTemplate()
            },
            new ServiceContext()
            {
                Name = "Drosjeloyve",
                Id = "drosjeloyve-product",
                Owner = "Novari IKS",
                ValidLanguages = new List<string>() {Constants.LANGUAGE_CODE_NORWEGIAN_NB, Constants.LANGUAGE_CODE_NORWEGIAN_NN },
                AuthorizationRequirements = new List<Requirement>() {
                    new PartyTypeRequirement()
                    {
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Requestor,PartyTypeConstraint.PublicAgency)
                        }
                    }
                },
                ServiceContextTextTemplate = new DrosjeloyveServiceContextTextTemplate()
            },
            new ServiceContext()
            {
                Name = "Tilda",
                Id = "tilsynsdata-product",
                Owner = "Brønnøysundregistrene",
                ValidLanguages = new List<string>() {Constants.LANGUAGE_CODE_NORWEGIAN_NB },
                AuthorizationRequirements = new List<Requirement>() {
                    new MaskinportenScopeRequirement()
                    {
                        RequiredScopes = new List<string> { "altinn:dataaltinnno/tilda" }
                    },
                    new AccreditationPartyRequirement()
                    {
                        PartyRequirements = new List<AccreditationPartyRequirementType>()
                        {
                            AccreditationPartyRequirementType.RequestorAndOwnerAreEqual
                        }
                    }
                }
            },
            new ServiceContext()
            {
                Name = "Seriøsitetsinformasjon",
                Id = "seriositet-product",
                Owner = "Brønnøysundregistrene",
                ValidLanguages = new List<string>() {Constants.LANGUAGE_CODE_NORWEGIAN_NB },
                AuthorizationRequirements = new List<Requirement>() {
                    new PartyTypeRequirement()
                    {
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Requestor,PartyTypeConstraint.PrivateEnterprise)
                        }
                    },
                    new AccreditationPartyRequirement()
                    {
                        PartyRequirements = new List<AccreditationPartyRequirementType>()
                        {
                            AccreditationPartyRequirementType.RequestorAndOwnerAreEqual
                        }
                    }
                },
                ServiceContextTextTemplate = new SeriositetServiceContextTextTemplate()
            },
            new ServiceContext()
            {
                Name = "OED",
                Id = "oed-product",
                Owner = "Digitaliseringsdirektoratet",
                ValidLanguages = new List<string>() {Constants.LANGUAGE_CODE_NORWEGIAN_NB },
                AuthorizationRequirements = new List<Requirement>() {
                    new PartyTypeRequirement()
                    {
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Subject,PartyTypeConstraint.PrivatePerson)

                        }
                    },
                    new MaskinportenScopeRequirement()
                    {
                        RequiredScopes = new List<string>() { "altinn:dataaltinnno/oed" }
                    }
                }
            },
            new ServiceContext()
            {
                Name = "Advokatregisteret",
                Id = "tilsynsraad-product",
                Owner = "Advokattilsynet", 
                ValidLanguages= new List<string>() {Constants.LANGUAGE_CODE_NORWEGIAN_NB},
                AuthorizationRequirements = new List<Requirement>()
                { 
                    new AccreditationPartyRequirement()
                    {
                        PartyRequirements = new List<AccreditationPartyRequirementType>()
                        {
                            AccreditationPartyRequirementType.RequestorAndOwnerAreEqual
                        }
                    }
                }
            },
            new ServiceContext()
            {
                Name = "DigitaleHelgeland",
                Id = "dihe-product",
                Owner = "Digitale Helgeland",
                ValidLanguages = new List<string>() {Constants.LANGUAGE_CODE_NORWEGIAN_NB },
                AuthorizationRequirements = new List<Requirement>() {
                    new PartyTypeRequirement()
                    {
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Subject,PartyTypeConstraint.PrivatePerson),
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Requestor, PartyTypeConstraint.PublicAgency)
                        }
                    },

                    new AccreditationPartyRequirement()
                    {
                        PartyRequirements = new List<AccreditationPartyRequirementType>() { AccreditationPartyRequirementType.RequestorAndOwnerAreEqual }
                    },
                    new MaskinportenScopeRequirement()
                    {
                        RequiredScopes = new List<string>() { "altinn:dataaltinnno/dihe" }
                    }
                }
            },
            new ServiceContext()
            {
                Name = "EURES",
                Id = "eures-product",
                Owner = "Arbeids- og velferdsetaten (Nav)",
                ValidLanguages = new List<string>() {Constants.LANGUAGE_CODE_NORWEGIAN_NB },
                AuthorizationRequirements = new List<Requirement>() {
                    new MaskinportenScopeRequirement()
                    {
                        RequiredScopes = new List<string>() { "altinn:dataaltinnno/eures" }
                    }
                },
                ServiceContextTextTemplate = new EuresServiceContextTextTemplate()
            },
            new ServiceContext()
            {
                Name = "Økonomisk informasjon",
                Id = "okinfo-product",
                Owner = "Brønnøysundregistrene",
                ValidLanguages = new List<string>() {Constants.LANGUAGE_CODE_NORWEGIAN_NB },
                AuthorizationRequirements = new List<Requirement>() {
                    new MaskinportenScopeRequirement()
                    {
                        RequiredScopes = new List<string>() { "altinn:dataaltinnno/okinfo" }
                    },
                    new PartyTypeRequirement()
                    {
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Subject,PartyTypeConstraint.PrivateEnterprise),
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Requestor, PartyTypeConstraint.PublicAgency)
                        }
                    },
                    new AccreditationPartyRequirement()
                    {
                        PartyRequirements = new List<AccreditationPartyRequirementType>() { AccreditationPartyRequirementType.RequestorAndOwnerAreEqual }
                    }
                },
                ServiceContextTextTemplate = new OkinfoServiceContextTextTemplate()
            },
            new ServiceContext()
            {
                Name = "Reelle rettighetshavere",
                Id = "reelle-product",
                Owner = "Brønnøysundregistrene",
                ValidLanguages = new List<string>() {Constants.LANGUAGE_CODE_NORWEGIAN_NB },
                AuthorizationRequirements = new List<Requirement>() {
                    new MaskinportenScopeRequirement()
                    {
                        RequiredScopes = new List<string>() { "altinn:dataaltinnno/reelle" }
                    },
                    new AccreditationPartyRequirement()
                    {
                        PartyRequirements = new List<AccreditationPartyRequirementType>() { AccreditationPartyRequirementType.RequestorAndOwnerAreEqual }
                    }
                }
            },
            new ServiceContext()
            {
                Name = "eDueDiligence",
                Id = "duediligence-product",
                Owner = "Brønnøysundregistrene",
                ValidLanguages = new List<string>() {Constants.LANGUAGE_CODE_NORWEGIAN_NB },
                AuthorizationRequirements = new List<Requirement>() {
                    new MaskinportenScopeRequirement()
                    {
                        RequiredScopes = new List<string>() { "altinn:dataaltinnno/duediligence" }
                    },
                    new AccreditationPartyRequirement()
                    {
                        PartyRequirements = new List<AccreditationPartyRequirementType>() { AccreditationPartyRequirementType.RequestorAndOwnerAreEqual }
                    }
                }
            },
            new ServiceContext()
            {
                Name = "DigitalGravferdsmelding",
                Id = "dgm-product",
                Owner = "Statsforvalteren i Vestfold og Telemark",
                ValidLanguages = new List<string>() {Constants.LANGUAGE_CODE_NORWEGIAN_NB },
                AuthorizationRequirements = new List<Requirement>() {
                    new MaskinportenScopeRequirement()
                    {
                        RequiredScopes = new List<string>() { "altinn:dataaltinnno/dgm" }
                    },
                    new AccreditationPartyRequirement()
                    {
                        PartyRequirements = new List<AccreditationPartyRequirementType>() { AccreditationPartyRequirementType.RequestorAndOwnerAreEqual }
                    }
                }
            },
             new ServiceContext()
            {
                Name = "Bits kontrollinformasjon",
                Id = "bits-product",
                Owner = "BITS AS",
                ValidLanguages = new List<string>() {Constants.LANGUAGE_CODE_NORWEGIAN_NB },
                AuthorizationRequirements = new List<Requirement>()
                {
                    new MaskinportenScopeRequirement() { RequiredScopes = new List<string> {"altinn:dataaltinnno/kontrollinformasjon"}}
                }
            },
			new ServiceContext
            {
                Name = "DAN-test",
                Id = "dantest-product",
                Owner = "Digitaliseringsdirektoratet",
                ValidLanguages = [Constants.LANGUAGE_CODE_NORWEGIAN_NB],
                AuthorizationRequirements =
                [
                    new MaskinportenScopeRequirement{RequiredScopes = ["dan:test"]}
                ],
                ServiceContextTextTemplate = new DanTestServiceContextTextTemplate()
            },
            new ServiceContext()
            {
                Name = "Altinn Studio-apps",
                Id = "altinnstudioapps-product",
                Owner = "Digitaliseringsdirektoratet",
                ValidLanguages = new List<string>() {Constants.LANGUAGE_CODE_NORWEGIAN_NB },
                AuthorizationRequirements = new List<Requirement>()
                {
                    new MaskinportenScopeRequirement()
                    {
                        RequiredScopes = new List<string>() { "dan:altinnstudioapps" }
                    },
                    new ProvideOwnTokenRequirement(),
                    new AccreditationPartyRequirement()
                    {
                        PartyRequirements = new List<AccreditationPartyRequirementType>() { AccreditationPartyRequirementType.RequestorAndOwnerAreEqual }
                    }
                }
            },                    
            new ServiceContext() {
                Name = "Digøk-friv",
                Id = "digokfriv-product", 
                Owner = "Brønnøysundregistrene",
                ValidLanguages = new List<string>() {Constants.LANGUAGE_CODE_NORWEGIAN_NB }                
            },
        };

        return await Task.FromResult(serviceContexts);
    }
}