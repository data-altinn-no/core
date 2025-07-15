using Dan.Common.Models;
using Dan.Core.Config;
using Dan.Core.Exceptions;
using Dan.Core.Helpers.Correspondence;
using Dan.Core.Helpers.Notification;
using Dan.Core.Services.Interfaces;
using System.ServiceModel;
using Dan.Common.Interfaces;
using Dan.Core.Helpers;
using Dan.Core.Models;
using AltinnFault = Dan.Core.Helpers.Notification.AltinnFault;

namespace Dan.Core.Services;

/// <summary>
/// The <see cref="AltinnCorrespondenceService"/> class is an implementation of the <see cref="IAltinnCorrespondenceService"/> interface and represents
/// a wrapper around a client of the Altinn Correspondence service.
/// </summary>
internal class AltinnCorrespondenceService : IAltinnCorrespondenceService
{
    private const string LanguageCode = "1044";
    private const string FromAddress = "no-reply@altinn.no";
    private const string NotificationType = "TokenTextOnly";

    /// <summary>
    /// Gets or sets the service code of the correspondence service to use when creating correspondence elements.
    /// </summary>
    public string CorrespondenceServiceCode { get; set; }

    /// <summary>
    /// Gets or sets the service edition code of the correspondence service to use when creating correspondence elements.
    /// </summary>
    public string CorrespondenceServiceEdition { get; set; }

    /// <summary>
    /// Gets or sets the system user name to use in authentication with the Correspondence service.
    /// </summary>
    public string SystemUserName { get; set; }

    /// <summary>
    /// Gets or sets the system password to use in authentication with the Correspondence service.
    /// </summary>
    public string SystemPassword { get; set; }

    /// <summary>
    /// Gets or sets the system user code to use when creating a correspondence.
    /// </summary>
    public string SystemUserCode { get; set; }

    private static Random _rand = new Random();

    private static int Rand => _rand.Next(100000, 999999);

    private readonly IChannelManagerService _channelManagerService;
    private readonly Interfaces.IEntityRegistryService _entityRegistryService;

    public AltinnCorrespondenceService(
        IChannelManagerService channelManagerService,
        Interfaces.IEntityRegistryService entityRegistryService)
    {
        _channelManagerService = channelManagerService;
        _entityRegistryService = entityRegistryService;

        _entityRegistryService.AllowTestCcrLookup = !Settings.IsProductionEnvironment;

        var correspondenceSettings = Settings.CorrespondenceSettings.Split(',');
        var correspondenceServiceCode = correspondenceSettings[0].Trim();
        var correspondenceServiceEdition = correspondenceSettings[1].Trim();
        var systemUserCode = correspondenceSettings[2].Trim();

        CorrespondenceServiceCode = correspondenceServiceCode;
        CorrespondenceServiceEdition = correspondenceServiceEdition;

        SystemUserName = Settings.AgencySystemUserName;
        SystemPassword = Settings.AgencySystemPassword;
        SystemUserCode = systemUserCode;

        //for profiling and bughunting only
        if (Settings.UseAltinnTestServers)
        {
            _channelManagerService.Add<CorrespondenceAgencyExternalBasicClient>("ServiceEngineExternal/CorrespondenceAgencyExternalBasic.svc?bigiptestversion=true");
            _channelManagerService.Add<NotificationAgencyExternalBasicClient>("ServiceEngineExternal/NotificationAgencyExternalBasic.svc?bigiptestversion=true");
        }
        else
        {
            _channelManagerService.Add<CorrespondenceAgencyExternalBasicClient>("ServiceEngineExternal/CorrespondenceAgencyExternalBasic.svc");
            _channelManagerService.Add<NotificationAgencyExternalBasicClient>("ServiceEngineExternal/NotificationAgencyExternalBasic.svc");
        }
    }

