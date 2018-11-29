using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SpeechKiosk
{
    class LUIS
    {
        public static void GetLuisResult(string returnString)
        {
            Console.WriteLine("\n\nLUIS Result...\n\n");
            Rootobject2 JSON = JsonConvert.DeserializeObject<Rootobject2>(returnString);
            Console.WriteLine($"Query: {JSON.query} \n\nTop Intent: [{JSON.topScoringIntent.score}] {JSON.topScoringIntent.intent} \n\nSentiment: [{JSON.sentimentAnalysis.score}] {JSON.sentimentAnalysis.label}");
        }

        public static string GetLuisIntent(string returnString)
        {
            Rootobject2 JSON = JsonConvert.DeserializeObject<Rootobject2>(returnString);
            return JSON.topScoringIntent.intent;
        }

        public static void ResponseToLuisIntent(string intent, string input)
        {
            //here, we can customize the response based on luis intent
            //for demo purpose, we only get response from QnAMaker when intent is FAQ
            switch (intent)
            {
                case "Kiosk.FAQ":
                    QnAMaker.GetFAQ(input).Wait();
                    break;
                default:
                    //otherwise, it just says what intent is detected
                    Console.WriteLine($"You have reached {intent} in LUIS, and your input is {input}");
                    TextToSpeech.TTSRequest($"You have reached {intent} in LUIS, and your input is {input}").Wait();
                    break;
            }
        }

        public static async Task SendToLuis(string query, string luisId, string luisKey)
        {
            //perform a REST HTTP Get to detect intent
            var client = new HttpClient();
            string queryString = "q=" + query;
            var endpoint = "https://southeastasia.api.cognitive.microsoft.com/luis/v2.0/apps/" + luisId + "?subscription-key=" + luisKey + "&" + queryString;
            var response = await client.GetAsync(endpoint);
            var responseString = await response.Content.ReadAsStringAsync();
            var JSON = JsonConvert.DeserializeObject<Rootobject3>(responseString);
            ResponseToLuisIntent(JSON.topScoringIntent.intent, JSON.query);
        }

        public class Rootobject2
        {
            public string query { get; set; }
            public Topscoringintent topScoringIntent { get; set; }
            public object[] entities { get; set; }
            public Sentimentanalysis sentimentAnalysis { get; set; }
        }

        public class Topscoringintent
        {
            public string intent { get; set; }
            public float score { get; set; }
        }

        public class Sentimentanalysis
        {
            public string label { get; set; }
            public float score { get; set; }
        }


        public class Rootobject3
        {
            public string query { get; set; }
            public Topscoringintent2 topScoringIntent { get; set; }
            public Entity[] entities { get; set; }
            public Sentimentanalysis2 sentimentAnalysis { get; set; }
        }

        public class Topscoringintent2
        {
            public string intent { get; set; }
            public float score { get; set; }
        }

        public class Sentimentanalysis2
        {
            public string label { get; set; }
            public float score { get; set; }
        }

        public class Entity
        {
            public string entity { get; set; }
            public string type { get; set; }
            public int startIndex { get; set; }
            public int endIndex { get; set; }
            public float score { get; set; }
        }
    }
}
