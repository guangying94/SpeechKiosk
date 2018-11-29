using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SpeechKiosk
{
    class QnAMaker
    {
        private const string endpointKey = "<QnAMaker API Key>";
        private const string kbID = "<QnAMaker Knowledgebase ID>";
        private const string qnaMakerUrl = "https://<APP NAME>.azurewebsites.net/qnamaker/knowledgebases/";

        public static async Task GetFAQ(string question)
        {
            //perform a simple HTTP POST to get response
            Console.WriteLine("\n\nQnAMaker Results...\n\n");
            var uri = qnaMakerUrl + kbID + "/generateAnswer";
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent("{question:'" + question + "'}", Encoding.UTF8, "application/json");
                request.Headers.Add("Authorization", "EndpointKey " + endpointKey);
                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                Rootobject JSON = JsonConvert.DeserializeObject<Rootobject>(responseBody);
                Console.WriteLine($"Question: {JSON.answers[0].questions[0]} \n\nAnswer: {JSON.answers[0].answer} \n\nScore:{JSON.answers[0].score} \n\nSource:{JSON.answers[0].source}");
                //send text to perform text to speech
                TextToSpeech.TTSRequest(JSON.answers[0].answer).Wait();
            }
        }
    }


    public class Rootobject
    {
        public Answer[] answers { get; set; }
    }

    public class Answer
    {
        public string[] questions { get; set; }
        public string answer { get; set; }
        public float score { get; set; }
        public int id { get; set; }
        public string source { get; set; }
        public object[] metadata { get; set; }
    }
}