    /// <inheritdoc />        
    public async Task<ReceiptExternal> SendCorrespondence(CorrespondenceDetails correspondence)
    {
        var insertCorrespondence = new InsertCorrespondenceV2
        {
            ServiceCode = CorrespondenceServiceCode,
            ServiceEdition = CorrespondenceServiceEdition,
            Reportee = correspondence.Reportee,
            MessageSender = correspondence.Sender,
            VisibleDateTime = DateTime.Now,
            AllowForwarding = false,
            Content = new ExternalContentV2
            {
                LanguageCode = LanguageCode,
                MessageTitle = correspondence.Title,
                MessageSummary = correspondence.Summary,
                MessageBody = correspondence.Body,
                Attachments = new AttachmentsV2()
            },
            Notifications = new NotificationBEList()
        };

        if (correspondence.Notification.SmsText != null)
        {
            insertCorrespondence.Notifications.Add(
                CreateNotification(Helpers.Correspondence.TransportType.SMS, correspondence.Notification.SmsText, string.Empty));
        }

        if (correspondence.Notification.EmailSubject != null && correspondence.Notification.EmailBody != null)
        {
            insertCorrespondence.Notifications.Add(
                CreateNotification(Helpers.Correspondence.TransportType.Email, correspondence.Notification.EmailSubject, correspondence.Notification.EmailBody));
        }

        var result = await _channelManagerService.With<CorrespondenceAgencyExternalBasicClient>(async x =>
                await x.InsertCorrespondenceBasicV2Async(SystemUserName, SystemPassword, SystemUserCode, $"EXT_SHIP{Rand}", insertCorrespondence))
            as InsertCorrespondenceBasicV2Response;

        ReceiptExternal? reply;
        try
        {
            reply = result?.Body.InsertCorrespondenceBasicV2Result;

            if (reply == null)
            {
                throw new Exception("Response was unexpectedly null");
            }
            else if (reply.ReceiptStatusCode != ReceiptStatusEnum.OK)
            {
                throw new AltinnServiceException($"{reply.ReceiptStatusCode}: {reply.ReceiptText}");
            }
        }
        catch (FaultException<AltinnFault> e)
        {
            throw new AltinnServiceException($"Could not send correspondence to Altinn: {e.Detail.AltinnErrorMessage}");
        }
        catch (Exception e)
        {
            throw new AltinnServiceException($"Could not send correspondence to Altinn: {e.Message}");
        }


        return reply;
    }

    private static Notification1 CreateNotification(Helpers.Correspondence.TransportType transportType, string subject, string body)
    {
        return new Notification1
        {
            FromAddress = FromAddress,
            LanguageCode = LanguageCode,
            NotificationType = NotificationType,
            ReceiverEndPoints = new Helpers.Correspondence.ReceiverEndPointBEList
            {
                new Helpers.Correspondence.ReceiverEndPoint
                {
                    TransportType = transportType
                }
            },
            TextTokens = new Helpers.Correspondence.TextTokenSubstitutionBEList
            {
                new Helpers.Correspondence.TextToken
                {
                    TokenNum = 0,
                    TokenValue = subject
                },
                new Helpers.Correspondence.TextToken
                {
                    TokenNum = 1,
                    TokenValue = body
                }
            }
        };
    }

    public async Task<List<NotificationReminder>> SendNotification(Accreditation accreditation, ServiceContext serviceContext)
    {
        StandaloneNotificationBEList notifications = await CreateNotifications(accreditation, serviceContext);
        var resultList = new List<NotificationReminder>();

        foreach (var notification in notifications)
        {
            var transportType = notification.ReceiverEndPoints.FirstOrDefault()?.TransportType.ToString();
            if (transportType == null)
            {
                continue;
            }

            try
            {
                var singleList = new StandaloneNotificationBEList { notification };
                var result =
                    (SendStandaloneNotificationBasicV3Response)await _channelManagerService
                        .With<NotificationAgencyExternalBasicClient>(async x =>
                            await x.SendStandaloneNotificationBasicV3Async(SystemUserName, SystemPassword, singleList));

                resultList.Add(GetNotificationReminderResponse("Sent", true, transportType,
                    result.Body.SendStandaloneNotificationBasicV3Result.Count()));
            }
            catch (FaultException<AltinnFault>)
            {
                resultList.Add(GetNotificationReminderResponse("Failed Altinn validation", false, transportType, 0));
            }
            catch (Exception)
            {
                resultList.Add(GetNotificationReminderResponse("Failed", false, transportType, 0));
            }
        }

        return resultList;
    }

