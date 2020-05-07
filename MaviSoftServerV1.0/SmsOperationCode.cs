using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaviSoftServerV1._0
{
    public static class SmsOperationCode
    {
        public static string GetCreditXMLCode(string UserName, string Password, string UserCode, string AccountId)
        {
            string getCreditXML = @"<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'
                                    xmlns='https://webservice.asistiletisim.com.tr/SmsProxy'>
                            <soapenv:Header/>
                              <soapenv:Body>
                                 <getCredit>
                                    <requestXml>
                                     <![CDATA[<GetCredit>
                                     <Username>" + UserName + @"</Username>
                                     <Password>" + Password + @"</Password>
                                     <UserCode>" + UserCode + @"</UserCode>
                                     <AccountId>" + AccountId + @"</AccountId>
                                     </GetCredit>]]>
                                     </requestXml>
                                     </getCredit>
                                  </soapenv:Body>
                              </soapenv:Envelope>";
            return getCreditXML;
        }

        public static string GetOrginatorXMLCode(string UserName, string Password, string UserCode, string AccountId)
        {
            string getOrginatorXML = @"<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'
                                        xmlns='https://webservice.asistiletisim.com.tr/SmsProxy'>
                                <soapenv:Header/>
                                  <soapenv:Body>
                                    <getOriginator>
                                        <requestXml>
                                           <![CDATA[<GetOriginator>
                                           <Username>" + UserName + @"</Username>
                                           <Password>" + Password + @"</Password>
                                           <UserCode>" + UserCode + @"</UserCode>
                                           <AccountId>" + AccountId + @"</AccountId>
                                           </ GetOriginator >]]>
                                          </ requestXml >
                                      </ getOriginator >
                                    </ soapenv:Body >
                                   </ soapenv:Envelope > ";
            return getOrginatorXML;
        }

        public static string GetSmsXMLCodeOneToMany(string UserName, string Password, string UserCode, string AccountId, string Originator, string MessageText, List<string> ReceiverNumber, string IsCheckBlackList = "0", string ValidityPeriod = "60")
        {
            string sendSMSXML = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns=""https://webservice.asistiletisim.com.tr/SmsProxy"">
                                           <soapenv:Header/>
                                           <soapenv:Body>
                                              <sendSms>
                                                 <requestXml><![CDATA[<SendSms>
                                                       <Username>" + UserName + @"</Username>
                                                       <Password>" + Password + @"</Password>
                                                       <UserCode>" + UserCode + @"</UserCode>
                                                       <AccountId>" + AccountId + @"</AccountId>
                                                       <Originator>" + Originator + @"</Originator>
                                                       <ValidityPeriod>" + ValidityPeriod + @"</ValidityPeriod>
                                                       <SendDate/>
                                                       <MessageText>" + MessageText + @"</MessageText>
                                                       <IsCheckBlackList>" + IsCheckBlackList + @"</IsCheckBlackList>
                                                          <ReceiverList>";
            foreach (string receiverNumber in ReceiverNumber)
            {
                sendSMSXML += "<Receiver>" + receiverNumber + "</Receiver>";
            }
            sendSMSXML += @"</ReceiverList>
                                                    </SendSms>]]></requestXml>
                                              </sendSms>
                                           </soapenv:Body>
                                        </soapenv:Envelope>";
            return sendSMSXML;
        }

        public static string GetSmsXMLCodeManyToMany(string UserName, string Password, string UserCode, string AccountId, string Originator, string MessageText, Dictionary<string, string> NameSurname, List<string> ReceiverNumber, string IsCheckBlackList = "0", string ValidityPeriod = "60")
        {
            string sendSMSXML = @"<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'
                                      xmlns = 'https://webservice.asistiletisim.com.tr/SmsProxy'>
                                            <soapenv:Header/>
                                            <soapenv:Body>
                                            <sendSms>
                                            <requestXml>
                                            <![CDATA[<SendSms>
                                            <Username>" + UserName + @"</Username>
                                            <Password>" + Password + @"</Password>
                                            <UserCode>" + UserCode + @"</UserCode>
                                            <AccountId>" + AccountId + @"</AccountId> 
                                            <Originator>" + Originator + @"</Originator>
                                            <SendDate></SendDate>
                                            <ValidityPeriod>" + ValidityPeriod + @"</ValidityPeriod>
                                            <MessageText>Sayın [##Name##] [##Surname##] " + MessageText + @"</MessageText>
                                            <IsCheckBlackList>0</IsCheckBlackList>
                                            <ReceiverList>";
            foreach (string receiverNumber in ReceiverNumber)
            {
                sendSMSXML += "<Receiver>" + receiverNumber + "</Receiver>";
            }

            sendSMSXML += @"</ReceiverList>
                            <PersonalMessages>";

            foreach (KeyValuePair<string, string> nameSurname in NameSurname)
            {
                sendSMSXML += @"<PersonalMessage>
                                <Parameter>" + nameSurname.Key + @"</Parameter>
                                <Parameter>" + nameSurname.Value + @"</Parameter>
                                </PersonalMessage>";
            }
            sendSMSXML += @"</PersonalMessages>
                            </SendSms>]]>
                            </requestXml>
                            </sendSms>
                            </soapenv:Body>
                            </soapenv:Envelope>";

            return sendSMSXML;
        }

        public static string GetSmsXMLCodeManyToMany(string UserName, string Password, string UserCode, string AccountId, string Originator, string MessageText, List<string> Message, List<string> ReceiverNumber, string IsCheckBlackList = "0", string ValidityPeriod = "60")
        {
            string sendSMSXML = @"<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'
                                     xmlns = 'https://webservice.asistiletisim.com.tr/SmsProxy'>
                                           <soapenv:Header />
                                           <soapenv:Body >
                                           <sendSms><requestXml>
                                           <![CDATA[<SendSms>
                                           <Username>" + UserName + @"</Username>
                                           <Password>" + Password + @"</Password>
                                           <UserCode>" + UserCode + @"</UserCode>
                                           <AccountId>" + AccountId + @"</AccountId>
                                           <Originator>" + Originator + @"</Originator>
                                           <SendDate></SendDate>
                                           <ValidityPeriod>" + ValidityPeriod + @"</ValidityPeriod>
                                           <MessageText>[##MESAJ##]</MessageText>
                                           <IsCheckBlackList>" + IsCheckBlackList + @"</IsCheckBlackList>
                                           <ReceiverList>";
            foreach (string receiverNumber in ReceiverNumber)
            {
                sendSMSXML += " <Receiver>" + receiverNumber + "</Receiver>";
            }

            sendSMSXML += @"</ReceiverList>
                           <PersonalMessages>";
            foreach (string message in Message)
            {
                sendSMSXML += @"<PersonalMessage>
                                <Parameter>" + message + @"</Parameter>
                                </PersonalMessage>";
            }
            sendSMSXML += @"</PersonalMessages>
                            </SendSms>]]>
                            </requestXml>";

            return sendSMSXML;
        }


    }
}
