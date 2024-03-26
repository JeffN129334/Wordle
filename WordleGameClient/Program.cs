using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using WordleGameServer;

namespace WordleGameClient
{
    public class Program
    {
        //TODO: Add a nicer user interface
        static async Task Main(string[] args)
        {
            //Connect to the WordleGameServer
            var channel = GrpcChannel.ForAddress("https://localhost:7103");
            var wordle = new DailyWordle.DailyWordleClient(channel);


            using (var call = wordle.Play())
            {
                //Create an empty request and a blank string
                PlayRequest playRequest = new PlayRequest() { Word = "" };
                string guess;

                DisplayMainMenu();

                Console.WriteLine("\nType in a guess, or hit enter to quit!");
                do
                {
                    //Get input from user (if they just hit enter then exit the loop)
                    guess = Console.ReadLine()!;
                    if (guess != "")
                    {
                        //Populate the request with the user input and send it to the server
                        playRequest.Word = guess;
                        await call.RequestStream.WriteAsync(playRequest);

                        //Read the response from the server, and print it to the console
                        await call.ResponseStream.MoveNext();
                        PlayResponse playResponse = call.ResponseStream.Current;
                        Console.WriteLine(playResponse.Message);
                    }
                    else
                    {
                        //When the user is done then close the stream
                        await call.RequestStream.CompleteAsync();
                    }
                } while (guess != "");
            }

            //Display user stats (which is currently a placeholder string)
            Console.WriteLine("User Stats: ");
            GetStatsResponse statRes = wordle.GetStats(new Empty());
            Console.WriteLine(statRes.Message);


            await channel.ShutdownAsync();
            Console.WriteLine("\nAll done. Press a key to exit.");
            Console.ReadKey();
        }

        static void DisplayMainMenu()
        {
            Console.WriteLine("+-------------------+");
            Console.WriteLine("|   W O R D L E D   |");
            Console.WriteLine("+-------------------+");
            Console.WriteLine("\nYou have 6 chances to guess a 5-letter word.");
            Console.WriteLine("Each guess must be a 'playable' 5 letter word.");
            Console.WriteLine("After a guess the game will display a series of");
            Console.WriteLine("characters to show you how good your guess was.");
            Console.WriteLine("x - means the letter above is not in the word.");
            Console.WriteLine("? - means the letter should be in another spot.");
            Console.WriteLine("* - means the letter is correct in this spot.");
            Console.WriteLine("\tAvailable: a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z");
        }
    }
}