using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using WordleGameServer;
using WordServer;

namespace WordServer.Clients
{
    //Static class used for interacting with the WordServer
    public static class WordServiceClient
    {
        private static DailyWord.DailyWordClient? _wordServer = null;

        //Connect to the word server, get the daily word with an empty request object, then use the response to populate the local string and return it
        public static string GetWord()
        {
            ConnectToService();

            Empty wordReq = new Empty();
            WordResponse? wordRes = _wordServer?.GetWord(wordReq);
            return wordRes?.Word ?? "Unable to get word";
        }

        //Connect to the word server, send a validate word request using the passed in word, then return the response
        public static bool ValidateWord(string word)
        {
            ConnectToService();

            ValidationRequest validReq = new ValidationRequest() { Word = word };
            ValidationResponse? validRes = _wordServer?.ValidateWord(validReq);

            return validRes?.Valid ?? false;
        }

        //Establish a connection to the word server, if one is not already established
        private static void ConnectToService()
        {
            if (_wordServer is null)
            {
                var channel = GrpcChannel.ForAddress("https://localhost:7112");
                _wordServer = new DailyWord.DailyWordClient(channel);
            }
        }
    }

}