    private NotificationReminder GetNotificationReminderResponse(string description, bool success, string type, int recipients)
    {
        return new NotificationReminder()
        {
            Description = description,
            Success = success,
            NotificationType = type,
            RecipientCount = recipients,
            Date = DateTime.Now
        };
    }

    private async Task<string> GetPartyDisplayName(Party party)
    {
        // TODO! Look up name of person? Party.ToString() will handle redacting.
        if (party.NorwegianOrganizationNumber == null) return party.ToString();

        var result = await _entityRegistryService.Get(party.NorwegianOrganizationNumber);
        return result?.Name ?? party.NorwegianOrganizationNumber;
    }

    private async Task<StandaloneNotificationBEList> CreateNotifications(Accreditation accreditation, ServiceContext serviceContext)
    {
        if (accreditation.RequestorParty == null)
        {
            throw new InvalidRequestorException("Accreditation missing requestor party");
        }

        if (accreditation.SubjectParty == null)
        {
            throw new InvalidSubjectException("Accreditation missing subject party");
        }

        var requestorName = await GetPartyDisplayName(accreditation.RequestorParty);
        var subjectName = await GetPartyDisplayName(accreditation.SubjectParty);

        var renderedTexts =
            TextTemplateProcessor.GetRenderedTexts(serviceContext, accreditation, requestorName, subjectName, null);

        var list = new StandaloneNotificationBEList();
        var notificationSms = new StandaloneNotification()
        {
            FromAddress = FromAddress,
            IsReservable = true,
            LanguageID = int.Parse(LanguageCode),
            NotificationType = NotificationType,
            ReporteeNumber = accreditation.Subject,
            UseServiceOwnerShortNameAsSenderOfSms = false,
            Service = new Service()
            {
                ServiceCode = this.CorrespondenceServiceCode,
                ServiceEdition = int.Parse(this.CorrespondenceServiceEdition)
            },
            Roles = null,
            ShipmentDateTime = DateTime.Now,
            TextTokens = GetTextTokens(renderedTexts.SMSNotificationContent, string.Empty),
            ReceiverEndPoints = GetReceiverEndpoints(Helpers.Notification.TransportType.SMS)
        };

        var notificationEmail = new StandaloneNotification()
        {
            FromAddress = FromAddress,
            IsReservable = true,
            LanguageID = int.Parse(LanguageCode),
            NotificationType = NotificationType,
            ReporteeNumber = accreditation.Subject,
            UseServiceOwnerShortNameAsSenderOfSms = false,
            Service = new Service()
            {
                ServiceCode = this.CorrespondenceServiceCode,
                ServiceEdition = int.Parse(this.CorrespondenceServiceEdition)
            },
            Roles = null,
            ShipmentDateTime = DateTime.Now,
            TextTokens = GetTextTokens(renderedTexts.EmailNotificationSubject, renderedTexts.EmailNotificationContent),
            ReceiverEndPoints = GetReceiverEndpoints(Helpers.Notification.TransportType.Email)
        };

        list.Add(notificationSms);
        list.Add(notificationEmail);

        return list;
    }

    private Helpers.Notification.ReceiverEndPointBEList GetReceiverEndpoints(Helpers.Notification.TransportType type)
    {
        var list = new Helpers.Notification.ReceiverEndPointBEList();

        list.Add(new Helpers.Notification.ReceiverEndPoint()
        {
            TransportType = type
        });

        return list;
    }

    private Helpers.Notification.TextTokenSubstitutionBEList GetTextTokens(string subject, string body)
    {
        var list = new Helpers.Notification.TextTokenSubstitutionBEList();
        list.Add(new Helpers.Notification.TextToken()
        {
            TokenNum = 0,
            TokenValue = subject
        });

        list.Add(new Helpers.Notification.TextToken()
        {
            TokenNum = 1,
            TokenValue = body
        });

        return list;
    }
}