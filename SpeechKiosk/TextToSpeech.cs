using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SpeechKiosk
{
    class TextToSpeech
    {
        const string speechKey = "<Speech Service API Key>";

        //text to speech is slightly complicated
        //first, we need to obtain a token, then use the token to obtain the speech file
        //Note that the token will expire every 10mins, hence need to refresh the token
        public static async Task TTSRequest(string query)
        {
            string accessToken;
            Authentication auth = new Authentication("https://southeastasia.api.cognitive.microsoft.com/sts/v1.0/issuetoken", speechKey);
            accessToken = auth.GetAccessToken();

            string xml = GenerateSsml(query);

            string uri = "https://southeastasia.tts.speech.microsoft.com/cognitiveservices/v1";
            using (var client = new HttpClient())

            {
                HttpResponseMessage response;
                client.DefaultRequestHeaders.Add("Authorization",accessToken);
                client.DefaultRequestHeaders.Add("User-Agent", "Speech Kiosk Demo");
                client.DefaultRequestHeaders.Add("X-Microsoft-OutputFormat", "riff-24khz-16bit-mono-pcm");
                byte[] byteData = Encoding.UTF8.GetBytes(GenerateSsml(query));
                using (var content = new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/ssml+xml");
                    response = await client.PostAsync(uri, content);
                    var resStream = await response.Content.ReadAsStreamAsync();
                    SoundPlayer player = new SoundPlayer(resStream);
                    player.PlaySync();
                }
            }
        }

        public static string GenerateSsml(string text)
        {
            //can customize the speaker voice
            var ssmlDoc = new XDocument(
                new XElement("speak",
                new XAttribute("version", "1.0"),
                new XAttribute(XNamespace.Xml + "xmlns", "http://www.w3.org/2001/10/synthesis"),
                new XAttribute(XNamespace.Xml + "lang", "en-US"),
                new XElement("voice",
                    new XAttribute("name", "Microsoft Server Speech Text to Speech Voice (en-US, Jessa24kRUS)"),
                    text)));
            return ssmlDoc.ToString();
        }
    }

    public class Authentication
    {
        private string AccessUri;
        private string apiKey;
        private string accessToken;
        private Timer accessTokenRenewer;

        private const int RefreshTokenDuration = 9;

        public Authentication(string issueTokenUri, string apiKey)
        {
            this.AccessUri = issueTokenUri;
            this.apiKey = apiKey;

            this.accessToken = HttpPost(issueTokenUri, this.apiKey);

            accessTokenRenewer = new Timer(new TimerCallback(OnTokenExpiredCallback),
                this,
                TimeSpan.FromMinutes(RefreshTokenDuration),
                TimeSpan.FromMilliseconds(-1));
        }

        public string GetAccessToken()
        {
            return this.accessToken;
        }

        private void RenewAccessToken()
        {
            string newAccessToken = HttpPost(AccessUri, this.apiKey);

            this.accessToken = newAccessToken;
            Console.WriteLine(string.Format("Renewed token for user: {0} is: {1}",
                              this.apiKey,
                              this.accessToken));
        }

        private void OnTokenExpiredCallback(object stateInfo)
        {
            try
            {
                RenewAccessToken();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Failed renewing access token. Details: {0}", ex.Message));
            }
            finally
            {
                try
                {
                    accessTokenRenewer.Change(TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed to reschedule the timer to renew access token. Details: {0}", ex.Message));
                }
            }
        }

        private string HttpPost(string accessUri, string apiKey)
        {
            // Prepare OAuth request, and send text to receive wav file so that console app can play the audio
            WebRequest webRequest = WebRequest.Create(accessUri);
            webRequest.Method = "POST";
            webRequest.ContentLength = 0;
            webRequest.Headers["Ocp-Apim-Subscription-Key"] = apiKey;

            using (WebResponse webResponse = webRequest.GetResponse())
            {
                using (Stream stream = webResponse.GetResponseStream())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        byte[] waveBytes = null;
                        int count = 0;
                        do
                        {
                            byte[] buf = new byte[1024];
                            count = stream.Read(buf, 0, 1024);
                            ms.Write(buf, 0, count);
                        } while (stream.CanRead && count > 0);

                        waveBytes = ms.ToArray();

                        return Encoding.UTF8.GetString(waveBytes);
                    }
                }
            }
        }
    }
}
